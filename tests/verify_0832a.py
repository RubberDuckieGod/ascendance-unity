from pathlib import Path
root=Path(__file__).resolve().parents[1]
q30=(root/'Assets/Scripts/Core/QuestRuntime0830.cs').read_text()
val=(root/'Assets/Editor/QuestBatchQ009Q012TestTools0832.cs').read_text()
checks=[]
def check(label,cond):
    checks.append((label,cond)); print(('PASS' if cond else 'FAIL')+' — '+label)
check('primary capability routes final batch', 'if(IsQuestBatch20831||IsQuestBatchFinal0832)return CanQuestPrimaryAction0831(hero);' in q30)
check('secondary capability routes final batch', 'if(IsQuestBatch20831||IsQuestBatchFinal0832)return CanQuestSecondaryAction0831(hero);' in q30)
check('primary execution routes final batch', 'if(IsQuestBatch20831||IsQuestBatchFinal0832){QuestPrimaryAction0831();return;}' in q30)
check('secondary execution routes final batch', 'if(IsQuestBatch20831||IsQuestBatchFinal0832){QuestSecondaryAction0831();return;}' in q30)
check('q010 validator progress guard', 'gateAttackGuard0832++<10' in val and 'beforeGate0832' in val and 'if(gm.DebugGateHealth0832>=beforeGate0832)break;' in val)
check('q010 validator asserts destroyed gate after guarded loop', 'Q010 repeated Gate attacks make progress and destroy the Fortress Gate without a validator hang.' in val)
check('hotfix changelog', (root/'CHANGELOG_0.8.32a.md').exists())
failed=[x for x in checks if not x[1]]
print(f'RESULT: {len(checks)-len(failed)}/{len(checks)} checks passed')
raise SystemExit(1 if failed else 0)
