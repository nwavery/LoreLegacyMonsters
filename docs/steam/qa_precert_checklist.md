# Steam Pre-Cert + LLM QA Matrix

Use this checklist for `qa-certification-dryrun`.

## Platform pre-cert

- [ ] Cold launch from Steam client.
- [ ] Alt+Tab in and out during overworld and combat.
- [ ] Minimize/restore behavior.
- [ ] Multi-monitor cursor/focus behavior.
- [ ] Overlay opens (`Shift+Tab`) in full-screen windowed and windowed.
- [ ] Screenshot capture and upload via overlay.
- [ ] Controller hot-plug/unplug behavior.
- [ ] Low VRAM pass (Intel UHD class test machine).
- [ ] 4K and ultrawide UI readability pass.

## LLM-specific matrix

- [ ] Bundled runtime first launch on fresh install.
- [ ] First model response under expected SLA.
- [ ] Port collision simulation (existing ollama on 11434).
- [ ] Bundled mode disabled -> scripted fallback run.
- [ ] Missing model file -> user-facing recovery message.
- [ ] Corrupt model file -> clear failure and fallback.
- [ ] Power-user endpoint override path works.

## Save and migration

- [ ] Load legacy v7 save and confirm migration to v1.0 schema.
- [ ] Verify achievement list and story flags preserved.
- [ ] Verify no regressions when saving/loading repeatedly.

## Telemetry and privacy

- [ ] Crash telemetry remains off by default.
- [ ] Opt-in toggle records error events only.
- [ ] Privacy policy reflects telemetry behavior.

## Internal branch rollout

- [ ] Upload build to `beta-internal` branch.
- [ ] Verify branch install/update on two tester accounts.
- [ ] Run smoke + manual playthrough before promoting to default branch.
