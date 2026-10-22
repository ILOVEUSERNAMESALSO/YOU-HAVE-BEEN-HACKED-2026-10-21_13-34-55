# Verification record

Date: 21 October 2026. Platform: Windows 11 x64. Editor: Unity 6000.5.1f1, matching the existing workspace.

## Results

- **33 EditMode tests passed, 0 failed, 0 skipped.**
- **Windows x64 development build succeeded.**
- **21 rendered UI checks passed at 1600 × 900.**
- **21 rendered UI checks passed at 1280 × 720.**
- Both runs completed an investigated five-case shift and a second route that intentionally missed the grandmother attack, then reached the evaluation.
- Gameplay checks reported no Unity Exception, Error or Assert events.

The existing workspace was open in a separate Editor session. Tests and builds therefore ran from **Builds/VerificationProject**, an isolated copy of Assets, Packages and ProjectSettings. Game C#, JSON, USS, TSS and assembly definitions were compared against the workspace before delivery. Generated scene/panel references were synchronized after an import-time copy race was found and corrected. The player's GameManager also recovers its panel asset from Resources if a scene reference is missing.

## Automated coverage

The EditMode suite covers schema validation, duplicate IDs and invalid references, date boundaries, the expired passport, paired comparisons, unknown/empty queries, index filtering, extensible records, decision-state guards, all-five-case completion, reset, canonical/perceived identity separation, evidence prerequisites, idempotence, unlocked dialogue, conditional approval, all final-case dispositions, evaluation counts, save round trips and malformed saved references.

The runtime driver activates the actual UI Toolkit controls through navigation submit events. It checks the title and replay controls, opening and docking documents, field inspection, comparing two fields, empty searches, evidence pinning, dialogue, cancel/confirm behavior, the grandmother overlay, integrity restoration, identity revelation, all five dispositions, evaluation counts, full replay reset, and the alternate missed-attack handoff without false detection credit.

Screenshots are GPU-rendered from the same runtime UI into a RenderTexture. This permits verification while the helper window stays hidden. It is not a mockup or browser recreation. The normal player renders to its window; the texture override is exclusive to the development QA argument.

## Artifacts

- Test XML: **Artifacts/QA/editmode-results.xml**
- Windows build: **Builds/Windows/NotJustAcceptButDeny.exe**
- Build log: **Artifacts/QA/build.log**
- Per-resolution results: **Artifacts/QA/1600x900/probe-results.json** and **Artifacts/QA/1280x720/probe-results.json**
- Ten screenshots per resolution: title, documents, expiration, search, grandmother perception, integrity reveal, restored identity, protection evidence, evaluation and larger text.

Screenshots were visually inspected for layout, text wrapping, document readability, the reveal dialog, and evaluation. Compact labels and document field spacing were adjusted from those observations.

## Qualifications

- This is automated domain and runtime-UI verification, not an external human usability study. The 10–20 minute target has not been timed with outside players.
- The runtime driver uses UI events rather than physical mouse input. Dragging and every possible keyboard-focus sequence have not received a full manual accessibility audit.
- Saves were tested for JSON round trips and invalid references. The smoke driver deliberately bypasses the player's disk autosave so it does not overwrite a real shift.
- The D3D12 engine logs contain debug-info-queue and shutdown ComputeBuffer diagnostics; one offscreen run also emitted a graphics ring-buffer warning. The rendered routes completed, and no gameplay exceptions were reported. A dedicated graphics/performance audit remains outside this slice.
- One shift, placeholder procedural art, no voice acting, no campaign, no localization, no live networking or LLM integration.

## Reproduce

Close other Editors using the target project and run **Tools/Verify.ps1**, or supply an isolated copy using **-ProjectPath**. The script runs tests, builds the Windows player, then runs both resolutions. **-SkipTests**, **-SkipBuild**, **-SkipProbe**, and **-Resolutions** support focused reruns.

For manual play, launch the executable without QA arguments, or open **Assets/Checkpoint/Scenes/Checkpoint.unity** in Unity and press Play.
