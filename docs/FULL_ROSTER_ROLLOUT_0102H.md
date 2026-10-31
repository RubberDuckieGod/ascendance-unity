# Verified full-roster Unity server rollout

The owner applied `unity-0.10.2h-online-campaigns-597f442-r2` to the existing DigitalOcean service. The installer reported **ACTIVATED_SECURE**, the existing data location preserved, and the existing campaign restored. Runtime source is `597f44215ec2624ff0f215db80c6c487f05a84d7`; its displayed build remains `0.10.2h`.

| Boundary | Recorded result |
| --- | --- |
| Owner-provided on-host result | Secure transport verified; data path and campaign preserved; 80 canary checks passed, zero failed |
| Private copied-save inspection | Verified on loopback; zero gameplay commands submitted to the copied room |
| Independent public connection check | Pinned certificate, validity period, IP address, protocol and build verified; missing room key refused without authority data |
| Public probe scope | One unauthenticated HELLO; no room key, player credentials, JOIN or gameplay command |

[Public verification workflow](https://github.com/RubberDuckieGod/ascendance-unity/actions/runs/41229199010) and [sanitized on-host result](../distribution/owner-r2-activation.json) record the separate evidence boundaries. The public probe does not itself inspect campaign contents; campaign preservation is reported by the on-host installer and its private restore checks. No private invitation, player save or access key was uploaded.

The first installer attempt refused before activation. Revision two handles only the demonstrated, validated presentation-selection differences of the reviewed old h server; gameplay, ordered state, ownership and copied-save checks remain strict. The runtime game binaries are unchanged from the tested full-roster release.

## Opening the new campaign

Use the matching [Windows client](https://github.com/RubberDuckieGod/ascendance-unity/releases/download/unity-0.10.2h-online-campaigns-597f442-r2/ascendance-windows-unity-0.10.2h-online-campaigns-597f442-r2.zip) and existing private invitation. An already installed full-roster 597 client can be kept. Actual Windows player launch confirmation remains separate from the server and Linux graphical test results.

The upgrade retains the active campaign. To begin fresh, have everyone return to the main menu. The creator chooses **NEW ONLINE CAMPAIGN → CREATE LOBBY**. Each friend then joins, claims a different Hero from all 16, and marks Ready before the creator starts. The roster locks after start; the original adventure remains in **SAVED CAMPAIGNS**.

[Player instructions](ONLINE_CAMPAIGNS_0102H.md) · [Expected/actual validation and remaining limits](ONLINE_CAMPAIGN_VALIDATION_0102H.md)

The testing branch remains separate from `main`. This DigitalOcean rollout does not deploy the website changes in the other repository.
