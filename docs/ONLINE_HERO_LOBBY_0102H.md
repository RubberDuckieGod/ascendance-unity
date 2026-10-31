# Online Hero selection client

Launch the multiplayer launcher, choose **JOIN ONLINE CAMPAIGN**, select one Hero from the server party, then **JOIN CAMPAIGN**. The server confirms ownership before gameplay. AVAILABLE, PLAYING and RESERVED labels show live player claims. A saved login offers a reconnect attempt; the server verifies it. If another player claims the same Hero first, the refusal stays visible and another Hero can be selected.

This client keeps protocol 1 and runtime version 0.10.2h so the existing secure server remains compatible. Runtime source is `8c143b7e6a7b5ec3c151b7692dbc7ed57cf48860`. The server and existing campaign need no deployment or reset. Solo party selection remains unchanged.

Download the complete client ZIP from [the client-only release](https://github.com/RubberDuckieGod/ascendance-unity/releases/tag/unity-0.10.2h-online-lobby-8c143b7). Extract the entire Windows folder, copy the existing private Ascendance.invite.json beside the launcher, and run Play-AscendanceMultiplayer.cmd. Keep your usual Windows profile and local player data to retain Hero reconnect credentials. Friends use the same invitation privately and choose different Heroes. Folder names must not contain square brackets.

Validation: 207 Unity checks, including 31 lobby checks; 196 helper fixtures; 41 checks using two actual graphical clients over pinned TLS with the unchanged frozen h server. The test includes joining during another Hero's turn, refusing wrong-turn input before network submission, saved owner reconnect, campaign preservation and untouched local-save sentinels. Both menu and world screenshots were visually reviewed after advancing rendered frames. Windows and Linux development builds succeeded from clean source. [Expected/actual summary and scope limits](ONLINE_HERO_LOBBY_0102H.json).

Windows gameplay and audio output are not certified. The actual graphical players ran Linux, and the host's missing audio device causes FMOD messages in development screenshots. The automated GUI exercises button callbacks; physical pointer clicks were not automated. No production campaign, invitation or player credential was used for gameplay testing.
