# Ascendance Unity 0.8.58c — Location Runtime Cascade Hotfix

Built from 0.8.58b.

## Fixed
- L065 Grand Market reveal draw-2/choose-1 parser accepts the authored semicolon wording.
- Regional Encounter deck routing now falls back to authored region for Ironspine Mountains, Verdant Reach, Storm Coast, and Sunscar Expanse when `encounterDeck` is `None`.
- L094 Stonegate Pass recognizes the authored `Costs +1 Action to leave while a Cave-In Token is here` wording.
- Validator uses `ORDER0_1_2` for L098's three-card post-Search Location reorder, preventing the stale reward choice that caused cascading CONTENT PENDING failures.

No other Location rules are intentionally changed.
