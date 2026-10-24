# Ascendance Unity 0.8.19a — Validation Harness Hotfix

Runtime log validation of 0.8.19 exposed two failures in the Play Mode validator: Dense Woods optional Hide and Ancient Ruins free Search. Investigation confirmed both gameplay paths were already correct. The validator invoked the Reveal resolver directly without first setting the tested Locations to Revealed, unlike the real Explore pipeline.

0.8.19a changes only the validator setup so Dense Woods, River Crossing, and Ancient Ruins are marked Revealed before direct Reveal-effect calls. Runtime permission guards remain unchanged.

Static validation: 276/276 checks passed after hotfix.
