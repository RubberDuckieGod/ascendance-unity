# 0.8.60a Compile Hotfix

Root cause: `EnemyState` exposed `Statuses` but did not expose the `HasStatus(string)` convenience method that `HeroState` already had. 0.8.60 correctly began querying Enemy conditions from Hero-card runtime code, revealing the missing API.

Fix: add the symmetric case-insensitive helper directly to `EnemyState`.
