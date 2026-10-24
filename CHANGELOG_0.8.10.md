# 0.8.10 — Enemy Attack Damage Reaction Timing

- Enemy d6 rolls now occur **after** pre-Attack target/protection reactions resolve.
- Damage-triggered and defeat-triggered Reactions are no longer offered during the target-declaration window.
- Added `HeroDamageSourceKind.EnemyAttack` so Enemy Attack damage is distinguishable from Enemy abilities, Conditions, and Hero self-damage.
- Enemy Attack damage now enters the centralized `BeginHeroDamage(...)` transaction instead of directly changing Hero Health.
- Preserves pre-Attack Armor, damage reduction, cancellation, retarget, and defeat-floor protection when the damage transaction begins.
- Preserves the actual attacking Enemy as the source passed to `HeroRulesRuntime.OnHeroDamaged(...)`.
- Damage-window revalidation still removes no-longer-legal lethal or damage reactions after each Reaction resolves.
- `Play after you suffer damage from an Enemy Attack` reduction reactions can now resolve in the post-roll damage window where the pending damage is known.
- Post-Attack reaction timing remains after final damage is applied.
- Added a deterministic Editor helper for Captain → Garruk damage timing with a forced d6 of 6.
