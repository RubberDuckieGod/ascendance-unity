# Source provenance

The game identifies itself as **Ascendance 0.10.2c**, using **Unity 6000.6.5f1 (3ff58d469c8a)**. Archive display names are not reliable version markers: the first archive's filename names an older validation milestone, while its PlayerSettings, README, and historical Editor log identify the later project.

Two uploaded ZIPs were actually used:

| Upload | Size | SHA-256 | Imported content |
| --- | ---: | --- | --- |
| `VALIDATION_0.8.62b file 2.zip` | 722,540 bytes | `4fa198a3d8109cf4595e2976ad35fbb42e200b8fc94e978ef586daf9710d83f5` | 405 files containing Packages, ProjectSettings, historical documentation/tests, local logs/settings, and build/personal scripts; no Assets directory. |
| `Editor.zip` | 5,260,305 bytes | `8fa10d3d6f933e83a49626ce4eb691339f55e55b37a4412cb9cd8b13d659b1ef` | 422 files and 23 directory entries. Its top-level `Scripts`, `Editor`, `Scenes`, `Resources`, `Art`, and metadata were imported beneath `Assets/`, preserving original bytes and GUIDs at import. |

The separate **Library file 1.zip** exceeded the 32 MiB transfer limit and was not downloaded or inspected. Its contents are unknown. An earlier **0.10.2c.zip** was likewise inaccessible above that limit. Neither archive was used. Unity regenerates Library from Assets, Packages, and ProjectSettings; the two readable uploads supplied the source needed for this checkout.

The recovered uploaded Assets baseline contains 156 C# files, two scenes, four JSON catalogs, 226 `.meta` files, and included art/resources. MainMenu and Stonebrook retain GUIDs `7a1e1c2f1b2e4a49a86759007a000701` and `7a1e1c2f1b2e4a49a86759007a000702`. The original tests directory contains 28 Python static verifiers and 97 saved text reports. New helpers and tests are separate additions to this baseline.

The initial source-import commit, `7f3c598`, intentionally excludes uploaded Logs, UserSettings, the personal root launcher, and the hardcoded root deployment updater. Their originals remain in the supplied archives, outside Git; local extracted copies were not added. The updater stopped the existing service and cleared its installation directory before copying; it was not executed. Original archives also remain outside the repository.

The original root README is preserved byte-for-byte as [history/README_uploaded.md](history/README_uploaded.md). Its old gate totals and release claims are historical context. Existing root changelogs, notes, static reports, and Python verifiers remain in their original locations because current source tooling refers to some of those paths. In particular, `CHANGELOG_0.10.2c.md` and `PERSISTENT_TCP_SESSION_HOTFIX_0.10.2c.md` remain at the project root.

Reliability work retains the game version **0.10.2c** and is identified by the resulting Git revision and per-build file/hash manifest. A manifest establishes artifact integrity; it does not establish successful compilation, runtime acceptance, or server deployment. See [VALIDATION_STATUS.md](VALIDATION_STATUS.md) for those distinctions.

The GitHub destination is the public `RubberDuckieGod/ascendance-unity` repository, created by the owner. The owner explicitly approved publishing this source publicly. A local commit or prepared workflow does not establish that push, Actions execution, or DigitalOcean deployment succeeded; consult the linked pull request and its actual check results.
