# Ascendance Unity 0.8.32a — Final Quest Action Routing Hotfix

Built from 0.8.32 after its first Unity Play Mode validation exposed a cross-version Quest-action dispatch regression.

## Fixes
- Routes Q009–Q012 primary and secondary Quest actions through the 0.8.31 delegation layer into `QuestRuntime0832`.
- Restores live player actions for Free Captain Roland, Attack Fortress Gate, Attack Ballista, and Destroy Banner.
- Preserves Q001–Q008 routing unchanged.
- Hardens the Q010 validator Gate-destruction loop with a progress/iteration guard so a failed action reports a failed assertion instead of hanging the validation coroutine.

Runtime build banner remains 0.8.32 because this is a hotfix candidate for the same feature batch.
