#!/usr/bin/env python3
"""Exercise two independent development Unity players and one native server.

The filesystem driver is compiled only for explicitly opted-in development players.
Gameplay uses the production client router; all server/client data is isolated.
"""
from __future__ import annotations

import argparse
import copy
import ctypes
import hashlib
import json
import os
from pathlib import Path
import secrets
import signal
import socket
import ssl
import struct
import subprocess
import time
import traceback
import uuid

from native_server_acceptance import NativeServer, WireClient, hero


def read_json(path):
    try:
        return json.loads(path.read_text())
    except (FileNotFoundError, json.JSONDecodeError):
        return None


def await_value(label, operation, predicate=lambda x: bool(x), timeout=30):
    deadline = time.monotonic() + timeout
    value = None
    while time.monotonic() < deadline:
        value = operation()
        if predicate(value):
            return value
        time.sleep(.08)
    raise TimeoutError(label + ": " + json.dumps(value, default=str)[:1500])


class Client:
    def __init__(self, executable, output, hero_id, port, room, display, authority, *, security=None, reject_probe=False):
        self.executable, self.output, self.hero = executable, output, hero_id
        self.port, self.room, self.display, self.authority = port, room, display, authority
        self.process = None
        self.runs = 0
        self.security, self.reject_probe = security or {}, reject_probe
        output.mkdir()
        for name in ["config", "cache", "data", "tmp", "requests", "responses"]:
            (output / name).mkdir()
        (output / ".ascendance-native-client").write_text("isolated-native-0.10.2c\n")

    def start(self):
        self.runs += 1
        (self.output / "state.json").unlink(missing_ok=True)
        player_environment = {k: v for k, v in os.environ.items() if k not in {
            "DIGITALOCEAN_ACCESS_TOKEN", "GH_TOKEN", "GITHUB_TOKEN", "OPENAI_API_KEY",
            "UNITY_SERVICE_ACCOUNT_ID", "UNITY_SERVICE_ACCOUNT_SECRET",
            "ASCENDANCE_TLS", "ASCENDANCE_CERT_SHA256", "ASCENDANCE_ROOM_ACCESS_KEY",
            "ASCENDANCE_TLS_PFX_PATH", "ASCENDANCE_TLS_PFX_PASSWORD_FILE", "ASCENDANCE_ROOM_ACCESS_KEY_FILE",
        }}
        self.process = subprocess.Popen(
            [str(self.executable), "-ascendance-connect", "127.0.0.1:" + str(self.port),
             "-room", self.room, "-player", "Native " + self.hero,
             "-asc-native-output", str(self.output), "-asc-native-hero", self.hero,
             "-asc-native-reject-probe", "1" if self.reject_probe else "0",
             "-screen-width", "800", "-screen-height", "600", "-screen-fullscreen", "0",
             "-force-glcore", "-logFile", str(self.output / ("client-" + str(self.runs) + ".log"))],
            cwd=self.executable.parent,
            env={**player_environment, "DISPLAY": self.display, "XAUTHORITY": str(self.authority),
                 "ASCENDANCE_NATIVE_ACCEPTANCE": "1", "XDG_CONFIG_HOME": str(self.output / "config"),
                 "XDG_DATA_HOME": str(self.output / "data"), "XDG_CACHE_HOME": str(self.output / "cache"),
                 "TMPDIR": str(self.output / "tmp"), "LIBGL_ALWAYS_SOFTWARE": "1", **self.security},
            stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT,
        )
        if self.reject_probe:
            return self.wait("negative native handshake completed", lambda x: bool(x.get("ConnectionError")), 35)
        return self.wait("native player initialized", lambda x: x.get("Ready") and x.get("HasHud") and x.get("PlayerId") and x.get("AvatarCount") == 2, 50)

    def state(self):
        if self.process and self.process.poll() is not None:
            raise RuntimeError(self.hero + " native player exited: " + str(self.process.returncode))
        return read_json(self.output / "state.json") or {}

    def wait(self, label, predicate, timeout=30):
        return await_value(self.hero + " " + label, self.state, predicate, timeout)

    def request(self, operation="send", **fields):
        request_id = uuid.uuid4().hex
        target = self.output / "requests" / (request_id + ".json")
        temporary = target.with_suffix(".tmp")
        temporary.write_text(json.dumps({"Id": request_id, "Operation": operation, **fields}))
        temporary.rename(target)
        return await_value(self.hero + " command " + str(fields.get("Type", operation)),
                           lambda: read_json(self.output / "responses" / (request_id + ".json")), timeout=30)

    def stop(self):
        if self.process and self.process.poll() is None:
            try:
                self.request("quit")
                self.process.wait(timeout=6)
            except Exception:
                self.process.terminate()
                try:
                    self.process.wait(timeout=6)
                except subprocess.TimeoutExpired:
                    self.process.kill()
                    self.process.wait()
        self.process = None


def body(state, hero_id):
    return next(h for h in state["Heroes"] if h["Id"] == hero_id)


def visible_state(state):
    """Persisted gameplay fields; UI selection/presence/notification revisions are separate."""
    return {"heroes": [{k: h[k] for k in ["Id", "Health", "Actions", "Potions", "LocationId", "TurnCompleted", "Hand", "Discard"]}
                       for h in sorted(state["Heroes"], key=lambda h: h["Id"])],
            "turn": state["TurnHeroId"], "round": state["Round"], "gold": state["Gold"],
            "enemies": [{k: e[k] for k in ["Id", "Health", "LocationId"]} for e in state["Enemies"]]}


class SecureWireClient(WireClient):
    def __init__(self, port, room, certificate, access_key):
        context = ssl.create_default_context(cafile=str(certificate))
        self.socket = context.wrap_socket(socket.create_connection(("127.0.0.1", port), timeout=5), server_hostname="127.0.0.1")
        self.socket.settimeout(10)
        self.room, self.player, self.access_key = room, None, access_key

    def exchange(self, operation, **fields):
        return super().exchange(operation, RoomAccessKey=self.access_key, **fields)


def tls_fixture(output):
    """Create a disposable CA-trusted/pinned self-signed localhost identity."""
    directory = output / "security"; directory.mkdir(mode=0o700)
    key, certificate, pfx = [directory / name for name in ["server-key.pem", "server-cert.pem", "server.pfx"]]
    password, access = secrets.token_hex(32), secrets.token_hex(32)
    password_file, access_file = directory / "password", directory / "access-key"
    password_file.write_text(password); access_file.write_text(access)
    subprocess.run(["openssl", "req", "-x509", "-newkey", "rsa:2048", "-nodes", "-sha256", "-days", "2",
                    "-subj", "/CN=localhost", "-addext", "subjectAltName=IP:127.0.0.1,DNS:localhost",
                    "-keyout", str(key), "-out", str(certificate)], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    subprocess.run(["openssl", "pkcs12", "-export", "-in", str(certificate), "-inkey", str(key), "-out", str(pfx),
                    "-passout", "file:" + str(password_file), "-keypbe", "PBE-SHA1-3DES", "-certpbe", "PBE-SHA1-3DES", "-macalg", "sha1"],
                   check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    for path in directory.iterdir(): path.chmod(0o600)
    der = ssl.PEM_cert_to_DER_cert(certificate.read_text())
    pin = hashlib.sha256(der).hexdigest().upper()
    return {"ASCENDANCE_TLS_PFX_PATH": str(pfx), "ASCENDANCE_TLS_PFX_PASSWORD_FILE": str(password_file),
            "ASCENDANCE_ROOM_ACCESS_KEY_FILE": str(access_file)}, {
                "ASCENDANCE_TLS": "1", "ASCENDANCE_CERT_SHA256": pin, "ASCENDANCE_ROOM_ACCESS_KEY": access}, certificate


def unfocus_and_arrange(display, authority, player_pids):
    """Find actual native windows recursively by PID, arrange them and remove focus."""
    previous = os.environ.get("XAUTHORITY")
    os.environ["XAUTHORITY"] = str(authority)
    connection = None
    try:
        x = ctypes.CDLL("libX11.so.6")
        pointer, window = ctypes.c_void_p, ctypes.c_ulong
        x.XOpenDisplay.argtypes = [ctypes.c_char_p]; x.XOpenDisplay.restype = pointer
        x.XDefaultRootWindow.argtypes = [pointer]; x.XDefaultRootWindow.restype = window
        x.XQueryTree.argtypes = [pointer, window, ctypes.POINTER(window), ctypes.POINTER(window), ctypes.POINTER(ctypes.POINTER(window)), ctypes.POINTER(ctypes.c_uint)]
        x.XInternAtom.argtypes = [pointer, ctypes.c_char_p, ctypes.c_int]; x.XInternAtom.restype = window
        x.XGetWindowProperty.argtypes = [pointer, window, window, ctypes.c_long, ctypes.c_long, ctypes.c_int, window,
                                        ctypes.POINTER(window), ctypes.POINTER(ctypes.c_int), ctypes.POINTER(ctypes.c_ulong),
                                        ctypes.POINTER(ctypes.c_ulong), ctypes.POINTER(ctypes.POINTER(ctypes.c_ubyte))]
        x.XGetGeometry.argtypes = [pointer, window, ctypes.POINTER(window), ctypes.POINTER(ctypes.c_int), ctypes.POINTER(ctypes.c_int),
                                  ctypes.POINTER(ctypes.c_uint), ctypes.POINTER(ctypes.c_uint), ctypes.POINTER(ctypes.c_uint), ctypes.POINTER(ctypes.c_uint)]
        x.XMoveResizeWindow.argtypes = [pointer, window, ctypes.c_int, ctypes.c_int, ctypes.c_uint, ctypes.c_uint]
        x.XSetInputFocus.argtypes = [pointer, window, ctypes.c_int, ctypes.c_ulong]
        x.XGetInputFocus.argtypes = [pointer, ctypes.POINTER(window), ctypes.POINTER(ctypes.c_int)]
        x.XSync.argtypes = [pointer, ctypes.c_int]; x.XCloseDisplay.argtypes = [pointer]
        x.XFree.argtypes = [pointer]
        connection = x.XOpenDisplay(display.encode())
        if not connection: raise RuntimeError("Could not authenticate to private X display")
        root = x.XDefaultRootWindow(connection)
        pid_atom = x.XInternAtom(connection, b"_NET_WM_PID", 0)

        def geometry(w):
            root_return, left, top = window(), ctypes.c_int(), ctypes.c_int()
            width, height, border, depth = [ctypes.c_uint() for _ in range(4)]
            if not x.XGetGeometry(connection, w, ctypes.byref(root_return), ctypes.byref(left), ctypes.byref(top),
                                  ctypes.byref(width), ctypes.byref(height), ctypes.byref(border), ctypes.byref(depth)):
                raise RuntimeError("Could not read actual native window geometry")
            return {"x": left.value, "y": top.value, "width": width.value, "height": height.value}

        def pid_of(w):
            actual, fmt, count, after = window(), ctypes.c_int(), ctypes.c_ulong(), ctypes.c_ulong()
            data = ctypes.POINTER(ctypes.c_ubyte)()
            try:
                status = x.XGetWindowProperty(connection, w, pid_atom, 0, 1, 0, 0, ctypes.byref(actual), ctypes.byref(fmt),
                                             ctypes.byref(count), ctypes.byref(after), ctypes.byref(data))
                if status == 0 and fmt.value == 32 and count.value and data:
                    return ctypes.cast(data, ctypes.POINTER(ctypes.c_ulong))[0]
            finally:
                if data: x.XFree(ctypes.cast(data, pointer))
            return None

        def discover():
            found = {}
            def visit(w, outer=None, depth=0):
                if depth > 10: raise RuntimeError("Unexpected native X window nesting")
                pid = pid_of(w)
                if pid in player_pids:
                    candidate = {"pid": pid, "window": w, "outer": outer or w, **geometry(w)}
                    if pid not in found or candidate["width"] * candidate["height"] > found[pid]["width"] * found[pid]["height"]:
                        found[pid] = candidate
                children, count, root_return, parent = ctypes.POINTER(window)(), ctypes.c_uint(), window(), window()
                if x.XQueryTree(connection, w, ctypes.byref(root_return), ctypes.byref(parent), ctypes.byref(children), ctypes.byref(count)):
                    try:
                        for index in range(count.value):
                            child = children[index]
                            visit(child, child if w == root else outer, depth + 1)
                    finally:
                        if children: x.XFree(ctypes.cast(children, pointer))
            visit(root)
            return found

        found = await_value("Actual native X windows for both process IDs", discover,
                            lambda f: set(f) == set(player_pids), timeout=10)
        windows = []
        for index, pid in enumerate(player_pids):
            entry = found[pid]
            x.XMoveResizeWindow(connection, entry["outer"], 800 * index, 0, 800, 600)
            if entry["window"] != entry["outer"]:
                x.XMoveResizeWindow(connection, entry["window"], 0, 0, 800, 600)
        x.XSync(connection, 0)
        for index, pid in enumerate(player_pids):
            entry = found[pid]
            inner, outer = geometry(entry["window"]), geometry(entry["outer"])
            if [inner["width"], inner["height"], outer["x"], outer["y"], outer["width"], outer["height"]] != [800, 600, 800 * index, 0, 800, 600]:
                raise AssertionError("Native window geometry does not match the real two-window fixture: " + repr((inner, outer)))
            windows.append({"pid": pid, "window": entry["window"], "outerWindow": entry["outer"], "innerGeometry": inner, "outerGeometry": outer})
        x.XSetInputFocus(connection, root, 1, 0)
        x.XSync(connection, 0)
        focused, revert = window(), ctypes.c_int()
        x.XGetInputFocus(connection, ctypes.byref(focused), ctypes.byref(revert))
        if focused.value != root: raise AssertionError("Real X input focus did not leave both players")
        return {"focusedRoot": True, "nativeWindows": windows}
    finally:
        if connection: x.XCloseDisplay(connection)
        if previous is None: os.environ.pop("XAUTHORITY", None)
        else: os.environ["XAUTHORITY"] = previous


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--server-build-dir", type=Path, required=True)
    parser.add_argument("--client-executable", type=Path, required=True)
    parser.add_argument("--xvfb", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--tls", action="store_true", help="Use pinned TLS and room invitation, including real native rejection probes")
    args = parser.parse_args()
    output = args.output.resolve()
    output.mkdir(mode=0o700)  # Never reuse another campaign or an earlier result.
    room = "NATIVE-ACCEPTANCE-" + uuid.uuid4().hex
    server_root = output / "server"; server_root.mkdir()
    server_security, client_security, certificate = {}, {}, None
    if args.tls: server_security, client_security, certificate = tls_fixture(output)
    factory = (lambda port, requested_room: SecureWireClient(port, requested_room, certificate, client_security["ASCENDANCE_ROOM_ACCESS_KEY"])) if args.tls else WireClient
    server = NativeServer(args.server_build_dir.resolve(), server_root, room, environment=server_security, client_factory=factory)
    report = {"kind": "independent-native-Unity-players", "room": room, "checks": [],
              "boundaries": ["Development test driver; shipping build excludes it.",
                             "Synthetic local fixture; no production data.",
                             "Native server wire retry/duplicate coverage is a separate suite."]}
    clients, rejection_clients = [], []
    observer = None
    xvfb = None

    def check(label, expected, actual):
        row = {"name": label, "expected": expected, "actual": actual, "passed": expected == actual}
        report["checks"].append(row)
        print(("PASS " if row["passed"] else "FAIL ") + label, flush=True)
        if not row["passed"]:
            raise AssertionError(label + ": " + json.dumps(row))

    def accepted(client, command, **fields):
        reply = client.request(Type=command, **fields)
        check(client.hero + " " + command + " accepted through production router", True, reply["Accepted"])
        return reply["State"]

    try:
        observer, initial = server.start()
        check("Fresh native server HELLO reports 0.10.2c", "0.10.2c", initial.get("ServerBuildVersion"))
        observer.close(); observer = None; server.stop()
        fixture = json.loads(server.save_path.read_text())
        campaign = fixture["DurableCampaign"]
        campaign["PartyHeroIds"] = ["Aldric", "Rowan"]
        campaign["VillagersRescued"] = 4
        victim = next(e for e in campaign["Enemies"] if e["TemplateId"] == "E001")
        victim.update({"Health": 6, "MaxHealth": 6, "Armor": 1, "LocationId": "L000", "Statuses": []})
        campaign["Enemies"] = [victim]
        campaign["QuestRuntime0829"]["QuestOwnedEnemyIds"] = [victim["Id"]]
        for h in campaign["Heroes"]:
            if h["Id"] in ["Aldric", "Rowan"]:
                h.update({"LocationId": "L000", "Actions": 0, "TurnCompleted": False, "HealingPotions": 1})
            if h["Id"] == "Rowan": h["Health"] = 4
        next(loc for loc in campaign["Locations"] if loc["Id"] == "L001").update(
            {"Revealed": True, "RevealResolved": True, "FirstVisitResolved": True, "RevealEffectDeferred": False})
        server.save_path.write_text(json.dumps(fixture))
        observer, _ = server.start()

        display_number = next(n for n in range(191, 240) if not Path("/tmp/.X" + str(n) + "-lock").exists())
        display = ":" + str(display_number)
        authority = output / "Xauthority"
        def field(data): return struct.pack("!H", len(data)) + data
        authority.write_bytes(struct.pack("!H", 65535) + field(b"") + field(str(display_number).encode())
                              + field(b"MIT-MAGIC-COOKIE-1") + field(secrets.token_bytes(16)))
        authority.chmod(0o600)
        with (output / "xvfb.log").open("wb") as log:
            xvfb = subprocess.Popen([str(args.xvfb.resolve()), display, "-screen", "0", "1600x900x24", "-nolisten", "tcp", "-noreset", "-auth", str(authority)], stdout=log, stderr=subprocess.STDOUT)
        await_value("private X display", lambda: Path("/tmp/.X11-unix/X" + str(display_number)).exists(), timeout=10)
        for name in ["Aldric", "Rowan"]:
            client = Client(args.client_executable.resolve(), output / name.lower(), name, server.port, room, display, authority, security=client_security)
            clients.append(client)
            state = client.start()
            check(name + " is a separate GUI player, not a dedicated process", [False, True, name], [state["Dedicated"], state["HasHud"], state["SelectedHeroId"]])
            check(name + " creates exactly the authoritative party avatars", [2, 2], [len(state["Heroes"]), state["AvatarCount"]])
            check(name + " production remote bootstrap enables background updates", True, state["RunInBackground"])
            check(name + " persistent data is isolated", True, state["PersistentDataPath"].startswith(str(client.output / "config") + "/"))
        a, b = clients
        check("Three distinct native OS process IDs", 3, len({server.process.pid, a.process.pid, b.process.pid}))
        check("Separate native clients claim distinct Hero sessions", True, a.state()["PlayerId"] != b.state()["PlayerId"])
        if args.tls:
            for name, overrides, expected in [
                ("wrong-pin", {"ASCENDANCE_CERT_SHA256": "00" * 32}, "Server certificate pin did not match."),
                ("wrong-invite", {"ASCENDANCE_ROOM_ACCESS_KEY": "00" * 32}, "Room access key was rejected."),
            ]:
                bad = Client(args.client_executable.resolve(), output / name, "Aldric", server.port, room, display, authority,
                             security={**client_security, **overrides}, reject_probe=True)
                rejection_clients.append(bad)
                state = bad.start()
                check("Native " + name + " reports actual security rejection", [True, False, expected],
                      [state["RejectedConnect"], state["Ready"], state["ConnectionError"]])
                check("Native " + name + " never claims a player", "", state.get("PlayerId") or "")
                bad.stop()
            check("Rejected native connections add no sessions", 2, len(observer.snapshot()["Players"]))
            report["transport"] = "TLS 1.2 with certificate pin and room invitation"
        report["focus"] = unfocus_and_arrange(display, authority, [client.process.pid for client in clients])
        check("Two actual native process windows have independent 800 by 600 geometry", [[800, 600], [800, 600]],
              [[w["innerGeometry"]["width"], w["innerGeometry"]["height"]] for w in report["focus"]["nativeWindows"]])
        check("X input focus is outside both native Unity players", True, report["focus"]["focusedRoot"])
        for client in clients:
            frame = client.state()["Frame"]
            client.wait("advances real unfocused frames", lambda s: s.get("Frame", 0) > frame + 10)
            check(client.hero + " native game advances while unfocused", True, client.state()["Frame"] > frame + 10)

        state = accepted(a, "StartTurn")
        check("Native StartTurn grants three Actions", [3, "Aldric"], [body(state, "Aldric")["Actions"], state["TurnHeroId"]])
        b.wait("sees Aldric turn", lambda s: s.get("TurnHeroId") == "Aldric" and body(s, "Aldric")["Actions"] == 3)
        rejected = b.request(Type="StartTurn")
        check("Concurrent other-Hero turn is rejected", [False, "RULE_REJECTED"], [rejected["Accepted"], rejected["Code"]])

        state = accepted(a, "UseHealingPotion", TargetId="Rowan")
        check("Potion heals exactly four and spends Action/item", [8, 2, 0], [body(state, "Rowan")["Health"], body(state, "Aldric")["Actions"], body(state, "Aldric")["Potions"]])
        b.wait("receives actual Potion effect", lambda s: body(s, "Rowan")["Health"] == 8 and body(s, "Aldric")["Actions"] == 2)
        for after_actions, after_health in [(1, 3), (0, 0)]:
            state = accepted(a, "BasicAttack", TargetId=victim["Id"])
            actual_enemy = next((e["Health"] for e in state["Enemies"] if e["Id"] == victim["Id"]), 0)
            check("Native Attack exact damage and cost " + str(after_actions), [after_actions, after_health], [body(state, "Aldric")["Actions"], actual_enemy])
            b.wait("observes exact Attack effect", lambda s: body(s, "Aldric")["Actions"] == after_actions and next((e["Health"] for e in s["Enemies"] if e["Id"] == victim["Id"]), 0) == after_health)
        accepted(a, "EndTurn")
        a.wait("zero-Action turn ended explicitly", lambda s: not s.get("TurnHeroId"))
        state = accepted(b, "StartTurn")
        check("Second native Hero receives own three Actions", [3, "Rowan"], [body(state, "Rowan")["Actions"], state["TurnHeroId"]])
        accepted(b, "Move", TargetId="L001")
        for client in clients:
            client.wait("observes committed movement", lambda s: body(s, "Rowan")["LocationId"] == "L001" and not body(s, "Rowan")["Moving"])
            check(client.hero + " sees exact movement Action cost", 2, body(client.state(), "Rowan")["Actions"])

        before = visible_state(a.state())
        a_id = a.state()["PlayerId"]
        a.stop(); a.start()
        report["restartedFocus"] = unfocus_and_arrange(display, authority, [client.process.pid for client in clients])
        check("Restarted native client retains Hero ownership", a_id, a.state()["PlayerId"])
        check("Restarted native client restores canonical gameplay", before, visible_state(a.state()))

        idle_start = time.monotonic()
        idle_frames = {client.hero: client.state()["Frame"] for client in clients}
        time.sleep(36)
        report["idleSeconds"] = time.monotonic() - idle_start
        check("Two native clients remain connected beyond 35 seconds", [True, True], [a.state()["Connected"], b.state()["Connected"]])
        report["idleFrameProgress"] = {client.hero: client.state()["Frame"] - idle_frames[client.hero] for client in clients}
        for client in clients:
            check(client.hero + " main loop stays live throughout unfocused idle", True, report["idleFrameProgress"][client.hero] > 30)
            check(client.hero + " idle observation is fresh", True, time.time() - (client.output / "state.json").stat().st_mtime < 3)
        check("Idle native peer state is stable", before, visible_state(b.state()))

        old_instance = a.state()["ServerInstanceId"]
        server_before = observer.snapshot()
        observer.close(); observer = None; server.stop()
        saved_before = json.loads(server.save_path.read_text())
        check("Actual native save persists Rowan's two remaining Actions", 2, next(h for h in saved_before["DurableCampaign"]["Heroes"] if h["Id"] == "Rowan")["Actions"])
        observer, restarted = server.start()
        check("Native restart creates a new process identity", True, restarted["Snapshot"]["ServerInstanceId"] != old_instance)
        for client in clients:
            client.wait("accepts new server instance", lambda s: s.get("Ready") and bool(s.get("ServerInstanceId")) and s["ServerInstanceId"] != old_instance, 35)
            reply = client.request("reconnect")
            check(client.hero + " reconnects after native server restart", True, reply["Accepted"])
            check(client.hero + " observes the actual restarted server identity", restarted["Snapshot"]["ServerInstanceId"], reply["State"]["ServerInstanceId"])
            check(client.hero + " sees persisted exact gameplay after server restart", before, visible_state(client.state()))
        state = accepted(b, "Move", TargetId="L000")
        for client in clients:
            client.wait("moves after persisted restart", lambda s: body(s, "Rowan")["LocationId"] == "L000" and not body(s, "Rowan")["Moving"])
            check(client.hero + " sees post-restart Action cost once", 1, body(client.state(), "Rowan")["Actions"])
            check(client.hero + " has no Unity errors", [], client.state()["Errors"])
        try:
            subprocess.run(["import", "-display", display, "-window", "root", str(output / "native-clients.png")],
                           env={**os.environ, "XAUTHORITY": str(authority)}, check=True, timeout=15, stdout=subprocess.DEVNULL, stderr=subprocess.PIPE)
            report["screenshot"] = "native-clients.png"
        except Exception as error:
            report["visualCapture"] = {"status": "UNSUPPORTED", "reason": str(error)}
        report["nativeProcesses"] = {"server": server.process.pid, "Aldric": a.process.pid, "Rowan": b.process.pid}
        report["passed"] = True
    except Exception:
        report["passed"] = False
        report["error"] = traceback.format_exc()
        for client in clients:
            try: report[client.hero + "State"] = client.state()
            except Exception as error: report[client.hero + "Error"] = str(error)
        print(report["error"], flush=True)
    finally:
        cleanup_errors = []
        for client in clients + rejection_clients:
            try: client.stop()
            except Exception as error: cleanup_errors.append("Player cleanup: " + str(error))
        if observer:
            try: observer.close()
            except Exception as error: cleanup_errors.append("Observer cleanup: " + str(error))
        try: server.stop()
        except Exception as error: cleanup_errors.append("Server cleanup: " + str(error))
        if xvfb and xvfb.poll() is None:
            xvfb.terminate()
            try: xvfb.wait(timeout=8)
            except subprocess.TimeoutExpired:
                xvfb.kill(); xvfb.wait()
                cleanup_errors.append("Private X server required forced shutdown")
        if cleanup_errors:
            report["cleanupErrors"] = cleanup_errors
            report["passed"] = False
        report["passedChecks"] = sum(row["passed"] for row in report["checks"])
        report["failedChecks"] = sum(not row["passed"] for row in report["checks"])
        (output / "results.json").write_text(json.dumps(report, indent=2) + "\n")
    return 0 if report.get("passed") else 1


if __name__ == "__main__":
    raise SystemExit(main())
