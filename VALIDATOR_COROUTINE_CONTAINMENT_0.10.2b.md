# Validator Coroutine Containment — 0.10.2b

The production rules were correct. The cross-test leak was created by editor validation: ending Lyra as the final unfinished Hero starts `ResolveRound()`, but the 0.10.2a router validator restored its snapshot before `ResolveRound()` finished. `ApplySaveData()` correctly cleared transient flags, while the already-scheduled coroutine remained alive and later resumed during the TCP test.

0.10.2b contains that coroutine at its source. The router validator waits until the next Hero Phase has begun and remains stably idle before restoring state. The network validator independently requires sustained idle/startable windows before issuing the real TCP command.
