#!/usr/bin/env python3
"""Prueba local: invitación -> SNS/SQS -> Mailpit -> aceptación -> login.

Crea una invitación y un usuario de prueba nuevos; no borra registros existentes.
No imprime contraseñas, JWT ni tokens de invitación.
"""

import argparse
import datetime
import html
import json
import os
from pathlib import Path
import re
import secrets
import time
import urllib.error
import urllib.parse
import urllib.request


ROOT = Path(__file__).resolve().parent.parent


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--reservations", action="store_true", help="Crear y cancelar una reserva del nuevo usuario; comprobar sus correos")
    args = parser.parse_args()
    values = {}
    for line in (ROOT / ".env").read_text().splitlines():
        key, separator, value = line.partition("=")
        if separator and not line.lstrip().startswith("#"):
            values[key.strip()] = os.environ.get(key.strip(), value.strip().strip("\"'"))
    identity = f"http://127.0.0.1:{values.get('IDENTITY_PORT', '5100')}"
    company_api = f"http://127.0.0.1:{values.get('COMPANY_PORT', '5000')}"
    mailpit = f"http://127.0.0.1:{values.get('MAILPIT_PORT', '8025')}"
    rooms_api = f"http://127.0.0.1:{values.get('ROOMS_PORT', '5300')}"
    reservation_api = f"http://127.0.0.1:{values.get('RESERVATION_PORT', '8081')}"

    def require(condition, message):
        if not condition:
            raise RuntimeError(message)

    def request(base, path, payload=None, bearer=None, method=None):
        headers = {}
        if bearer:
            headers["Authorization"] = "Bearer " + bearer
        if payload is not None:
            headers["Content-Type"] = "application/json"
        req = urllib.request.Request(
            base + path,
            data=json.dumps(payload).encode() if payload is not None else None,
            headers=headers,
            method=method,
        )
        try:
            with urllib.request.urlopen(req, timeout=20) as response:
                content = response.read().decode()
                return response.status, json.loads(content) if content else None
        except urllib.error.HTTPError as error:
            code = error.code
            error.close()
            return code, None

    def login(email, password):
        status, data = request(identity, "/api/auth/login", {"email": email, "password": password})
        require(status == 200 and data and data.get("token"), "Falló el login de la prueba")
        return data["token"]

    admin = login(
        os.environ.get("WORKLY_TEST_ADMIN_EMAIL", "admin@saas.com"),
        os.environ.get("WORKLY_TEST_ADMIN_PASSWORD", "Password1"),
    )
    status, companies = request(company_api, "/api/companies", bearer=admin)
    require(status == 200 and isinstance(companies, list), "No se pudieron consultar empresas")
    company = next((item for item in companies if item.get("areas")), None)
    require(company is not None, "La prueba necesita una empresa con un área")
    recipient = f"invitation-check-{secrets.token_hex(6)}@example.com"
    status, invitation = request(identity, "/api/invitations", {
        "userEmail": recipient,
        "companyId": company["id"],
        "areaId": company["areas"][0]["id"],
    }, bearer=admin)
    require(status == 200 and invitation and invitation.get("id"), "No se pudo crear la invitación")
    print("Invitación de prueba creada", flush=True)

    deadline = time.monotonic() + 90
    message = None
    while time.monotonic() < deadline:
        status, listing = request(mailpit, "/api/v1/search?" + urllib.parse.urlencode({"query": "to:" + recipient}))
        if status == 200 and listing and listing.get("messages"):
            status, message = request(mailpit, "/api/v1/message/" + listing["messages"][0]["ID"])
            if status == 200:
                break
        time.sleep(1)
    require(message is not None, "No llegó el correo a Mailpit; revisar SNS/SQS y el worker")
    content = html.unescape(message.get("Text", "") + "\n" + message.get("HTML", ""))
    link = None
    for candidate in re.findall(r"https?://[^\s<>\"']+", content):
        parts = urllib.parse.urlsplit(candidate)
        parameters = urllib.parse.parse_qs(parts.query)
        if parameters.get("invitationId") == [invitation["id"]] and parameters.get("token"):
            link = parts
            break
    require(link is not None, "El correo no contiene el enlace esperado")
    require(link.hostname in {"localhost", "127.0.0.1"}, "El enlace no apunta al frontend local")
    require(link.port == int(values.get("FRONTEND_PORT", "8080")), "El enlace usa un puerto de frontend incorrecto")
    require(link.path == "/invitations/accept", "Ruta de aceptación inesperada")
    print("Correo recibido en Mailpit con enlace local válido", flush=True)

    status, info = request(identity, "/api/invitations/" + invitation["id"])
    require(status == 200 and info and info.get("email") == recipient, "No se pueden consultar los datos de invitación")
    password = secrets.token_urlsafe(24) + "aA1"
    token = urllib.parse.parse_qs(link.query)["token"][0]
    payload = {"invitationId": invitation["id"], "token": token, "password": password}
    status, _ = request(identity, "/api/invitations/accept", {**payload, "token": "invalid-token-for-local-test"})
    require(status == 400, "No se rechazó el token inválido")
    status, accepted = request(identity, "/api/invitations/accept", payload)
    require(status == 200 and accepted and accepted.get("email") == recipient, "No se pudo aceptar la invitación")
    status, _ = request(identity, "/api/invitations/accept", payload)
    require(status == 409, "No se rechazó la reutilización del enlace")
    member = login(recipient, password)
    print("Aceptación y login correctos; token inválido y reutilización rechazados", flush=True)
    if args.reservations:
        status, rooms = request(rooms_api, "/api/rooms", bearer=member)
        require(status == 200 and isinstance(rooms, list) and rooms, "La prueba necesita una sala disponible")
        start = (datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(
            days=30, minutes=secrets.randbelow(10000))).replace(second=0, microsecond=0)
        timestamp = lambda date: date.isoformat().replace("+00:00", "Z")
        status, reservation = request(reservation_api, "/api/reservations", {
            "roomId": rooms[0]["id"], "start": timestamp(start),
            "end": timestamp(start + datetime.timedelta(minutes=15)),
        }, bearer=member)
        require(status == 201 and reservation and reservation.get("id"), "No se pudo crear la reserva de prueba")
        status, _ = request(reservation_api, "/api/reservations/" + reservation["id"], bearer=member, method="DELETE")
        require(status == 200, "No se pudo cancelar la reserva recién creada")
        deadline = time.monotonic() + 90
        delivered = False
        while time.monotonic() < deadline:
            status, listing = request(mailpit, "/api/v1/search?" + urllib.parse.urlencode({"query": "to:" + recipient}))
            if status == 200 and listing and len(listing.get("messages", [])) >= 3:
                delivered = True
                break
            time.sleep(1)
        require(delivered, "No llegaron ambos correos de creación y cancelación de reserva")
        print("Reserva nueva creada y cancelada; ambos correos recibidos en Mailpit", flush=True)
    print("Se conservaron el usuario y el correo ficticios de esta prueba en el entorno local.")


if __name__ == "__main__":
    try:
        main()
    except RuntimeError as error:
        raise SystemExit(f"Prueba fallida: {error}. No se borraron datos.") from None
    except (OSError, ValueError, KeyError):
        raise SystemExit(
            "La prueba no pudo completarse. Comprobá el arranque con --messaging, "
            "el usuario administrador, Mailpit y los logs locales; no se borraron datos."
        ) from None
