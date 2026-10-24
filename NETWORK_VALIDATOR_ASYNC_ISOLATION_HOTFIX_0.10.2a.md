# Network Validator Async-Isolation Hotfix — 0.10.2a

The 0.10.2 socket command reached `AscendanceTcpServer0102.Update()` and `AscendanceRoomAuthority0100.Execute()` on the Unity main thread, but `TryStartHeroTurn` correctly rejected the command because an inherited asynchronous movement/reaction/resolution path was still active. This was cross-test contamination from the nested router/server/core regression stack, not a transport failure.

0.10.2a drains those transient runtime gates before the isolated socket test begins and verifies that the runtime reached an idle boundary. The authoritative START TURN rule remains unchanged.
