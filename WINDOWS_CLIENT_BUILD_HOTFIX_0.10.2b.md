# Ascendance 0.10.2b — Windows Client Build Tool Hotfix

This hotfix does not change Ascendance gameplay, network protocol, or BuildVersion.

It fixes the Windows standalone client builder after a Dedicated Server build by explicitly setting:

```csharp
subtarget = (int)StandaloneBuildSubtarget.Player
```

Without this explicit Player subtarget, Unity 6 can retain the previous Dedicated Server standalone subtarget and incorrectly report:

`Error building Player: Dedicated Server support for Win is not installed.`

Apply over the existing 0.10.2b project and rerun:

`Ascendance > Build > Build Windows Standalone 0.10.2b`
