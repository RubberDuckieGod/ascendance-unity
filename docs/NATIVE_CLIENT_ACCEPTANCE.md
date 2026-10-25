# Independent native Unity multiplayer acceptance

`tools/native_unity_clients.py` launches a compiled Linux dedicated server and two separate graphical Unity development players. Gameplay requests use the production client command router and TCP/TLS transport. These checks complement the Editor gates and `native_server_acceptance.py`; they do not certify every card, full visual interaction, Windows execution, or the public DigitalOcean deployment.

Build the normal Linux server with `Ascendance.EditorTools.AscendanceDedicatedServerBuilder0100.BuildLinuxDedicatedServer0102`. Build a Linux **development player** with `Ascendance.EditorTools.NativeClientAcceptanceBuilder0102.BuildLinuxClient`. Both methods accept `-buildOutput /absolute/executable/path` through Unity's batch build interface. Use the pinned Unity version from `ProjectSettings/ProjectVersion.txt` and build serially in a single project.

The graphical checks require Python 3, Xvfb, OpenSSL, native X11/OpenGL libraries, and a Linux software renderer when no GPU is available. ImageMagick's `import` records the final real desktop image. Run with a new output directory:

```sh
python tools/native_unity_clients.py \
  --server-build-dir /absolute/LinuxServer \
  --client-executable /absolute/LinuxClient/AscendanceClient.x86_64 \
  --xvfb /absolute/path/to/Xvfb \
  --output /absolute/new-native-acceptance-directory \
  --tls
```

The test driver exists only in Editor/development builds and requires explicit opt-in, a loopback endpoint, a synthetic room prefix, a marked directory, and an isolated `XDG_CONFIG_HOME`. It configures windowed display settings through the normal settings API within those temporary preferences. It does not force background execution. The production remote bootstrap must enable background updates for these checks to pass. Do not use `-batchmode` for the two graphical players: the application treats batch processes as dedicated servers.

The harness generates a disposable certificate, certificate pin, invitation key, private X authentication cookie, and synthetic campaign data. It starts from a real native save and prepares two Heroes, one Raider, an injured ally, and an already revealed adjacent location. It never accepts a remote server or an existing campaign directory. Private synthetic credentials stay within its output directory; remove that directory when its evidence is no longer needed.

The expected gameplay and network results include:

- Two distinct graphical processes receive the authoritative party, create its two avatars and HUD, and claim separate Hero sessions.
- Actual native TLS rejects a wrong certificate pin and wrong room invitation without claiming another player.
- Real X windows are identified by process ID, measured at 800 by 600 pixels, arranged separately, and left unfocused. Both main loops must advance.
- Starting Aldric's turn grants three Actions. Rowan cannot start a concurrent turn.
- Aldric's potion restores Rowan from 4 to 8 Health and spends one Action and one potion.
- Aldric's two attacks against the Raider each deal exactly 3 damage and spend one Action. Both replicas observe the costs and damage.
- After explicit EndTurn, Rowan starts with three Actions and moving to the revealed location leaves two. Both clients observe committed movement.
- Restarting a graphical player preserves its Hero ownership and exact canonical gameplay.
- Both unfocused players remain connected for more than 35 seconds, each advances more than 30 frames, and observations remain fresh.
- Restarting the native server preserves the actual saved game. Both clients accept the new server identity, reconnect, and perform a subsequent move with exactly one Action charged.

`results.json` records expected versus actual values and failures. Native logs, isolated saves, actual process/window IDs, frame progress and `native-clients.png` provide supporting evidence. Screenshot capture being unavailable is reported explicitly. A passed command or build alone does not satisfy the gameplay assertions. Retain failed-run evidence when correcting a harness or application defect; use a new directory for every rerun.
