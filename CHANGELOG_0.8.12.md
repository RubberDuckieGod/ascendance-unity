# 0.8.12 — Round Transition & Reaction Reserve Enforcement

- Built from the runtime-verified **0.8.11 True Hero Turn Start Lifecycle** baseline.
- Added one explicit Reaction payment gate: Reaction cards may spend only a participating, non-Defeated Hero's **general held Actions**. Attack-only temporary Actions cannot pay Reaction costs.
- Every Reaction spend now logs the held-Action transaction, e.g. `3 → 2 remaining this Round`.
- Preserved 0.8.11 End Turn behavior: unused general Actions stay on the completed Hero for legal Reactions during the rest of the Round.
- End-Phase status effects resolve **before** held Actions expire, so legal Burning/condition damage Reactions still have access to the Hero's reserve.
- Consolidated Action expiry into one `ResolveEndPhaseReactionReserves()` path.
- End Phase now logs any unspent Reaction Actions that expire, then forces every participating Hero to 0 Actions / 0 attack-only Actions and resets `TurnCompleted` for the next Round.
- Added `BeginNextHeroPhase()` as the sole Round-start transition. It clears turn ownership and repairs impossible stale Action/TurnCompleted carry-over before player choice resumes.
- The next Hero Phase explicitly starts with **no predetermined turn owner** and **0 Actions on every Hero**.
- Round transitions do not draw cards. Round 2+ drawing remains owned by each Hero's individual `TryStartHeroTurn()` transaction.
- Added deterministic Play Mode validation:
  **Ascendance → Validation → Test 0.8.12 - Round Transition + Reaction Reserve (Play Mode)**.
- The test stages a Captain-only World Phase against Garruk, spends one saved Action on **Ignore the Pain**, verifies End Phase clears the remaining reserve, advances one Round, and verifies Garruk plus a second Hero draw/gain Actions only when each starts their own turn.
