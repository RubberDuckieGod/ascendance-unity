# Ascendance Unity 0.8.31a — Q008 Champion Runtime Hotfix

Built from 0.8.31 after Unity Play Mode exposed two validator failures and one missing runtime enemy template.

## Runtime fix
- Added E015 Goblin Champion to `SearchEncounterContent0820.EnemyTemplates` with authoritative workbook stats: 14 Health, 4 Attack, 2 Armor.
- Q008 final Siege Engine destruction can now spawn its required Goblin Champion instead of logging CONTENT PENDING.

## Validator fixes
- WE003 Archer assertion now proves the authored modifier exactly: effective Attack equals that Archer's stored base Attack + 1.
- The Q008 Champion completion path now guards a missing/null Champion ID and records a normal failed assertion instead of indexing the enemy dictionary with a null key.

No Q006, Q007, Siege Engine 10 Health / 2 Armor balance, reward, save-state, or campaign-carryover rules were changed.
