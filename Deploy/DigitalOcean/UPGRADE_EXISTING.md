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

The current h entrypoint pins the matching h release descriptor, runtime source
55d25c0, reviewed helper source 74084ea, and the g save-schema payload. Earlier
c releases and their descriptors remain unchanged. For an
apply request it runs the pinned read-only service diagnostic before downloading
the game archive or running the canary. Known build, protocol, correlation or
exact room/save identity mismatches return the precise failed checks and safe
diagnostics immediately, without stopping the service. An unavailable plaintext
probe defers to the upgrader's transport-aware checks. Later failures attach the
diagnostic automatically; a separate diagnostic command is unnecessary.

For the original verified plain 0.10.2c server, the authoritative
`MovingHeroIds` list must be explicitly empty and resolution/reaction flags
must be false. That source version has no supplemental `HeroMovement` field;
its absence alone no longer blocks maintenance. A present null, malformed or
nonempty detail field still fails. The secured target must supply its newer
movement field. Version, protocol, room identity, client and backup guards are
unchanged. The source-defined legacy contract passed 12 independent loopback
TCP fixture checks; this is not execution of an original legacy Unity binary.

Wire routing accepts ASCII room capitalization differences, but Linux save
filenames and existing clients' reconnect storage do not. A capitalization
mismatch remains blocked; the h runtime independently refuses an aliased save
before an initial write. Do not force that guard or change client invitation spelling. Native
fixtures reproduce both lost progress under stale spelling and retained progress
under exact active spelling; they do not certify a production migration.

The owner can also run the SHA-256-pinned Windows wrapper using the existing
Windows OpenSSH key. The wrapper prepares the complete verified Windows client before
requesting any server activation. Source scripts, Linux server archive, and
manifest are separately pinned by the reviewed release descriptor.

The Windows wrapper runs as the current Windows user. It changes only access
permissions, preserves ownership and audit settings, and verifies private paths
before transferring the invitation. Foreign-owned paths and filesystem links
are refused. It does not require administrator elevation. The access-only permission helper fixes the `SeSecurityPrivilege` failure in
the original Windows permission step; h client setup reuses those validated
private-path functions.
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

## Reviewed g-to-h update

The live g assembly is recognized by exact SHA-256, independently from target h. The pinned recursive schema covers all 24 reachable serialized types and 415 fields; unknown fields/types, duplicate keys, future pending-state schemas or mismatched live/save identities stop the update before the service is stopped.

After the ordinary synthetic native canary, the helper loads a private copy of the stable existing room with target h, using a fresh XDG directory, loopback-only TLS and HELLO only. It compares complete campaign state, ordered card lists, player records including credentials and original creation identity. Only demonstrated restart equivalences are allowed: build/timestamps, enemy-array order with unique IDs, explicit pending-presence normalization, disconnected sessions, and a narrowly verified empty-enemy-selection fallback. Preferred existing targets remain exact. A mismatch leaves the real service and source save unchanged.

After activation the same progress proof and restored-source diagnostics must pass. Recovery verifies the original complete code and current intact state before restarting g. Damaged progress causes an explicit recovery failure with retained backups, rather than silently restoring old campaign data.

The new client-only Windows tool takes the pinned release descriptor URL and checksum, verifies the whole matching h ZIP and clean build provenance, and preprotects the invitation before interactive SCP. Its ACL helpers passed 34 assertions on a real nonadministrator Windows PowerShell 5.1 account without SeSecurityPrivilege. This evidence does not run the game or perform SCP.
