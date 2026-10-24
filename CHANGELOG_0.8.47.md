# Ascendance Unity 0.8.47 — Interactive High-Fantasy Campaign Hub

Built from the runtime-verified 0.8.45a baseline. This replaces the poster-style Campaign II presentation experiment with a real, fully live Unity UGUI screen.

## Player-facing changes
- High-fantasy Valewatch skyline is decorative atmosphere only; no functional text or controls are baked into artwork.
- Three-column Campaign II hub: campaign/party, selected quest details, and campaign progression.
- Four live World Event groups using strict sequential Quest blocks:
  - WE005: Q013–Q015
  - WE006: Q016–Q018
  - WE007: Q019–Q021
  - WE008: Q022–Q024
- All 12 Quest rows are actual Unity Buttons and remain clickable for inspection.
- Selecting a Quest updates a live detail panel with Quest title, role, World Event, chapter timing, and runtime availability.
- Main/Optional/Final is a label; it never reorders the Quest number sequence.
- Live selected-row highlight and hover/pressed button states.
- Save Campaign II, Begin Selected Quest, and Return to Main Menu are real Unity buttons.
- Begin Selected Quest is driven by the authoritative `CanBeginCampaignQuest0833`; it remains disabled while Q013 runtime is not implemented and will become live automatically when a scenario is available.

## Safety / authority
- No Campaign II gameplay runtime is added in this milestone.
- Q013 remains unable to launch or consume a Campaign Day.
- Campaign I archive, Campaign II identity, save/resume, economy, reputation, Hero persistence, and authority remain unchanged from verified 0.8.45a.
