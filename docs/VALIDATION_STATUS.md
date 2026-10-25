# Validation status

**The secure DigitalOcean upgrade has not been deployed.** The runtime revision is **`eb51b2292cf433a2cbe5dabb4863f1fef993161a`**. Its three builds, six Editor gates, helper tests, complete native plain/TLS server checks and **77 independent native Unity GUI checks** passed, including actual world/Hero visibility review. The game remains **0.10.2c**; this Unity implementation is separate from the Render JavaScript game.

## Evidence and its limits

| Check | Evidence currently available | What it establishes |
| --- | --- | --- |
| Recovered source intake | [Intake report](INTAKE_STATUS.json), recovered Assets, metadata and pinned package graph. Both hosted Source intake runs for `eb51b22` passed. | Static source/settings presence, authored catalogs and asset GUIDs. It executes no gameplay. |
| Static verifiers and helper fixtures | **91 passed, 0 failed, 0 skipped** on the final source, including the PowerShell-backed fixtures. | Helper behavior against temporary files, synthetic artifacts and fake services; not a live deployment or Unity gameplay verdict. |
| Prior licensed Editor gates | Commit `9792d169b1141d0dbef043906246f3fe0dab4497`: master **54/0**, foundation **24/0**, routing **29/0**, transport **27/0**, reliability **22/0**, snapshot **20/0**. [Runtime report](RUNTIME_RESULTS_20261024.json). | Actual Editor execution for that revision. Real-socket tests run inside one Editor process; snapshot checks include synthetic presentation states. These are not independent native-client acceptance. |
| Prior native platform builds | Linux dedicated server and Windows client built at `9792d16`; [file provenance](BUILD_PROVENANCE_9792d16.json). Intermediate secure builds also compiled successfully. | Compilation and emitted artifacts for those specific revisions. Neither hashes nor successful compilation prove gameplay or deployment. |
| Final current-source six Editor gates | **176 passed, 0 failed, 0 nested errors**: master **54**, foundation **24**, routing **29**, transport **27**, reliability **22**, snapshot **20**. Completed in **96.2 s** on clean `eb51b22`. | Actual licensed Editor execution after the persistence, TLS, background and camera repairs. Real sockets inside one Editor are not independent native clients. |
| Final current-source platform builds | **PASS**: Linux dedicated server (**47.4 s**), normal Linux GUI development client (**47.9 s**), shipping Windows client (**52.7 s**). | Embedded provenance identifies clean `eb51b22`; source/application inputs were unchanged. Windows was compiled, not executed. The development driver is absent from shipping Windows/server assemblies. |
| Actual native Linux server and two Python wire clients | **PASS: 73 plain / 80 TLS checks, 0 failed** on the exact final server build. Both modes passed original untouched restart, partial-state preservation, real command effects and 36-second idle checks. Across 30 native process logs, no unexpected application exceptions were found. | Exercises separately running shipped Unity servers over real sockets with isolated data. Python clients do not certify Unity client UI or presentation. |
| Two independent native Unity GUI clients | **77 passed, 0 failed**, against final clean `eb51b22` binaries. Two actual Linux GUI processes passed state/HUD/gameplay checks, client/server restarts and **36.0004 s** idle; main loops advanced **78/80** frames, with exactly **0** camera drift. Actual screenshots show the village and standing Heroes in both windows after restart. | Independent native GUI, isolated-data and bounded visual proof. Also checks a focused window with its pointer outside. Does not certify Windows execution, all menus/cards/full visual UX or public routing. |
| Windows gameplay and audio | **NOT RUN / UNVALIDATED**. The Windows player compiled; no actual Windows GUI or audio-output acceptance was performed here. | Linux graphical execution does not certify platform-specific Windows behavior or sound-device initialization. |
| Existing DigitalOcean server inspection | Owner's reviewed read-only inspection succeeded: service/user, executable, room, port and actual save path identified; no inspection warnings. | Existing deployment identity and migration prerequisites. No campaign save content was used for testing. |
| Private DigitalOcean native canary | **NOT RUN**. Planned isolated TLS native-server canary, as the existing service user, before stopping production. | Server/protocol behavior on the actual Droplet with disposable data. It still does not establish remote Unity GUI behavior. |
| Secure activation, public access and friend connection | **NOT DEPLOYED**. The [matching prerelease](https://github.com/RubberDuckieGod/ascendance-unity/releases/tag/unity-0.10.2c-multiplayer-eb51b22) is published. Public TCP 7777 remains restricted until secure activation is verified. | Requires matching reviewed release, backups, owner-side activation, secured endpoint checks and real remote-client acceptance. SSH remains restricted to the owner's home address. |
| GitHub | Public repository authorized; draft [PR #1](https://github.com/RubberDuckieGod/ascendance-unity/pull/1) is the review path. | A PR or source-intake Action is not a deployment or runtime verdict. |

No row certifies every Hero/card/item interaction, every possible multiplayer sequence, or a penetration test. A failed, incomplete or unsupported check remains visible; successful execution alone is insufficient.

## Runtime and tooling provenance

The final evidence directory is `artifacts/unity-validation/release-eb51b22-20261025T233739Z/`. It contains `gates/validation-results.json`, `gates-summary.json`, all three `build-*-summary.json` files, player provenance files, `final-build-integrity-and-driver-exclusion.json`, and `native-acceptance/{plain,tls,summary}.json`. The native summary records **153 passed / 0 failed**, with 15 isolated native process launches in each mode. These local artifacts are retained evidence, not automatically Git-tracked reports or hosted runs.

Final GUI evidence is in `native-gui-tls-final-01/` beneath that directory: `results.json` records the **77/0** expected-versus-actual checks, `visual-review.json` records actual image review and limits, `README.md` lists exact gameplay results, and `native-clients-before.png` / `native-clients.png` show the real two-window desktop. The final images were viewed and confirmed to show Stonebrook terrain/buildings and standing Hero models after server restart. Every client/server/private X process stopped cleanly. Audio remains outside acceptance; native FMOD startup reported no audio device on this host.

The application, acceptance tools and release helpers were frozen in runtime source `eb51b2292cf433a2cbe5dabb4863f1fef993161a`. Build provenance reports `dirty: false`, and before/after checks found no application-input changes. Later documentation-only commits may have a different repository HEAD; they do not imply a recompiled runtime. Player provenance, release manifests and `RuntimeSourceRevision.json` identify the actual built source. The [published prerelease](https://github.com/RubberDuckieGod/ascendance-unity/releases/tag/unity-0.10.2c-multiplayer-eb51b22) contains those matching runtime artifacts and public reports; DigitalOcean activation is still unperformed. The [acceptance summary](MULTIPLAYER_ACCEPTANCE_EB51B22.json) records the completed suites and remaining boundaries.

## Native defects and current repairs

An actual, untampered native server save/start-turn/end-turn/restart sequence failed even though the prior six Editor gates passed. Unity `JsonUtility` can deserialize absent optional `PendingRewardChoice` and `PendingEncounterDeckControl` values as non-null objects containing only empty/default fields. Those objects were treated as real pending choices and prevented the next turn after loading.

The repair normalizes only wholly empty optional DTOs at save loading/cloning, game-state application and room persistence loading. Any nonempty source ID, name, Hero, mode, prompt or revealed-card list preserves the saved object, including partial/future choice modes. Preserving a partial object exposed a separate null-`Mode` dereference in the Campaign I choice handlers; both handlers now guard null/empty modes without deleting that choice. The final plain/TLS native runs passed the original untouched restart, legitimate choice/deck resolution and partial-object preservation cases, without excluding the earlier failures.

Remote clients now enable `Application.runInBackground` in the production online bootstrap. The test driver does not force that flag. Visual review of an earlier gameplay-only GUI pass found the world panned off-screen: unfocused camera edge-scroll kept running while background updates were enabled. Camera input now stops when the window lacks focus or the pointer is outside its bounds. The final 77-check GUI run passed exact zero camera drift in both input cases and actual world/Hero visibility after idle and restart, alongside state/HUD assertions. Gameplay counters alone were insufficient to detect this defect.

The optional transport now supports TLS 1.2 with exact certificate pinning, hostname/validity checks, and a 64-hex-character room key on every RPC before room authority is exposed. DigitalOcean activation requires secure configuration and has no plaintext fallback. The final 80-check TLS run passed wrong/missing keys on the first HELLO, wrong certificate pin, plaintext against the TLS listener, incomplete handshakes, no-first-request timeouts and continued listener health. The unauthenticated peers closed after approximately five seconds. Unity may return a structurally empty default snapshot DTO on rejection; acceptance rejects populated private state rather than incorrectly requiring the DTO itself to be null.

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

The separate manual **Native multiplayer release acceptance** workflow downloads reviewed release assets, checks the supplied hashes and clean source provenance, then runs the native plain/TLS server suites with temporary data on a hosted Linux runner. It does not run Unity GUI clients, build new players or activate DigitalOcean. It has not supplied the local native verdicts recorded above.

## DigitalOcean activation boundary

The owner verified that `ascendance` runs as the existing `ascendance` service user, preserving room `ASC-PRIMARY` on TCP 7777. The actual persistent root is `/var/lib/ascendance/.config/unity3d/Ascendance/Ascendance`, including `AscendanceServerRooms0100`. The base service, working directory and current code remain available for recovery. Production player data is excluded from acceptance fixtures.

Cloud SSH is blocked by this environment's TLS-terminating proxy; repeating SSH keys or widening the firewall cannot fix that route. The owner's existing Windows SSH connection is the activation path. Only after final build/test proofs and review will a pinned PowerShell command download and verify the complete client, stage immutable server code, run an isolated TLS canary as the service user, refuse connected players, create verified backups, gracefully stop the old service and activate/check the secured replacement.

The private profile is retrieved by SCP to an owner-only Windows ACL and shared privately with trusted friends. It contains a room access key and must never enter Git, chat, public release assets or logs. Public TCP 7777 is opened only after secure activation is verified; SSH stays restricted to the owner's home address. A later rollback to the previous plaintext server requires restricting public game access first. Automatic recovery restores code/configuration without silently restoring campaign data.

A private canary pass, successful process start or open TCP port cannot establish successful production migration or remote GUI synchronization. Preserve separate deployment and client acceptance reports.
