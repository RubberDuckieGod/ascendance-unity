# Ascendance Unity 0.10.2c

This checkout contains the recovered Unity project: the uploaded Assets baseline has 156 C# scripts, MainMenu and Stonebrook scenes, four authored JSON catalogs, art, and original asset metadata. Packages and ProjectSettings pin **Unity 6000.6.5f1**. New validation and deployment helpers are maintained alongside that baseline.

The game version remains **0.10.2c**. Identify reliability revisions by Git commit and the build's SHA-256 manifest. The source destination is the public [RubberDuckieGod/ascendance-unity](https://github.com/RubberDuckieGod/ascendance-unity) repository; the owner explicitly authorized public publication.

## Multiplayer rollout status

**The secure DigitalOcean upgrade is not deployed.** Source `eb51b2292cf433a2cbe5dabb4863f1fef993161a` passed all **176 Editor checks**, **91 helper tests** with no skips, **73 plain / 80 TLS native-server checks**, and **77 independent native Unity GUI checks**. Matching Linux server, Linux GUI development client and Windows client builds succeeded. Both GUI windows passed actual world/Hero visibility review after idle and restart. The [matching prerelease](https://github.com/RubberDuckieGod/ascendance-unity/releases/tag/unity-0.10.2c-multiplayer-eb51b22) now includes complete client/server downloads, checksums and detailed validation reports. See the [acceptance summary](docs/MULTIPLAYER_ACCEPTANCE_EB51B22.json). Server activation remains pending.

The existing server's identity, service and actual save location have been inspected; that inspection does not establish that the new build is running. Render hosts a separate JavaScript game. Windows was compiled here; the independent graphical acceptance uses Linux players. Audio output is unvalidated.

The review branch adds encrypted transport, a private invite launcher, native restart repairs, and a staged upgrade for the existing DigitalOcean service. See [validation status](docs/VALIDATION_STATUS.md) and [multiplayer review](docs/MULTIPLAYER_REVIEW.md) for the evidence and remaining gates.

## Playing with friends after activation

Players need the complete matching **Windows game ZIP**, not Unity or the Unity Editor. These steps become usable after the owner completes secure server activation and receives the private invite:

1. Extract the entire Windows ZIP to a new folder. Keep `Ascendance.exe`, `Ascendance_Data`, `UnityPlayer.dll`, the other libraries, and both `Play-AscendanceMultiplayer` launcher files together.
2. Obtain `Ascendance.invite.json` privately from the server owner. Put it beside `Play-AscendanceMultiplayer.cmd`, or supply its full path when the launcher asks. The invite contains the endpoint, room, pinned certificate and private access key; never upload it to GitHub or paste it into chat.
3. Run **Play-AscendanceMultiplayer.cmd** and enter your own player name. Every friend uses a distinct name. Keep that name and the same Windows user profile for later reconnects.
4. In the game, choose **NEW GAME**, select a Hero that is available in the server's existing party, then choose **BEGIN ADVENTURE**. Each player must control a different Hero. Local party selection does not replace the server campaign or add Heroes to its party.

The room supports at most **four player sessions**. A fresh default party is Garruk, Rowan, Aldric and Elara; an existing campaign may have a different party. If a Hero is already claimed, use your original session to reconnect or select an unclaimed Hero. Do not delete reconnect data to work around ownership checks. Friends use the same private invite and matching client build; a Render URL or browser room code cannot connect this Unity client.

## Setup and local checks

Open this directory with a licensed Unity 6000.6.5f1 Editor. Install the matching Linux Dedicated Server build support for server builds and Windows standalone support for Windows clients. Unity restores the pinned packages and regenerates Library on first import; the uploaded Library cache is unnecessary.

From the project root, use Python 3.11+ for these independent checks:

```bash
python3 -B tools/project_intake.py --require-source --report docs/INTAKE_STATUS.json
python3 -B -m unittest discover -s tests -p 'test_*.py' -v
```

The intake checker verifies metadata, package-lock consistency, minimum source/data presence, and asset GUIDs. It parses historical Python verifiers without executing them. Fixture tests check the new helper behavior. These checks do not run Unity or certify gameplay. See [validation status](docs/VALIDATION_STATUS.md) and the [machine-readable intake report](docs/INTAKE_STATUS.json).

## Unity validation and builds

On your Windows computer, close this project in the Editor, then run from its root in PowerShell:

```powershell
& .\tools\Run-UnityValidation.ps1 -Editor 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe' -BuildLinuxServer -BuildWindows
```

Adjust the Editor path if Unity Hub installed it elsewhere. No cloud Unity license is required when running with your existing local licensed Editor. Install the matching **Linux Dedicated Server** and **Windows Build Support** modules in Hub before requesting those builds. A compiler/toolchain may also be required by your configured scripting backend.

With a licensed Linux Editor available, the equivalent validation wrapper is:

```bash
bash tools/run_unity_validation.sh \
  --editor /absolute/path/to/6000.6.5f1/Editor/Unity
```

Add `--build-linux-server` to request the Linux server build after validation. Keep the full build output, including its data directory and UnityPlayer library. The wrapper records logs and results under `artifacts/unity-validation/`.

The wrappers create isolated test-save directories and run all six gates: **master 54/0, server foundation 24/0, routing 29/0, transport 27/0, reliability 22/0, snapshot 20/0**. All six passed for clean source `eb51b2292cf433a2cbe5dabb4863f1fef993161a` with zero nested errors, using licensed Unity 6000.6.5f1. Logs and results are retained under `artifacts/unity-validation/release-eb51b22-20261025T233739Z/`. These Editor checks are separate from independent native-client acceptance. The [2026-10-24 report](docs/RUNTIME_RESULTS_20261024.json) remains evidence for the earlier `9792d16` revision only. The inherited Play Mode menu is **Ascendance → Validation → Test 0.10.2c - Real Network Transport (Play Mode)**; it requires the same explicit isolated-data environment as batch validation. Use the wrapper for the full chain.

The automatic **Source intake** GitHub Action runs static/helper checks. **Licensed Unity validation** is a separate manual workflow for a Windows self-hosted runner labeled `unity-6000.6.5f1`, with an existing licensed Editor and build modules. Configure a protected `unity-validation` environment and its `UNITY_EDITOR_PATH` variable. Review the selected commit before dispatching it; public pull requests never automatically execute on that machine. This workflow becomes dispatchable after it is present on the default branch. Neither workflow deploys a server.

The manual **Native multiplayer release acceptance** workflow verifies reviewed release hashes and source provenance, then runs the compiled Linux server's plain/TLS protocol suites in isolated data on a hosted Linux runner. It does not build Unity, run graphical clients or deploy. The current native runtime evidence was produced locally, not by that hosted workflow.

Windows clients build through **Ascendance → Build → Build Windows Standalone 0.10.2c**. Server builds use **Build Linux Dedicated Server 0.10.2c**. The final builds used clean source `eb51b2292cf433a2cbe5dabb4863f1fef993161a`; build provenance and application-input hashes confirm that the source stayed unchanged. The development test driver is absent from the shipping Windows player and dedicated server. Later documentation-only commits do not change the runtime revision embedded in these artifacts. Native server/protocol checks, two independent Unity GUI clients, Windows compilation and DigitalOcean activation are separate acceptance gates.

## Multiplayer and DigitalOcean

Clients send length-prefixed UTF-8 JSON over persistent custom TCP connections, normally port **7777**. The dedicated Unity process owns room authority and executes queued gameplay commands on Unity's main thread. Local play uses the same command boundary through a loopback transport. Online clients keep processing in the background so an unfocused window can receive authoritative updates.

TLS is optional in the transport for trusted local tests, and **required by the DigitalOcean upgrade**. Secure clients use TLS 1.2, require the exact SHA-256 certificate pin, and check the certificate's hostname and validity period. A 64-character hexadecimal room access key accompanies every RPC, including the initial HELLO, SNAPSHOT and PING. A failed secure connection never retries as plaintext. The shared room key controls access; it is not an individual player-account system. Socket-bound Hero ownership and reconnect credentials remain separate checks.

Use the private-invite launcher for online play. It passes the access key only through the child process environment, never through command-line arguments, public launchers or URLs. Server TLS requires all three private file settings together: `ASCENDANCE_TLS_PFX_PATH`, `ASCENDANCE_TLS_PFX_PASSWORD_FILE` and `ASCENDANCE_ROOM_ACCESS_KEY_FILE`; partial configuration fails closed. With all three absent, trusted local fixtures retain plain TCP.

The inspected existing target is `159.65.100.206:7777`, room `ASC-PRIMARY`, service `ascendance`. Its persistent Unity data is `/var/lib/ascendance/.config/unity3d/Ascendance/Ascendance`, including `AscendanceServerRooms0100`. The upgrade preserves that identity and data path, stages immutable code separately, and verifies an isolated TLS native-server canary before stopping the existing service. It checks that players have disconnected, creates verified code/configuration/data backups and a consistent data backup after a graceful stop, then activates and checks the secured service. Recovery restores prior code/configuration; it does not silently replace campaign saves.

This cloud workspace cannot carry SSH through its network proxy; the owner's Windows SSH connection works. One pinned owner-side PowerShell command will download and verify the matching client and server, stage the release, run the private canary, back up and activate it through that existing SSH connection. It retrieves the private invite using SCP and applies a Windows ACL restricted to the owner. SSH private keys, passphrases, TLS private material, access keys and private invites stay out of Git and chat. Share the game ZIP and the private invite with trusted friends privately.

Public inbound **TCP 7777** is opened only after verified secure activation. SSH remains restricted to the owner's home address; no UDP port is needed. Any later rollback to the old plaintext server must first restrict public game access. See the [deployment tools](Deploy/DigitalOcean/README.md) for the underlying artifact helpers. A staged artifact, helper fixture pass or successful port connection does not prove a production upgrade or correct gameplay.

## Source and history

[Source provenance](docs/SOURCE_PROVENANCE.md) identifies both archives used to recover this project. The [uploaded README](docs/history/README_uploaded.md) is preserved verbatim as historical material. Existing changelogs, validation reports, and historical Python verifiers remain in place; their saved PASS lines are not current test results.

Commit source and original `.meta` files. Keep generated Library/Logs/UserSettings, personal launchers, hardcoded updaters, runtime saves, reconnect tokens, licenses, and credentials outside Git. The ignore rules cover these common paths; inspect staged content before sharing.
