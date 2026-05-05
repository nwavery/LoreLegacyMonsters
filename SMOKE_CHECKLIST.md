# Pre-alpha smoke checklist

Run **before** sending a build to internal testers. Check each box.

## Automated

- [ ] Run `.\scripts\Invoke-UnityBatchTask.ps1 -Task edit-tests` (non-zero discovered tests, green run).
- [ ] Confirm EditMode artifacts were produced in `Artifacts/TestResults/`:
  - `unity-editmode-batch.log`
  - `editmode-batch-summary.json`
  - `editmode-batch-results.xml`
- [ ] Run `.\scripts\Invoke-UnityBatchTask.ps1 -Task smoke-full` (all assertions pass).
- [ ] Run `.\scripts\Invoke-UnityBatchTask.ps1 -Task smoke-main-menu` (all assertions pass).

## Main menu (standalone build)

- [ ] **Test LLM connection** with Ollama running → success toast.
- [ ] **Test LLM** with Ollama stopped → failure toast (clear message).
- [ ] **Help** / **About build** open and close.
- [ ] **New Game** → loads game scene, player can move.
- [ ] **Load Slot 0** works when a save exists; sensible message when missing.

## In-game (10–15 min)

- [ ] **F1** opens/closes help; **Esc** closes help when open.
- [ ] **F5** save / **F9** load (slot 0) works from overworld (not in battle).
- [ ] Talk to an **LLM-enabled** NPC → reply or scripted fallback + toast on failure.
- [ ] Complete or advance one **quest objective** (tracker updates).
- [ ] Enter **combat**, win or flee; **Space** closes result when applicable.
- [ ] **Auto-save**: wait for interval (default 90s) or trigger manually — no silent failure (toast if save fails).

## Regression targets

- [ ] `ContentCatalogValidator` / catalog tests still pass (content refs valid).
- [ ] No accidental **VerticalSliceDebugControls** hotkeys unless `enableHotkeys` is explicitly enabled in scene.

## Optional CI

- [ ] If using CI, use the same wrapper commands so local and CI gates produce identical artifacts/log names.
