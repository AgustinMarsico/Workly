#!/usr/bin/env python3
"""Crea el .env local con secretos aleatorios, sin mostrar ni sobrescribir valores."""

import os
from pathlib import Path
import secrets


SECRET_NAMES = {
    "SUPER_SECRET_KEY",
    "INTERNAL_SERVICE_KEY",
    "API_KEY_SECRET",
    "COMPANY_DB_ROOT_PASSWORD",
    "COMPANY_DB_PASSWORD",
    "IDENTITY_DB_ROOT_PASSWORD",
    "IDENTITY_DB_PASSWORD",
    "ROOMS_DB_ROOT_PASSWORD",
    "ROOMS_DB_PASSWORD",
    "RESERVATION_DB_ROOT_PASSWORD",
    "RESERVATION_DB_PASSWORD",
    "AUDIT_DB_SA_PASSWORD",
    "AUDIT_DB_APP_PASSWORD",
}


def main():
    root = Path(__file__).resolve().parent.parent
    target = root / ".env"
    lines = (root / ".env.example").read_text().splitlines()
    found = set()
    for index, line in enumerate(lines):
        name, separator, _ = line.partition("=")
        if separator and name in SECRET_NAMES:
            # Alfanumérico: seguro para interpolación y cadenas de conexión.
            # El prefijo también satisface la complejidad de SQL Server.
            lines[index] = f"{name}=Wk1a{secrets.token_hex(32)}"
            found.add(name)
    if found != SECRET_NAMES:
        raise SystemExit("La plantilla no contiene todos los secretos esperados.")

    try:
        fd = os.open(target, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
    except FileExistsError:
        raise SystemExit("Ya existe workly/.env; no se sobrescribió ningún valor.")
    with os.fdopen(fd, "w") as stream:
        stream.write("\n".join(lines) + "\n")
    print("Creado workly/.env con secretos locales y permisos 600. No lo compartas.")


if __name__ == "__main__":
    main()
