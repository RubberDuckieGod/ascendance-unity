# Updating the existing DigitalOcean installation

This package upgrades the inspected `ascendance` systemd service on
`159.65.100.206`. The existing installation is `/opt/ascendance`; its Unity data
remains `/var/lib/ascendance/.config/unity3d/Ascendance/Ascendance`. It neither
clears saves nor creates player sessions in the existing room.

For a direct installation, open an interactive root SSH session and run the
checksum-pinned `install-release.py --apply` entrypoint on the Droplet. It fetches
the existing reviewed runtime with separately pinned deployment helpers and delegates to the same canary, backup and
activation implementation. Progress goes to stderr and public result JSON goes
to stdout. Without `--apply` it only stages and validates. The private invitation
stays on the Droplet for a separate owner-only transfer after activation.

The revised entrypoint uses `releases/release-deployment-ef41d18.json`; the
original published `release.json` and game binaries remain unchanged. For an
apply request it runs the pinned read-only service diagnostic before downloading
the game archive or running the canary. Known build, protocol, correlation or
exact room/save identity mismatches return the precise failed checks and safe
diagnostics immediately, without stopping the service. An unavailable plaintext
probe defers to the upgrader's transport-aware checks. Later failures attach the
diagnostic automatically; a separate diagnostic command is unnecessary.

Wire routing accepts ASCII room capitalization differences, but Linux save
filenames and existing clients' reconnect storage do not. A capitalization
mismatch remains blocked until the actual active save and original launcher are
verified. Do not force that guard or change client invitation spelling. Native
fixtures reproduce both lost progress under stale spelling and retained progress
under exact active spelling; they do not certify a production migration.

The owner can also run the SHA-256-pinned Windows wrapper using the existing
Windows OpenSSH key. The wrapper prepares the complete verified Windows client before
requesting any server activation. Source scripts, Linux server archive, and
manifest are separately pinned by the reviewed release descriptor.

The Windows wrapper runs as the current Windows user. It changes only access
permissions, preserves ownership and audit settings, and verifies private paths
before transferring the invitation. Foreign-owned paths and filesystem links
are refused. It does not require administrator elevation. The revised wrapper
fixes the `SeSecurityPrivilege` failure in the original Windows permission step;
the validated game builds and release descriptor are unchanged.
The [actual Windows PowerShell 5.1 test](https://github.com/RubberDuckieGod/ascendance-unity/actions/runs/40249083895)
passed under a separate ordinary account with no `SeSecurityPrivilege`, including
34 setup/permission assertions, two unchanged audit policies and full cleanup.
See the [recorded evidence](../../docs/WINDOWS_DEPLOYMENT_ACL_VALIDATION_910D7BD.json).

Without `-Apply`, the wrapper stages the immutable server release, prepares
private transport files, and runs a complete native server canary with synthetic
rooms and temporary data. It leaves the active service running. With `-Apply`,
the same checks precede activation. The upgrader refuses connected players or
unresolved movement, reactions, or resolution. Persisted reward and deck choices
remain valid and do not block maintenance.

Before stopping the service, it verifies a stable full old-code manifest and its
rollback archive, existing unit/environment files, and a private data backup.
After a graceful stop, it copies and verifies the final consistent data. A scoped
systemd override selects the immutable release, explicitly preserves the XDG
data identity, and enables TLS plus the private room key. The original service,
installation, and data remain in place.

Post-start verification checks the actual process and executable hash, routing
and data identity, pinned certificate, missing and incorrect key rejection, and
authenticated read-only HELLO. It does not JOIN or command the existing room.
Failed initial activation restores the previous service/code configuration;
save data is never automatically rolled back. A recovery failure is reported
separately and retains the private backups for inspection.

**Keep TCP 7777 restricted to the owner until secure activation is verified.**
The helper does not change cloud firewall rules. An automatic initial rollback
may restore the old plaintext server while that port is still restricted.
After public access is enabled, any later rollback to a plaintext legacy build
must first restrict TCP 7777 again. SSH access must remain restricted separately.

After success, the wrapper retrieves the private invite directly from the server
to an owner-only Windows directory. The invite contains the shared room access
key and must not be published, committed, or pasted into chat. Friends receive
the complete Windows game ZIP and the invite privately; the launcher supplies
the key through the child environment rather than command-line arguments. Each
player chooses a distinct available Hero in the server party.

The native canary and helper fixtures are evidence for the tested scenarios.
They do not certify every authored card, quest, or combat mechanic. A production
process-health pass is not a production gameplay test.
