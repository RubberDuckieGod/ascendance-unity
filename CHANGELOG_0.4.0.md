# Ascendance Unity 0.4.0 — Production Asset & Animation Foundation

## Locked baseline
Built directly on stable 0.3.2. The working camera/HUD, Location movement, NavMesh, targeting, Q001, Actions, Hero automatic-hit rule, and Enemy d6 rule remain authoritative.

## Production character bridge
- Added convention-based loading for real Hero and Enemy prefabs.
- Prefabs are optional; all 0.3.x procedural characters remain automatic fallbacks.
- Added safe Animator parameter adapter for locomotion, attacks, hit, death, downed, and revive.
- Added optional per-character `CharacterAnimationProfile` assets.
- Added standardized equipment socket discovery/creation for right hand, left hand, back, hip, staff, and bow.

## Animation-timed combat
- Hero damage is now committed at the presentation impact event rather than at button press.
- Enemy damage is likewise committed at the attack impact.
- Production clips may call `AscendanceImpact` and `AscendanceAttackFinished` Animation Events.
- Missing Animation Events cannot lock combat: each attack has an unscaled-time impact fallback.
- Groundbreaker uses one heavy attack presentation and resolves all legal affected enemies on impact.

## Combat feel hooks
- Added short presentation-only hit stop.
- Added camera impulse/shake support.
- Added optional production VFX hook (`Hit_Impact.prefab`) with the existing procedural hit flash as fallback.

## Environment replacement architecture
- Stonebrook can now automatically replace House, Tree, Bush, Rock, Bridge, Well, Wagon, Crates, and Brazier blockout pieces with production Resources prefabs.
- Runtime NavMesh source collection now includes MeshFilters inside child objects of walkable prefabs.

## No third-party dependencies
0.4.0 keeps the same minimal package manifest as 0.3.2 and does not reintroduce the incompatible Input System package.
