from pathlib import Path
root = Path(__file__).resolve().parents[1]
checks=[]
def check(name, ok):
    checks.append((name, bool(ok)))
check("BuildVersion", 'BuildVersion = "0.10.2c"' in (root/'Assets/Scripts/Core/AscendanceGameManager.cs').read_text())
check("bundleVersion", 'bundleVersion: 0.10.2c' in (root/'ProjectSettings/ProjectSettings.asset').read_text())
for rel, expected in [
    ('Assets/Editor/MasterSaveLoadRegressionTestTools0862.cs',54),
    ('Assets/Editor/DedicatedServerFoundationTestTools0100.cs',24),
    ('Assets/Editor/ClientCommandRoutingTestTools0101.cs',29),
    ('Assets/Editor/NetworkTransportTestTools0102.cs',27),
]:
    s=(root/rel).read_text(); check(f"{Path(rel).name} count", s.count('Check(')-1 == expected)
router=(root/'Assets/Editor/ClientCommandRoutingTestTools0101.cs').read_text()
check("router round drain", 'gm.Round > roundBeforeWorldPhase' in router)
check("router authoritative reaction drain", 'router.RequestResolveReaction()' in router)
check("router stable boundary", 'stableHeroPhaseFrames >= 30' in router)
network=(root/'Assets/Editor/NetworkTransportTestTools0102.cs').read_text()
check("network expects router 29", '29 passed, 0 failed' in network)
check("stable idle", 'stableIdleFrames >= 90' in network)
check("stable start", 'stableStartFrames >= 45' in network)
check("raw TCP", 'SendRawAsync' in network)
check("HELLO", 'Operation = "HELLO"' in network)
check("START TURN", 'Type = ServerCommandType0100.StartTurn' in network)
check("socket assertion", 'START TURN crosses a real TCP socket' in network)
for rel in ['Assets/Scripts/Online/AscendanceTcpServer0102.cs','Assets/Scripts/Online/AscendanceTcpCommandTransport0102.cs','Assets/Scripts/Online/AscendanceNetworkProtocol0102.cs']:
    check(Path(rel).name, (root/rel).exists())
check("changelog", (root/'CHANGELOG_0.10.2c.md').exists())
check("persistent TCP note", (root/'PERSISTENT_TCP_SESSION_HOTFIX_0.10.2c.md').exists())

server=(root/'Assets/Scripts/Online/AscendanceTcpServer0102.cs').read_text()
client=(root/'Assets/Scripts/Online/AscendanceTcpCommandTransport0102.cs').read_text()
check("server no idle disconnect", 'ReceiveTimeout = 0' in server and 'ReceiveTimeout = 35000' not in server)
check("server keepalive", 'SocketOptionName.KeepAlive' in server)
check("client keepalive", 'SocketOptionName.KeepAlive' in client)
check("client reconnect retry", 'for (int attempt = 0; attempt < 2; attempt++)' in client and 'same CommandId once after reconnect' in client)

current=list((root/'Assets').rglob('*.cs'))+[root/'ProjectSettings/ProjectSettings.asset',root/'README.md',root/'Deploy/DigitalOcean/README.md']
check("no stale current 0.10.2b", all('0.10.2b' not in p.read_text(errors='ignore') for p in current))
for name,ok in checks: print(('PASS' if ok else 'FAIL'), name)
print(f"TOTAL {sum(ok for _,ok in checks)}/{len(checks)}")
raise SystemExit(0 if all(ok for _,ok in checks) else 1)
