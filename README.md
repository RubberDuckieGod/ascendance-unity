# Ascendance Unity 0.10.2c

This checkout contains the recovered Unity project: the uploaded Assets baseline has 156 C# scripts, MainMenu and Stonebrook scenes, four authored JSON catalogs, art, and original asset metadata. Packages and ProjectSettings pin **Unity 6000.6.5f1**. New validation and deployment helpers are maintained alongside that baseline.

The game version remains **0.10.2c**. Identify reliability revisions by Git commit and the build's SHA-256 manifest. The source destination is the public [RubberDuckieGod/ascendance-unity](https://github.com/RubberDuckieGod/ascendance-unity) repository; the owner explicitly authorized public publication.

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

Adjust the Editor path if Unity Hub installed it elsewhere. No cloud Unity license is required when running with your existing local licensed Editor. Install the **Linux Dedicated Server** and **Windows Build Support (IL2CPP)** modules in Hub before requesting those builds. A compiler/toolchain may also be required by your configured scripting backend.

With a licensed Linux Editor available, the equivalent validation wrapper is:

```bash
bash tools/run_unity_validation.sh \
  --editor /absolute/path/to/6000.6.5f1/Editor/Unity
```

Add `--build-linux-server` to request the Linux server build after validation. Keep the full build output, including its data directory and UnityPlayer library. The wrapper records logs and results under `artifacts/unity-validation/`.

The wrappers create isolated test-save directories and run all six gates: **master 54/0, server foundation 24/0, routing 29/0, transport 27/0, reliability 22/0, snapshot 20/0**. All six gates passed in a licensed cloud Editor run on 2026-10-24, with zero nested errors. See [runtime results](docs/RUNTIME_RESULTS_20261024.json). The inherited Play Mode menu is **Ascendance → Validation → Test 0.10.2c - Real Network Transport (Play Mode)**; it now requires the same explicit isolated-data environment as batch validation. Use the wrapper for the full chain.

The automatic **Source intake** GitHub Action runs static/helper checks. **Licensed Unity validation** is a separate manual workflow for a Windows self-hosted runner labeled `unity-6000.6.5f1`, with an existing licensed Editor and build modules. Configure a protected `unity-validation` environment and its `UNITY_EDITOR_PATH` variable. Review the selected commit before dispatching it; public pull requests never automatically execute on that machine. This workflow becomes dispatchable after it is present on the default branch. Neither workflow deploys a server.

Windows clients build through **Ascendance → Build → Build Windows Standalone 0.10.2c**. Server builds use **Build Linux Dedicated Server 0.10.2c**. The cloud run compiled the project and built complete Linux dedicated-server and Windows players. Independent native-client acceptance and DigitalOcean activation remain outstanding.

## Multiplayer and DigitalOcean

Clients send length-prefixed UTF-8 JSON over persistent custom TCP connections, normally port **7777**. The dedicated Unity process owns room authority and executes queued gameplay commands on Unity's main thread. Local play uses the same command boundary through a loopback transport.

The supplied transport is plain TCP: reconnect credentials are bearer credentials and are not encrypted on the wire. Do not expose it to untrusted networks without reviewing encrypted transport or a trusted private tunnel. This change adds socket-bound ownership and reconnect reliability, not TLS or player-account authentication. Upgrade client and server together for the new revision/snapshot behavior.

Example arguments for an already verified client build:

```text
-ascendance-connect SERVER_HOST:7777 -room ROOM_ID -player PLAYER_NAME
```

The [DigitalOcean deployment tools](Deploy/DigitalOcean/README.md) support reviewed Linux release artifacts, separate persistent data, and checked activation/rollback. Their fixture tests use fake services and synthetic artifacts. No production deployment or live multiplayer acceptance has been completed by adding these tools. An existing server requires inspection of its service, actual save path, and backups before migration.

## Source and history

[Source provenance](docs/SOURCE_PROVENANCE.md) identifies both archives used to recover this project. The [uploaded README](docs/history/README_uploaded.md) is preserved verbatim as historical material. Existing changelogs, validation reports, and historical Python verifiers remain in place; their saved PASS lines are not current test results.

Commit source and original `.meta` files. Keep generated Library/Logs/UserSettings, personal launchers, hardcoded updaters, runtime saves, reconnect tokens, licenses, and credentials outside Git. The ignore rules cover these common paths; inspect staged content before sharing.
