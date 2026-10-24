# 0.8.14 Post-Quest Free Exploration

The Quest result and the gameplay loop are separate state.

`QuestComplete == true` means the Q001 victory conditions were satisfied and the result is recorded. It is **not** permission to bypass or disable the rest of Ascendance.

After completion:

- Hero Phase continues normally.
- Start Turn / End Turn continue normally.
- Action costs remain enforced.
- Movement and engagement rules remain enforced.
- Explore remains available whenever its normal prerequisites are met.
- Combat and Reactions remain active.
- Healing Potion and revival rules remain active.
- Health, Downed, and Dead remain active.
- Save/Continue preserves the completed Quest state while normal Hero state continues to change.

A completed Quest is not retroactively converted to failure because a Hero—or even the whole party—is defeated later during voluntary post-Quest exploration. Those later defeats still happen mechanically and remain part of the continuing game state.
