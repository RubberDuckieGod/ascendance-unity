# Ascendance Unity 0.8.43 — Save / Quit / Continue UX Polish

Built from the runtime-verified 0.8.42a baseline.

## Player-facing UX
- Manual save feedback now reports success/failure, exact Continue destination, and saved timestamp.
- Save failure explicitly preserves and calls out the previous safe campaign save.
- Pause menu Quit to Menu and Quit Game now require confirmation.
- Confirmation offers Save & Quit, Quit Without Saving, and Cancel.
- Save & Quit only exits after a successful save.
- Unsafe states disable Save & Quit while keeping explicit Quit Without Saving available.
- Quit Without Saving suppresses the automatic application-quit save so the player's choice is honored.
- Main Menu warns before starting a new campaign when a valid campaign save already exists.
- Main Menu Quit now requires confirmation.
- Continue button names the saved resume surface when a valid save exists.
- Safe autosave records the reason/status for debugging and player-support visibility.
- Downtime and Quest Results now show visible save status instead of silently saving.

## Locked behavior
- No campaign, combat, Quest, crafting, trading, loadout, Downtime Activity, or ready-up rules changed.
- Save schema remains compatible with 0.8.42a.
