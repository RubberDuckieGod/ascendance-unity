# Dedicated native combat validation inputs

This branch carries four compiled test assets solely so GitHub Actions can run
the two-player native combat acceptance suite. Do not merge this branch's binary
payload into a feature branch or main.

Both builds come from the clean runtime source commit
`2bcf6efe9a20b2692fc9dad10f7b9fb5f6dfa585`. The separate test harness is checked
out at the workflow run's own commit. The hash-pinned inventories identify every
asset and chunk. Before uploading, the job reconstructs the four files, checks
their complete manifests, safe archive paths, clean build provenance and public
file rules, then verifies the existing private draft's fixed ID, tag and source.
It uploads only missing matching assets and refuses conflicts.

The upload job transfers only the four verified build files through a short-lived
Actions artifact. The native test job has read-only repository permissions and
independently verifies them before launching an isolated local server and two
actual graphical clients. Only the checked semantic gameplay report is retained.

The draft `combat-validation-2bcf6ef` stays unpublished. This workflow does not
change Render, DigitalOcean, player campaigns, connection profiles, or the
separate private licensed-asset intake draft.
