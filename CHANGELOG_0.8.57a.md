# Ascendance Unity 0.8.57a — World Event / Quest Count Parser Hotfix

## Root cause fixed

The generalized Quest objective counter in `QuestRuntime0856.cs` could begin a numeric match inside an authored ID. Examples found by the full catalog audit:

- Q024: `L087 ... 3 Crown Ward` could infer `87` and clamp the Crown Ward target to 8.
- Q062: `LT185 ... Wagon supply cache` could infer `185` and clamp the cache target to 8.
- Q072: `L235 ... Solar Crown Ward` could infer `235` and clamp the ward target to 8.

0.8.57a requires numeric counts to begin outside an alphanumeric identifier, so these resolve to their authored targets: Q024 = 3, Q062 = 1, Q072 = 3.

## Duplicate generalized objective fix

The noun parser could match both a broad and specific term against the same authored victory sentence. Identical Interact labels are now de-duplicated. Audit-confirmed fixes:

- Q040: one `Restore all 3 Spirit Waystones` goal.
- Q062: one `Recover LT185 and at least one Wagon supply cache` goal.
- Q072: one `Destroy all Solar Crown Wards` goal.

## World Event regression

The WE007/Q024 checks now validate the full chain:

- base printed setup = 3 Crown Wards,
- Royal Seal = 2 Crown Wards,
- WE007 incomplete-chapter penalty = 4 Crown Wards.

The validator now executes **62 checks**.

## Unity validation

**Ascendance → Validation → Test 0.8.57a - Full World Event Runtime Execution (Play Mode)**

Expected:

`[ASCENDANCE][0.8.57a TEST] PASS — Full World Event Runtime Execution completed: 62 passed, 0 failed.`
