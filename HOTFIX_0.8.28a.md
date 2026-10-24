# Ascendance Unity 0.8.28a — Exploration Resolver Compile Hotfix

- Fixed CS0103 in `ExplorationRuntime0828.cs`: `TryResolve0828ExplorationChoice` incorrectly referenced the UI-only `options` list.
- The `ExplorationChooseInspect` resolver now parses `INSPECT#`, persists the selected Location, and transitions the existing choice to `ExplorationInspectConnected` for the authored Reveal/Leave decision.
- Runtime version remains 0.8.28 because no authored gameplay rules were changed beyond making the intended SD006 multi-location choice executable.
- Added static compile guard: the exploration choice resolver may not reference `options.Add`.
