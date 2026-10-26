# Updating the existing DigitalOcean installation

This package upgrades the inspected `ascendance` systemd service on
`159.65.100.206`. The existing installation is `/opt/ascendance`; its Unity data
remains `/var/lib/ascendance/.config/unity3d/Ascendance/Ascendance`. It neither
clears saves nor creates player sessions in the existing room.

The owner runs the SHA-256-pinned Windows wrapper using the existing Windows
OpenSSH key. The wrapper prepares the complete verified Windows client before
requesting any server activation. Source scripts, Linux server archive, and
manifest are separately pinned by the reviewed release descriptor.

The Windows wrapper runs as the current Windows user. It changes only access
permissions, preserves ownership and audit settings, and verifies private paths
before transferring the invitation. Foreign-owned paths and filesystem links
are refused. It does not require administrator elevation. The revised wrapper
fixes the `SeSecurityPrivilege` failure in the original Windows permission step;
the validated game builds and release descriptor are unchanged.

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
