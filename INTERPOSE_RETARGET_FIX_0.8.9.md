# Interpose Retarget Regression — 0.8.9

## Live-test finding from 0.8.8
The Captain targeted Rowan and Interpose was correctly offered. Interpose resolved and was discarded, but the Attack still hit Rowan.

## Root cause
The generic retarget detector expected the contiguous phrase `become the target instead`. Interpose is authored as `You become the target of that Attack instead`, so the detector returned false. Its +1 Armor modifier then incorrectly applied to Rowan because the pending target never changed.

## Fix
A legal before-Attack reaction is treated as a retarget effect when its rules contain both `become the target` and `instead`. This matches Interpose and the compatible wording already used by Aldric effects without depending on an exact intervening phrase.

## Expected live log
After choosing Interpose, expect a `REACTION RETARGET — Interpose: Rowan Greenward → Sir Aldric Ironwall` entry. The subsequent `ENEMY ATTACK` line must name Sir Aldric Ironwall, not Rowan Greenward.
