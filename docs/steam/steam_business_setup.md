# Steam Business Setup Checklist

This checklist is the operational source of truth for `steam-business-setup`.

## Account and legal onboarding

- [ ] Create Steamworks partner account with business owner as admin.
- [ ] Complete Steam Direct fee payment for this app.
- [ ] Complete tax interview and confirm payout banking details.
- [ ] Invite release managers, build engineers, QA leads, and community managers with least-privilege roles.
- [ ] Confirm support email and legal contact email.

## App and depot reservation

- [ ] Create Steam app entry and reserve final product name.
- [ ] Record App ID in `tools/steam/steam_release_config.json`.
- [ ] Create depots:
  - [ ] `windows-main`
  - [ ] `windows-llm-runtime`
  - [ ] `windows-symbols` (optional but recommended)
- [ ] Configure branches:
  - [ ] `default` (public)
  - [ ] `beta-internal`
  - [ ] `rc-candidate`

## Compliance forms and disclosures

- [ ] Complete content survey in Steamworks admin.
- [ ] Complete age ratings workflow (IARC and region-specific needs).
- [ ] Complete AI-generated/live-generated disclosure using `docs/steam/steam_ai_disclosure.md`.
- [ ] Attach privacy policy URL and EULA URL from `docs/legal/`.

## Store launch prerequisites

- [ ] Upload capsule/header/library assets (see `docs/steam/store_marketing_checklist.md`).
- [ ] Publish Coming Soon page.
- [ ] Confirm wishlist CTA and trailer visibility.
- [ ] Set launch timing and release region matrix.

## Required evidence to attach in release ticket

- [ ] App ID and depot IDs screenshot.
- [ ] Partner role matrix screenshot.
- [ ] Completed AI disclosure form screenshot.
- [ ] Completed tax/banking verification screenshot.
- [ ] Link to approved EULA and privacy policy documents.
