# 0.8.60c validator hotfix

The prior test required both endpoints of an active Quest graph edge to already be revealed. At Q001 startup, that assumption is false. The revised helper finds a genuine Quest-enterable connection first, then reveals the two endpoints for validation only. This preserves actual GraphDistance, target range, and movement legality while avoiding synthetic cross-region test Locations.
