# Validation status

**The secure DigitalOcean upgrade has not been deployed.** Final validation of the current source and its matching artifacts is pending. The game remains **0.10.2c**; use the full Git revision and build manifests to identify a reliability revision. This Unity implementation is separate from the Render JavaScript game.

## Evidence and its limits

| Check | Evidence currently available | What it establishes |
| --- | --- | --- |
| Recovered source intake | Prior [intake report](INTAKE_STATUS.json), recovered Assets, metadata and pinned package graph. | Static source/settings presence, authored catalogs and asset GUIDs. It executes no gameplay. |
| Static verifiers and helper fixtures | Earlier intake, release, verdict and wrapper checks passed. New secure installer/launcher checks are still being reviewed; final current-source helper totals are pending. | Helper behavior against temporary files, synthetic artifacts and fake services. Counts with skipped PowerShell checks must retain those skips. |
| Prior licensed Editor gates | Commit `9792d169b1141d0dbef043906246f3fe0dab4497`: master **54/0**, foundation **24/0**, routing **29/0**, transport **27/0**, reliability **22/0**, snapshot **20/0**. [Runtime report](RUNTIME_RESULTS_20261024.json). | Actual Editor execution for that revision. Real-socket tests run inside one Editor process; snapshot checks include synthetic presentation states. These are not independent native-client acceptance. |
| Prior native platform builds | Linux dedicated server and Windows client built at `9792d16`; [file provenance](BUILD_PROVENANCE_9792d16.json). Intermediate secure builds also compiled successfully. | Compilation and emitted artifacts for those specific revisions. Neither hashes nor successful compilation prove gameplay or deployment. |
| Final current-source six Editor gates | **PENDING**. Targets remain **54/24/29/27/22/20**, with zero failed checks or nested Unity errors. | Must be rerun after the native persistence/security/background changes and tied to the final source revision. |
| Final current-source platform builds | **PENDING**: Linux dedicated server, normal Linux GUI development client for acceptance, and shipping Windows client. | Matching source/build provenance is required. Windows compilation does not mean the Windows GUI ran here. |
| Actual native Linux server and two Python wire clients | Earlier untampered native restart exposed a real JsonUtility defect. Intermediate plain/TLS runs exercised real gameplay and security but failed preservation cases before the latest repair. Final complete runs are **PENDING**. | Exercises a separately running shipped Unity server over real sockets. Python clients do not certify Unity client UI or presentation. |
| Two independent native Unity GUI clients | Intermediate TLS runs exercised real gameplay and client-process reconnect. The complete final acceptance is **PENDING**, including reliable background/idle observation and controlled server restart. | Requires two actual standalone Unity processes, isolated user data and explicit state/HUD assertions. An Editor fixture or two Python sockets cannot satisfy this gate. |
| Existing DigitalOcean server inspection | Owner's reviewed read-only inspection succeeded: service/user, executable, room, port and actual save path identified; no inspection warnings. | Existing deployment identity and migration prerequisites. No campaign save content was used for testing. |
| Private DigitalOcean native canary | **NOT RUN**. Planned isolated TLS native-server canary, as the existing service user, before stopping production. | Server/protocol behavior on the actual Droplet with disposable data. It still does not establish remote Unity GUI behavior. |
| Secure activation, public access and friend connection | **NOT DEPLOYED**. No client release/download URL published yet. Public TCP 7777 remains restricted until secure activation is verified. | Requires matching reviewed release, backups, owner-side activation, secured endpoint checks and real remote-client acceptance. SSH remains restricted to the owner's home address. |
| GitHub | Public repository authorized; draft [PR #1](https://github.com/RubberDuckieGod/ascendance-unity/pull/1) is the review path. | A PR or source-intake Action is not a deployment or runtime verdict. |

No row certifies every Hero/card/item interaction, every possible multiplayer sequence, or a penetration test. A failed, incomplete or unsupported check remains visible; successful execution alone is insufficient.

## Native defects and current repairs

An actual, untampered native server save/start-turn/end-turn/restart sequence failed even though the prior six Editor gates passed. Unity `JsonUtility` can deserialize absent optional `PendingRewardChoice` and `PendingEncounterDeckControl` values as non-null objects containing only empty/default fields. Those objects were treated as real pending choices and prevented the next turn after loading.

The repair normalizes only wholly empty optional DTOs at save loading/cloning, game-state application and room persistence loading. Any nonempty source ID, name, Hero, mode, prompt or revealed-card list preserves the saved object, including partial/future choice modes. Preserving a partial object exposed a separate null-`Mode` dereference in the Campaign I choice handlers; both handlers now guard null/empty modes without deleting that choice. Final native acceptance must include the original untouched restart and preservation cases, not only a hand-repaired fixture.

Remote clients now enable `Application.runInBackground` in the production online bootstrap. The test driver must not force that flag and thereby hide the real defect. The two native GUI windows need actual independent processes and observable authoritative updates while one window lacks focus.

The optional transport now supports TLS 1.2 with exact certificate pinning, hostname/validity checks, and a 64-hex-character room key on every RPC before room authority is exposed. DigitalOcean activation requires secure configuration and has no plaintext fallback. Final wire checks include wrong/missing keys on the first HELLO, wrong certificate pin, plaintext against the TLS listener, incomplete handshakes, no-first-request timeouts and continued listener health. Unity may return a structurally empty default snapshot DTO on rejection; acceptance must reject populated private state, not incorrectly require the DTO itself to be null.

## Reproduce local helper checks

From the project root:

```bash
python3 -B tools/project_intake.py --metadata-only --report /tmp/ascendance-metadata-status.json
python3 -B tools/project_intake.py --require-source --report docs/INTAKE_STATUS.json
python3 -B -m unittest discover -s tests -p 'test_*.py' -v
bash -n Deploy/DigitalOcean/run-ascendance.sh
```

`--metadata-only` may succeed when source is missing; its report must still show source/runtime blockers. `--require-source` fails on an incomplete handoff and writes its report even on failure. The source-intake workflow runs that strict gate. Optional deployment file checks establish presence, not activation. PowerShell-backed fixtures need an available PowerShell executable; skipped fixtures are not passes.

Historical `tests/verify_*.py` files are source-string checks for particular versions, and some overwrite saved reports. Intake parses them without executing them. Existing `VALIDATION_*.txt`, `tests/verification-*.txt`, the uploaded Editor log and the [original README](history/README_uploaded.md) describe earlier work. They cannot replace a current Unity build or gameplay assertion.

## Licensed Unity gates and native acceptance

On a licensed Windows machine, close the project in the Editor and run:

```powershell
& .\tools\Run-UnityValidation.ps1 -Editor 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe' -BuildLinuxServer -BuildWindows
```

Install matching build modules in Unity Hub and adjust the path as needed. The wrapper preserves Editor licensing/configuration, isolates game test data, refuses reused output directories, checks terminal gate verdicts and errors, and enforces a process deadline. It does not deploy.

With a licensed Linux Editor:

```bash
bash tools/run_unity_validation.sh \
  --editor /absolute/path/to/6000.6.5f1/Editor/Unity
```

Add `--build-linux-server` to build the server after the runtime gates. Retain the logs, structured verdicts, complete player output and manifests with the tested revision under `artifacts/unity-validation/`. Do not run multiple Editors against the same project simultaneously.

The master/foundation/router/transport targets are **54/24/29/27**; the uploaded README's older 25-check transport count is historical. The **22** reliability checks cover actual turn mutations, idle polling, duplicate-command freshness, ownership/lease rejection, broken-socket recovery, previous-process rejection and a 36-second idle socket. The **20** snapshot checks cover presentation installation and exact actions/decks, selection, HUD, movement destinations, options, logs and stale/retired revisions using synthetic states. The latter do not execute all gameplay rules.

`tools/native_server_acceptance.py` exercises the separately running Linux server through two Python protocol clients. It must verify expected room/build, actual numeric gameplay changes, ownership, stale and duplicate commands, idle/reconnect, legitimate pending choices and controlled save/restart behavior in temporary data. Run both trusted-local plain transport and secured TLS modes; a diagnostic run excluding a known failing case is not a complete pass.

For the two-process GUI procedure and evidence boundaries, see [native client acceptance](NATIVE_CLIENT_ACCEPTANCE.md). Its development-only driver is not included in shipping clients or the server. Linux GUI acceptance against production client code, Windows player compilation, and an actual Windows gameplay run are separate claims.

The manual GitHub workflow requires a configured, licensed self-hosted Windows runner and a protected environment. It does not automatically run public PR code and has not supplied the local evidence described here. A configured wrapper or workflow is not proof that it ran.

## DigitalOcean activation boundary

The owner verified that `ascendance` runs as the existing `ascendance` service user, preserving room `ASC-PRIMARY` on TCP 7777. The actual persistent root is `/var/lib/ascendance/.config/unity3d/Ascendance/Ascendance`, including `AscendanceServerRooms0100`. The base service, working directory and current code remain available for recovery. Production player data is excluded from acceptance fixtures.

Cloud SSH is blocked by this environment's TLS-terminating proxy; repeating SSH keys or widening the firewall cannot fix that route. The owner's existing Windows SSH connection is the activation path. Only after final build/test proofs and review will a pinned PowerShell command download and verify the complete client, stage immutable server code, run an isolated TLS canary as the service user, refuse connected players, create verified backups, gracefully stop the old service and activate/check the secured replacement.

The private profile is retrieved by SCP to an owner-only Windows ACL and shared privately with trusted friends. It contains a room access key and must never enter Git, chat, public release assets or logs. Public TCP 7777 is opened only after secure activation is verified; SSH stays restricted to the owner's home address. A later rollback to the previous plaintext server requires restricting public game access first. Automatic recovery restores code/configuration without silently restoring campaign data.

A private canary pass, successful process start or open TCP port cannot establish successful production migration or remote GUI synchronization. Preserve separate deployment and client acceptance reports.
