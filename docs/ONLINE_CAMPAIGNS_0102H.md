# Unity online campaigns

New campaign setup requires the matching server and client update. Downloading a new client alone does not reset the server campaign. If the game says a server update is required, the owner must apply the reviewed server upgrade first. This guide does not confirm that the DigitalOcean server has already been updated.

The upgrade keeps the current campaign and retains a backup. That campaign remains active until the creator explicitly creates or loads another campaign.

## Install the game for each player

1. Extract the complete matching Windows game ZIP. Keep `Ascendance.exe`, `Ascendance_Data`, the libraries and both multiplayer launcher files together.
2. Put the existing private `Ascendance.invite.json` beside `Play-AscendanceMultiplayer.cmd`. The owner can share that invitation privately with friends; it is not included in the public download.
3. Run **Play-AscendanceMultiplayer.cmd from the new folder** and enter your usual player name. Friends use their own names. They do not need Unity or server SSH access.

Keep your existing local player data when updating. Do not copy an old launcher over the new one. Use a folder name without square brackets.

## Create a fresh campaign with all 16 Heroes

1. Have everyone return to the main menu. Campaign switching is blocked while another player's game session is connected or gameplay is still resolving.
2. The creator chooses **NEW ONLINE CAMPAIGN**, then confirms **CREATE LOBBY**. The previous campaign is saved separately and remains available in **SAVED CAMPAIGNS**.
3. The new lobby offers **all 16 Heroes**. Each player selects one available Hero and presses **CLAIM HERO**. The party supports one to four players, each controlling a different Hero.
4. Friends open their own multiplayer launcher, choose **JOIN ONLINE CAMPAIGN**, then select and claim their Heroes in that same lobby.
5. Each player presses **MARK READY** when their choice is final. Selecting and claiming a different Hero clears that player's readiness; mark ready again afterward.
6. Once every claimed player is connected and ready, the creator presses **START CAMPAIGN**. Everyone enters the fresh campaign with the selected party at Level 1.

**Have all friends claim their Heroes before START CAMPAIGN.** The party roster locks once the campaign starts; starting alone does not reserve additional Heroes for friends who join later.

Before starting, **NOT READY** lets a player withdraw their ready status. **BACK** releases that player's draft Hero for someone else to choose. An unexpected disconnection or crash keeps the Hero reserved; reconnect on the same computer and Windows account, choose the same Hero, and claim it again. The creator waits for that player to reconnect and become ready.

## Continue or load a saved campaign

For the active campaign, choose **JOIN ONLINE CAMPAIGN**, select your existing Hero, and choose **JOIN CAMPAIGN**. Only that saved campaign's party appears after it has started.

To switch campaigns, have everyone return to the main menu and let pending gameplay finish. The creator chooses **SAVED CAMPAIGNS**, selects the desired save, chooses **LOAD CAMPAIGN**, and confirms **LOAD CAMPAIGN**. The current campaign is saved separately before switching. **PREVIOUS** and **NEXT** show additional save pages. **JOIN CURRENT** opens the campaign that is already active.

Loading an old campaign restores its saved Heroes, progression, board state and reconnect identities. Each returning player should use their original computer and Windows account to reclaim their existing Hero.

## Creator and invitation access

The creator's campaign-management credential is stored in local Unity player data on that computer and Windows account. Keep that player data; restarting the game retains it. The shared invitation grants room access, not creator controls. Do not share personal player data or the creator credential with friends.

If **START CAMPAIGN** is unavailable, check that the creator is on their original computer, every player has claimed one Hero, and every claimed player is connected and ready. Reserved Heroes need their original player to reconnect; intentional **BACK** releases a draft choice.
