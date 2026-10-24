# Validation status

This document distinguishes uploaded historical evidence, checks run on this checkout, and work that still requires a licensed Unity Editor or an inspected deployment environment. The game version remains **0.10.2c**; reliability changes are identified by Git revision and build manifest.

| Check | Current evidence | Meaning |
| --- | --- | --- |
| Independent source intake | PASS for supplied metadata and recovered Assets; see [INTAKE_STATUS.json](INTAKE_STATUS.json). | Static settings, package graph, minimum assets, original scene GUIDs, and authored JSON catalog totals match. |
| Intake-checker fixture tests | 14 tests passed locally. | Exercises missing source, invalid metadata/package locks, malformed data, scene GUID mismatches, failure reports, and syntax-only handling of historical verifiers. |
| Release-tool fixture tests | 15 tests passed locally. | Uses temporary files, synthetic non-runnable artifacts, and fake service/health programs. It provides no live deployment evidence. |
| Verdict and launcher tests | 17 tests passed locally, including five PowerShell process-control tests. | Synthetic verdicts and non-Unity executable fixtures verify missing results, wrong versions, failure exits, stale output refusal and required gates. |
| Current historical static verifier | `tests/verify_0102c.py`: 26/26 source-string checks passed. | This executes no game code; older milestone verifiers were syntax checked only. |
| C# / PowerShell / Bash syntax | All 161 C# files parse; the PowerShell wrapper and Bash scripts parse. | Parsing is not Unity compilation or platform build validation. |
| Unity import/compilation and new builds | BLOCKED / not run here. | Requires Unity 6000.6.5f1, a valid license, and matching platform modules. |
| Inherited gameplay/network gates | Historical targets only: 54/0, 24/0, 29/0, 27/0. | No new Play Mode pass is claimed. |
| New Unity reliability/snapshot regressions | 22 real-socket and 20 replica/HUD checks prepared; not run in Unity here. | Additional runtime checks, separate from the inherited target counts. |
| DigitalOcean activation and remote multiplayer | BLOCKED / not performed. | Requires a reviewed build, inspected service/save paths and backups, deployment access, and application-level acceptance. |
| GitHub publication and hosted Actions | Public repository confirmed; owner approved source publication. | Actual push/PR and Actions results must be verified separately from local checks. |

## Reproduce local helper checks

From the project root:

```bash
python3 -B tools/project_intake.py --metadata-only --report /tmp/ascendance-metadata-status.json
python3 -B tools/project_intake.py --require-source --report docs/INTAKE_STATUS.json
python3 -B -m unittest discover -s tests -p 'test_*.py' -v
bash -n Deploy/DigitalOcean/run-ascendance.sh
```

`--metadata-only` may succeed when source is missing; its report must still show source/runtime blockers. `--require-source` fails on an incomplete handoff and writes its report even on failure. The source-intake workflow runs that strict gate and preserves the generated report. Its success certifies static intake only. The report's optional deployment section checks package-file presence, not activation or live service health.

The new Python tests exercise helper behavior. The historical `tests/verify_*.py` scripts are source-string checks tied to particular old versions, and some overwrite their saved reports. The independent intake checker parses them without execution. Running historical verifiers wholesale against the current version is not a substitute for Unity compilation or gameplay validation.

## Licensed Unity acceptance

On the owner's licensed Windows machine, close the project in the Editor and run:

```powershell
& .\tools\Run-UnityValidation.ps1 -Editor 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe' -BuildLinuxServer -BuildWindows
```

Install matching build modules in Unity Hub and adjust the Editor path as needed. The wrapper preserves existing Editor licensing/configuration and redirects only game test data. It refuses existing output directories, waits for all gates, rejects errors and incomplete verdicts, and only then builds players. Its process has a hard deadline as well as the Editor bridge's runtime deadline. It does not deploy.

Alternatively, use a licensed Linux Editor:

```bash
bash tools/run_unity_validation.sh \
  --editor /absolute/path/to/6000.6.5f1/Editor/Unity
```

Add `--build-linux-server` to request a server build after runtime validation. Retain logs and structured results under `artifacts/unity-validation/` with the tested Git revision. A configured workflow or wrapper is not evidence that it ran.

The inherited **Test 0.10.2c - Real Network Transport (Play Mode)** menu chains the master save/load regression, dedicated server foundation, client routing, and TCP transport checks. Their acceptance targets are respectively **54 passed / 0 failed**, **24/0**, **29/0**, and **27/0**. The uploaded README's 25-check transport count is stale; the current version's verifier and changelog specify 27. Preserve the original README as history rather than treating its older totals as current acceptance.

New **22/0 reliability** and **20/0 snapshot** checks supplement those gates. The reliability suite uses the production TCP client/server and actual Hero turn mutations, idle polling, duplicate-command freshness, ownership rejection, broken-socket recovery, lease handoff, previous-process rejection and a genuinely idle 36-second session. The snapshot suite uses synthetic server states solely to check main-thread installation, initial readiness, exact deck/action state, local selection, existing HUD reactions, movement destinations, options, log synchronization and stale/retired revisions. Its fixture never executes gameplay rules. Review actual batch results and nested Unity errors before accepting any result. Build artifacts also need a successful Unity build result and the complete emitted executable, libraries, and data; file presence and hash agreement alone are insufficient.

The manual GitHub workflow requires a configured, licensed self-hosted Windows runner and protected environment; it has not run here. It never triggers automatically for public pull requests. Until that workflow is merged/configured, the local Windows command is the acceptance path. Source intake passing does not make Unity acceptance pass.

## Historical evidence and live acceptance

The uploaded Editor.log records successful Linux dedicated-server and Windows client builds on the original Windows machine. Those outputs were not included in the source archives, and that log was excluded from Git. Existing `VALIDATION_*.txt` and `tests/verification-*.txt` PASS lines describe earlier work; they are not results from this checkout.

Before accepting remote multiplayer, verify the expected build/room through protocol HELLO, two-client authoritative gameplay and state reconciliation, idle periods beyond 35 seconds, reconnect with retained Hero ownership, interrupted-command retry without duplicate mutations, and persistence across a controlled restart. TCP port reachability proves only that a listener accepted a connection.

Follow the [DigitalOcean deployment guide](../Deploy/DigitalOcean/README.md) for release-tool boundaries and existing-server migration prerequisites. Helper safety tests, staged artifacts, and rollback mechanisms do not establish that a production campaign was backed up or that a live upgrade succeeded.
