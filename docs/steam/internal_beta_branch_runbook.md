# Internal Beta Branch Runbook

Branch name: `beta-internal`

## Goal

Validate release candidate builds with trusted testers before default branch promotion.

## Process

1. Build candidate with `scripts/Build-Steam.ps1`.
2. Upload with `scripts/Upload-Steam.ps1` using app build VDF configured for beta branch.
3. Assign branch password in Steamworks if needed.
4. Invite internal testers and request:
   - startup + save/load validation,
   - LLM first-run validation,
   - controller and accessibility validation.
5. Collect issues in tracker and patch on hotfix branch.
6. Re-upload candidate and repeat until all release blockers are closed.

## Promotion gate

- No blocker severity bugs.
- Smoke checks pass.
- Save migration pass confirmed.
- AI disclosure text approved and aligned with final build behavior.
