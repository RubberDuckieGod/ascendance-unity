#!/usr/bin/env python3
"""Verify the reviewed public h endpoint with one unauthenticated HELLO.

The endpoint, room, build and owner-reported certificate pin are fixed. No room
key, player credential, JOIN or gameplay command is accepted or transmitted.
Only public verification booleans are written; raw responses are never emitted.
"""
import argparse
import hashlib
import importlib.util
import ipaddress
import json
from pathlib import Path
import socket
import ssl
import struct
import tempfile
import time
import uuid


HOST = "159.65.100.206"
PORT = 7777
ROOM = "ASC-PRIMARY"
BUILD = "0.10.2h"
PIN = "7f9987d3848887b028e52e2e8f9077035cedca0e038711212c2ba7c86fb7a6a9"
MAX_FRAME = 8 * 1024 * 1024


class Refusal(Exception):
    pass


def require(condition, message):
    if not condition:
        raise Refusal(message)


def read_exact(stream, count):
    value = bytearray()
    while len(value) < count:
        block = stream.recv(count - len(value))
        require(bool(block), "The public HELLO connection closed before its response")
        value.extend(block)
    return bytes(value)


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        require(key not in result, "The public HELLO response contains duplicate fields")
        result[key] = value
    return result


def verify_certificate(certificate, now=None):
    require(hashlib.sha256(certificate).hexdigest() == PIN,
            "The public certificate does not match the owner-reported activation pin")
    with tempfile.TemporaryDirectory(prefix="asc-public-certificate-") as directory:
        path = Path(directory) / "certificate.pem"
        path.write_text(ssl.DER_cert_to_PEM_cert(certificate))
        decoded = ssl._ssl._test_decode_cert(str(path))
    current = time.time() if now is None else now
    require(ssl.cert_time_to_seconds(decoded["notBefore"]) <= current <=
            ssl.cert_time_to_seconds(decoded["notAfter"]),
            "The pinned public certificate is outside its validity period")
    addresses = [value for kind, value in decoded.get("subjectAltName", []) if kind == "IP Address"]
    require(any(ipaddress.ip_address(value) == ipaddress.ip_address(HOST) for value in addresses),
            "The pinned certificate does not identify the reviewed public address")


def verify_response(response, request_id):
    require(isinstance(response, dict), "The public HELLO response is not recognized")
    require(type(response.get("ProtocolVersion")) is int and response["ProtocolVersion"] == 1,
            "The public protocol version differs from the reviewed game")
    require(response.get("RequestId") == request_id, "The public HELLO correlation differs")
    require(response.get("ServerBuildVersion") == BUILD, "The public game build differs from h")
    source = Path(__file__).resolve().with_name("native_server_acceptance.py")
    spec = importlib.util.spec_from_file_location("public_h_blank_authority", source)
    native = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(native)
    require(native.denied_without_authority(response),
            "The public server did not reject missing room access without authority data")


def run():
    report = {"format": "ascendance-public-h-tls-probe-1", "endpoint": HOST + ":" + str(PORT),
              "room": ROOM, "expectedBuildVersion": BUILD, "expectedCertificateSha256": PIN,
              "roomAccessKeySubmitted": False, "playerCredentialsSubmitted": False,
              "joinRequestsSubmitted": 0, "gameplayCommandsSubmitted": 0}
    request_id = uuid.uuid4().hex
    request = {"ProtocolVersion": 1, "RequestId": request_id, "Operation": "HELLO", "RoomId": ROOM}
    context = ssl.SSLContext(ssl.PROTOCOL_TLS_CLIENT)
    context.minimum_version = ssl.TLSVersion.TLSv1_2
    context.check_hostname = False
    context.verify_mode = ssl.CERT_NONE  # Authentication is exact pin + time + IP SAN below.
    try:
        with socket.create_connection((HOST, PORT), timeout=12) as raw:
            raw.settimeout(12)
            with context.wrap_socket(raw, server_hostname=HOST) as stream:
                verify_certificate(stream.getpeercert(binary_form=True))
                report.update(publicTlsReachable=True, certificatePinVerified=True,
                              certificateTimeAndAddressVerified=True, tlsVersion=stream.version())
                body = json.dumps(request, separators=(",", ":")).encode()
                stream.sendall(struct.pack("!I", len(body)) + body)
                size = struct.unpack("!I", read_exact(stream, 4))[0]
                require(0 < size <= MAX_FRAME, "The public HELLO frame exceeds its reviewed bound")
                response = json.loads(read_exact(stream, size), object_pairs_hook=unique_object)
                verify_response(response, request_id)
                report.update(status="PASS", buildAndProtocolVerified=True,
                              missingRoomKeyRejected=True, deniedResponseAuthorityBlank=True,
                              requestsSubmitted=1)
    except Exception as error:
        report.update(status="FAIL", error=type(error).__name__,
                      message=str(error) if isinstance(error, Refusal) else
                      "The fixed public TLS endpoint did not complete its reviewed read-only probe")
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    require(not args.output.exists() and not args.output.is_symlink(), "Use a new public report path")
    args.output.parent.mkdir(parents=True, exist_ok=True)
    report = run()
    args.output.write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report))
    return 0 if report["status"] == "PASS" else 1


if __name__ == "__main__":
    raise SystemExit(main())
