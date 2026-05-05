# Support and Refund-Risk Playbook

## Severity levels

- **S0**: crash/blocker, progression loss, unplayable startup.
- **S1**: major feature regression, save sync failures.
- **S2**: quality issues, balancing, UX rough edges.
- **S3**: cosmetic/non-blocking requests.

## Refund-risk indicators

- Startup crash before menu.
- Missing expected local AI behavior with no fallback.
- Severe performance on minimum specs.
- Save corruption or cloud mismatch.

## Response SLA

- S0: acknowledge within 2 hours, hotfix triage same day.
- S1: acknowledge within 8 hours, patch plan within 24 hours.
- S2/S3: weekly review and bundled patch planning.

## Required ticket fields

- Build version + branch.
- Hardware profile.
- Repro steps.
- Player log path and excerpt.
- Whether local AI feature was enabled.
