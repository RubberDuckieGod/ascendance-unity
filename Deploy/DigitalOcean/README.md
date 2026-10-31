# Local Linux release support

This directory contains fresh-install support, not a deployed server. It does not contain a Linux game build or a multiplayer health implementation. No SSH, cloud API, firewall changes, account creation, or automatic CI deployment is included.

`release.py` uses Python 3.9+ on Linux. The runner requires Bash and `readlink`. A real install requires systemd, an existing service account, a reviewed Unity Linux Dedicated Server build, and an application health command supplied by the operator.

## Existing DigitalOcean server

The legacy PowerShell updater stops the existing service and clears its executable directory before upload. Do not use that updater for these releases. This helper refuses an existing install directory, config/data/log directory, unit file, or registered service at initialization. An existing server needs a separate reviewed migration after its service user, unit, actual save path, current room, and backups have been inspected. Do not select a new identity or empty directory to bypass those checks when continuing an existing campaign.

No migration, live upgrade, production connection, or server restart has been performed by adding these files. This helper can update only a layout that it created itself.

## Build and verify

The source builder is `Ascendance.EditorTools.AscendanceDedicatedServerBuilder0100.BuildLinuxDedicatedServer0102`, using only `Assets/Scenes/Stonebrook.unity`. The project pins Unity `6000.6.5f1`. Build with a licensed Editor and its Linux Dedicated Server support. Use the complete `Builds/LinuxServer` output, including `AscendanceServer.x86_64`, `UnityPlayer.so`, `AscendanceServer_Data`, and all other emitted files.

Run the inherited Unity validation gates before accepting a build: master **54/0**, server **24/0**, routing **29/0**, transport **27/0**. A static source check or TCP connection is not multiplayer validation.

On the machine holding the reviewed build, record every regular file and its SHA-256 digest:

```bash
python3 Deploy/DigitalOcean/release.py manifest \
  --build-dir /absolute/path/to/Builds/LinuxServer \
  --output /absolute/path/to/linux-server-manifest.json
```

Keep the manifest with the trusted build output. It detects transfer/tampering differences; it is not a signature or independent proof of provenance. The helper checks an x86-64 ELF executable/library and nonempty data content, rejects symlinks, special files and traversal, and requires an exact file/hash match. It cannot prove that an arbitrary compiled game is complete or retains the source's persistence behavior. Keep the builder's `Ascendance` company/product identity and persistence implementation unchanged unless a migration is reviewed.

## Prepare a fresh host locally

The following commands are examples for an empty host, executed locally by its operator. Supply the intended account and absolute paths. The parent directories must exist; the four install/config/data/log paths must be absent and disjoint. The helper creates those directories and records immutable service user UID/GID, room, port, external paths, runner, and unit content. It starts and enables nothing during `init`.

```bash
sudo python3 Deploy/DigitalOcean/release.py init \
  --root /opt/ascendance \
  --config-dir /etc/ascendance \
  --data-home /var/lib/ascendance \
  --log-dir /var/log/ascendance \
  --unit-dir /etc/systemd/system \
  --service-name ascendance --user ascendance \
  --room ASC-PRIMARY --port 7777
```

Use a dedicated non-root account. Config stays outside releases; data/log ownership is assigned to the selected account. The runner pins `HOME` and `XDG_CONFIG_HOME` under the external data home and passes the documented `-batchmode -nographics -ascendance-server -room ... -port ... -logFile ...` flags. Confirm the actual `Application.persistentDataPath` on a disposable test install before accepting a campaign. The source stores rooms under `Application.persistentDataPath/AscendanceServerRooms0100`; no game-specific save-path override is invented here.

The systemd unit example is illustrative. `init` renders the exact unit and records it. Later commands refuse edited units/config, changed account identity, moved roots, or unmanaged `current` pointers; activation also refuses systemd overrides. Inspect a partial initialization failure manually; the helper never removes external data to retry initialization.

## Stage and activate

Staging does not touch the service. A release name is immutable and cannot be overwritten. Every emitted artifact file is copied, checked again, and kept in `releases/NAME/build`.

```bash
sudo python3 Deploy/DigitalOcean/release.py stage \
  --root /opt/ascendance --release 0.10.2c-verified-1 \
  --build-dir /absolute/path/to/Builds/LinuxServer \
  --manifest /absolute/path/to/linux-server-manifest.json
```

Activation requires an explicit operator health command; the example path below must be replaced with an installed, trusted implementation. It runs as the invoking operator with no shell expansion and a bounded timeout. Its environment includes `ASCENDANCE_ROOT`, `ASCENDANCE_RELEASE`, `ASCENDANCE_ROOM`, and `ASCENDANCE_PORT`. It should wait for readiness and fail if the expected build/room or application behavior is incorrect. Allow enough time within the timeout for Unity startup.

```bash
sudo python3 Deploy/DigitalOcean/release.py activate \
  --root /opt/ascendance --release 0.10.2c-verified-1 \
  --health-command '/absolute/path/to/operator-multiplayer-health' \
  --health-timeout 120
```

The helper verifies the managed service and both release manifests, changes `current` with an atomic symlink replacement, restarts the service, then runs health validation. Restart/health failure restores the previous pointer, restarts it, and checks recovery. A failed first activation restores the absence of `current` and stops the service. A recovery failure is reported separately; inspect the service immediately. Every existing release and external data directory is retained. There is no release garbage collection, data rollback, or automatic boot enablement.

Changing the executable pointer does not make campaign persistence transactional. Stop gameplay at a safe save boundary and back up the actual room data before an accepted update. Transient reactions/combat and a command-result cache may not survive process restart. Code rollback cannot undo a new release's save-schema changes. Confirm backward compatibility before activation.

```bash
sudo python3 Deploy/DigitalOcean/release.py status --root /opt/ascendance
```

`status` checks local managed metadata only. TCP `7777` liveness proves only that a listener is reachable. Multiplayer acceptance must separately cover protocol HELLO, correct room/build, two-client authoritative actions and snapshots, idle periods beyond 35 seconds, reconnect with retained Hero ownership, and interrupted-command retry without duplicate actions. The documented wire protocol is length-prefixed JSON over TCP; this helper adds no TLS or protocol authentication.

## Local safety tests

```bash
PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests -p test_release_safety.py -v
bash -n Deploy/DigitalOcean/run-ascendance.sh
```

The 15 tests use temporary directories, synthetic non-runnable ELF fixtures, and fake service/health programs. They never run systemd, Unity, SSH, or network checks. They cover unsafe paths/artifacts, existing layouts/services, restrictive umasks, immutable configuration, retained data/releases, failed restart/health/timeout, initial activation recovery, and rollback failure reporting. These tests validate release-tool safeguards, not a working game build or live server.
