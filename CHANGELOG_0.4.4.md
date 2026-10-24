# Ascendance Unity 0.4.4 — Unity 6.6 Presentation Lifecycle Cleanup

## Fixed
- Moved `MaterialPropertyBlock` construction out of `CharacterPresentationController`'s MonoBehaviour field initializer and into `Awake()`.
- Added a defensive lazy initialization guard before production-prefab highlight rendering.
- This removes Unity 6.6's `CreateImpl is not allowed to be called from a MonoBehaviour constructor (or instance field initializer)` exception during Hero/enemy spawning.

## Audit
- Scanned runtime C# field initializers for native-backed Unity objects created with `new`.
- `MaterialPropertyBlock` was the remaining offending production/runtime initializer.
- Managed collections and value types (`List`, `Dictionary`, `HashSet`, `Color`, `Vector3`) remain field-initialized because they do not create Unity native objects.

## Baseline
- Preserves the 0.4.3 NavMesh/state fix.
- Preserves 0.4.x production prefab, animation, socket, impact timing, VFX, hit-stop, and environment replacement architecture.
- Preserves the stable 0.3.2 camera/HUD/gameplay behavior.
