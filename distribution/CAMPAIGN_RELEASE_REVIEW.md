The campaign release workflow is inactive until its exact inventory checksum is
frozen in `publish-campaign-reviewed.yml`. It verifies full server and Windows
archives, then reruns compiled-server plain TCP and pinned TLS acceptance on
isolated runner data. Upload uses only the workflow token and only the exact
reviewed new prerelease draft. It never publishes, deploys, or resets a campaign.

Prepare a clean runtime commit and matching LinuxServer, Windows shipping, and
Linux development builds. Run actual graphical full-roster campaign acceptance,
visually review screenshots, and prove copied original-h campaign restoration.
Package LinuxServer + Windows with `tools/package_multiplayer_release.py`.

Add a `release.json` pinned to the real runtime and deployment-tooling commits,
then an allowlisted `full-campaign-validation-reports.zip`. Reports must include
`acceptance-summary.json`, `editor-gates.json`, `native-full-campaign-tls.json`,
`native-h-campaign-restore.json`, `windows-build-provenance.json`, and
`linux-development-build-provenance.json`. No campaign
save, private invite, room key, creator token, TLS key, or license belongs in any
asset or report. Update `SHA256SUMS` to cover the other nine assets.

Create the exact Git tag at the runtime commit BEFORE creating the draft. Freeze
the exact draft ID, runtime/deployment revisions, all ten asset hashes and byte
counts, launcher hash, report hashes and byte counts, and actual acceptance
counts in `distribution/reviewed-campaign-upload.json`. Its `acceptance` object
contains `editorGates`, `helperChecks`, `graphicalChecks`, `migrationChecks`,
`plainServerChecks`, and `tlsServerChecks`. The workflow inventory checksum must
match the reviewed bytes. Uploads refuse existing assets with different bytes.

After independent remote digest verification, publishing the release is separate
from deployment. Final on-host installer constants must use the actual release,
runtime/tooling commits and descriptor SHA; show its checksum-pinned command to
the owner. A successful draft upload is not a successful server update.

For a client update, reuse the owner's existing private invite through
`tools/windows-install-client.ps1 -ExistingInvitePath <local-private-path>` and
pass the new `-ExpectedRelease`, `-ExpectedSourceRevision`, descriptor URL/SHA,
and unchanged expected certificate SHA. This avoids another SSH copy of the
invite. Keep the existing player's local data for creator and reconnect identity.

The upgrade keeps the current campaign active. After updating server and clients,
the creator explicitly chooses NEW ONLINE CAMPAIGN, confirms the previous slot
is retained, then players choose unique Heroes from all 16, READY, and START.
SAVED CAMPAIGNS restores the archived adventure.
