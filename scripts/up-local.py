#!/usr/bin/env python3
"""Arranque local por etapas, comprobando explícitamente las migraciones."""

import argparse
import os
from pathlib import Path
import subprocess
import time
import urllib.error
import urllib.request


ROOT = Path(__file__).resolve().parent.parent


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build", action="store_true", help="Construir imágenes antes de arrancar")
    parser.add_argument("--audit", action="store_true", help="Incluir Audit y SQL Server (más memoria)")
    args = parser.parse_args()
    if not (ROOT / ".env").is_file():
        raise SystemExit("Primero ejecutá: python3 scripts/init-local-env.py")

    values = {}
    for line in (ROOT / ".env").read_text().splitlines():
        if not line.strip() or line.lstrip().startswith("#"):
            continue
        key, separator, value = line.partition("=")
        if separator:
            key = key.strip()
            values[key] = os.environ.get(key, value.strip().strip("\"'"))
    private = [
        value for key, value in values.items()
        if value and ("PASSWORD" in key or "SECRET" in key or key == "INTERNAL_SERVICE_KEY")
    ]
    compose = ["podman", "compose", "--env-file", ".env", "-f", "compose.yaml"]
    if args.audit:
        compose += ["--profile", "audit"]

    def run(command, label, timeout=300):
        print(label, flush=True)
        result = subprocess.run(command, cwd=ROOT, capture_output=True, text=True, timeout=timeout)
        if result.returncode:
            output = result.stdout + result.stderr
            for value in private:
                output = output.replace(value, "[REDACTED]")
            print("\n".join(line[:500] for line in output.splitlines()[-25:]))
            raise RuntimeError(f"{label}: código de salida {result.returncode}")

    def ready(service, port, path):
        url = f"http://127.0.0.1:{port}{path}"
        deadline = time.monotonic() + 120
        while time.monotonic() < deadline:
            try:
                with urllib.request.urlopen(url, timeout=3) as response:
                    if response.status == 200:
                        print(f"{service}: HTTP 200", flush=True)
                        return
            except (urllib.error.URLError, TimeoutError):
                pass
            time.sleep(1)
        raise RuntimeError(f"{service} no respondió correctamente en {url}")

    try:
        run(compose + ["config", "-q"], "Validando configuración")
        if args.build:
            builds = ["company-api", "identity-api", "rooms-api", "reservation-api", "frontend"]
            if args.audit:
                builds.append("audit-api")
            for service in builds:
                run(compose + ["build", service], f"Construyendo {service}", timeout=1800)

        databases = ["company-db", "identity-db", "rooms-db", "reservation-db"]
        if args.audit:
            databases.append("audit-db")
        run(compose + ["up", "-d", "--no-deps", *databases], "Arrancando bases de datos")
        project = values.get("COMPOSE_PROJECT_NAME", "workly")
        containers = [f"{project}_{service}_1" for service in databases]
        run(["podman", "wait", "--condition", "healthy", *containers], "Esperando bases saludables")

        migrations = ["company-migrate", "identity-migrate", "rooms-migrate"]
        if args.audit:
            migrations.append("audit-migrate")
        for service in migrations:
            # run propaga el exit code real del proceso; no basta con que esté detenido.
            run(
                compose + ["run", "--rm", "--no-deps", "-T", service, "migrate"],
                f"Ejecutando {service}",
            )

        apis = ["company-api", "identity-api", "rooms-api"]
        if args.audit:
            apis.append("audit-api")
        run(compose + ["up", "-d", "--no-deps", *apis], "Arrancando APIs migradas")
        for service, port, path in [
            ("Company", values.get("COMPANY_PORT", "5000"), "/health"),
            ("Identity", values.get("IDENTITY_PORT", "5100"), "/api/health"),
            ("Rooms", values.get("ROOMS_PORT", "5300"), "/health"),
        ]:
            ready(service, port, path)
        if args.audit:
            ready("Audit", values.get("AUDIT_PORT", "5151"), "/health")

        # Reservation migra al iniciar; su health confirma que terminó antes de servir.
        run(
            compose + ["up", "-d", "--no-deps", "reservation-api", "frontend"],
            "Arrancando Reservation y frontend",
        )
        ready("Reservation", values.get("RESERVATION_PORT", "8081"), "/health")
        ready("Frontend", values.get("FRONTEND_PORT", "8080"), "/")
        print(f"Workly listo: http://localhost:{values.get('FRONTEND_PORT', '8080')}")
    except (RuntimeError, subprocess.TimeoutExpired, OSError) as error:
        raise SystemExit(
            f"Arranque interrumpido: {error}\n"
            "No se eliminaron datos. Revisá los contenedores/logs antes de reintentar."
        ) from None


if __name__ == "__main__":
    main()
