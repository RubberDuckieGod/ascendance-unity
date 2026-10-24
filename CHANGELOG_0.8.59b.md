# Ascendance Unity 0.8.59b — Enemy Movement / Reach Runtime Hotfix

Built from the 0.8.59a compile-corrected Enemy Runtime candidate.

## Fixed
- E013 Goblin Wolf Rider validator now checks its authored Movement 3 before World Event modifiers; WE001 may correctly raise effective Movement to 4 before activation.
- E102 Kraken Spawn `Reach` is now live: although printed Range is Melee, it may attack a connected Hero when no Hero is in its current Location, at the already-implemented -1 Attack modifier.
- Validator now places Heroes outside the Kraken Spawn's Location before testing Reach, matching the authored condition.

No other Enemy behavior was intentionally changed. Validator target remains 64 checks.
