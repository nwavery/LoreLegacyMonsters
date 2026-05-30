# Completion Roadmap: Lore, Legacy, and Monsters

**Status target:** take the project from a playable alpha (~65% feature-complete) to a polished,
content-complete, shippable **Steam 1.0**.

This roadmap is grounded in the gaps tracked in [`PROGRESS.md`](PROGRESS.md) (the 🔄 / ❌ items),
[`README.md`](README.md), and [`SMOKE_CHECKLIST.md`](SMOKE_CHECKLIST.md), plus one pillar the current
docs never mention — **audio** — which is required for a 1.0.

## What "100%" means

"Done" requires **all four** of these to be true:

1. **Systems complete** — no gameplay system is "partial."
2. **Content complete** — a full campaign with a real ending, plus endgame content.
3. **Production values complete** — audio, visual distinction, UX, performance, balance.
4. **Release/legal/ops complete** — real Steam App ID, store page, finalized legal/AI disclosure.

The work splits into **5 phases**. Phases 1–2 are the bulk (mechanics + content), Phase 3 is
production polish, Phase 4 is hardening, Phase 5 is launch.

---

## Phase 1 — Close the open core systems

*Goal: no gameplay system is "partial." These are completions, not rewrites — the foundation is solid.*

- [ ] **Monster Evolution** — wire level/item/condition triggers, evolution animation/notification,
      persist results in saves. *(PROGRESS: 🔄)*
- [ ] **Advanced Combat** — full type-effectiveness matrix, all status effects (DoT, stat stages,
      control), stage/weather interactions finalized. *(PROGRESS: 🔄)*
- [ ] **Day/Night Cycle** — time progression affecting encounters, NPC availability, and visuals;
      saved/restored. *(PROGRESS: 🔄, not yet implemented)*
- [ ] **World Events** — time/condition-triggered events (tie at least a few to the Chapter 3 storm
      region); saved state. *(PROGRESS: 🔄)*
- [ ] **Quest Chains / branching** — prove branching/dependency support with ≥1 real branching
      questline. *(PROGRESS: 🔄)*
- [ ] **Map UI** — standalone discovered-area map screen with quest markers (the `M` key is already
      reserved for this). *(PROGRESS: 🔄)*

**Exit criteria:** the PROGRESS.md "Systems in Progress" list has zero 🔄 entries.

---

## Phase 2 — Content complete

*Goal: a full campaign arc with a real ending, plus an endgame layer.*

- [ ] **Main story climax + ending** — author the Chapter 3 conclusion (Varo / Storm Tyrant arc
      resolution) and a credits flow. *(PROGRESS: 🔄 Main Story)*
- [ ] **Monster database** — fill the roster to an explicit target (suggested **~50–60 species**),
      complete every evolution line, ensure each region has encounter identity. *(PROGRESS: 🔄)*
- [ ] **Item database** — finish unique-effect items (status cures, stat boosters, evolution items,
      key items for Phase-1 quests). *(PROGRESS: 🔄)*
- [ ] **Legendary / endgame monsters** — author the true endgame-tier captures. *(PROGRESS: ❌)*
- [ ] **Boss trainer ladder** — a structured post-game challenge ladder beyond existing major
      trainers. *(PROGRESS: ❌)*
- [ ] **Side quests** — broaden regional collector/rumor/mentor lines for replay value.
      *(PROGRESS: 🔄)*

**Exit criteria:** a new game can be played start → credits; post-game content exists; no ❌/🔄 in
the Content sections.

---

## Phase 3 — Production polish

*Goal: it feels like a product, not a prototype.*

- [ ] **AUDIO (largest unlisted gap)** — no music or SFX exists anywhere in the current docs. Add
      area/battle/menu music, combat & UI SFX, an audio manager, and save-persisted volume prefs.
      **Treat this as a first-class workstream, not a footnote.**
- [ ] **Per-area visual distinction** — push beyond the generated-tile pass so the 9 zones read as
      visually distinct. *(Known issue #4)*
- [ ] **UI/UX polish** — settings menu (audio/controls/display), key rebinding, consistent themed
      chrome across all panels. *(Known issue #2)*
- [ ] **Performance** — optimize larger areas; set an explicit frame-rate target. *(Known issue #5)*
- [ ] **Game balance** — difficulty curve, XP/economy tuning, capture rates. **Do this after content
      is locked (Phase 2).** *(Known issue #1)*

**Exit criteria:** a settings menu exists, audio is in, and known issues #1/#2/#4/#5 are resolved.

---

## Phase 4 — Test & hardening

*Goal: confidence to ship. Fold test-writing into Phases 1–2 as systems/content land — don't defer it all here. Phase 4 is then hardening + end-to-end, not backfill.*

- [ ] **Monster / Evolution tests** — close the thin coverage. *(PROGRESS: ❌)*
- [ ] **Game Controller end-to-end flow tests** — new-game → quest → combat → save/load → chapter
      advance. *(PROGRESS: ❌)*
- [ ] **Presentation-layer flow tests** — expand beyond current modal/toast/quest-priority coverage.
- [ ] **Save hardening** — broader failure-path / migration coverage. *(Known issue #3)*
- [ ] **Full smoke pass** — run `SMOKE_CHECKLIST.md` + visual smoke on a real Windows build; get the
      LLM scenario suite green (`Start-Improvements.ps1` → `exitCode: 0`).
- [ ] **Bug-bash / alpha→beta** — drive the `ALPHA_TESTING.md` flow.

**Exit criteria:** Edit-mode suite green with no ❌ test categories; smoke checklist fully checked on a build.

---

## Phase 5 — Release & ops

*Goal: actually on Steam.*

- [ ] **Real Steam App ID** — swap off Spacewar/480 (`docs/steam` handoff checklist §6), wire real
      depot upload.
- [ ] **Steam achievements** — map the in-game achievement system to Steamworks stats.
- [ ] **Store page** — capsule art, screenshots, trailer, description (scaffolded in `docs/steam/`).
- [ ] **Legal finalization** — promote EULA, Privacy Policy, and Steam **AI disclosure** from draft →
      final (required: the game ships a bundled local LLM).
- [ ] **Launch runbook** — execute the `docs/steam/` release/QA/launch checklists.

**Exit criteria:** build uploaded to the real App ID, store page live, legal docs final, launch
runbook complete.

---

## Sequencing & rationale

Mostly linear **1 → 2 → 3 → 4 → 5**, with two parallelization opportunities:

- **Audio (Phase 3)** is largely independent of code-system work — start it immediately, in parallel
  with Phases 1–2.
- **Balance (Phase 3)** must come **after** content lock (Phase 2), or it gets re-tuned repeatedly.
- **Test writing (Phase 4)** should be folded into Phases 1–2 as each system/content lands; Phase 4
  becomes hardening + e2e rather than backfill.

## Biggest risks / watch items

1. **Audio is unscoped** — invisible in current docs but essential for 1.0; likely the single largest
   *new* effort.
2. **Steam App ID swap** is the gating external dependency — an account/business step, not just code.
   Start the Steamworks paperwork early.
3. **LLM feature = ongoing QA tax** — the bundled-Ollama NPC layer needs the scenario suite green per
   content change and adds AI-disclosure/legal obligations. Keep `Start-Improvements.ps1` in CI.
4. **Balance churn** if content isn't locked before tuning.

---

*This roadmap is a planning artifact. Update the checkboxes and the PROGRESS.md status markers as
each item lands.*
