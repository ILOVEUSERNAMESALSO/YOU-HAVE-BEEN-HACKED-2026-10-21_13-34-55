# Not Just Accept, But Deny

A playable Unity desktop prototype about an AI operating an airport checkpoint. Five travelers share one document desk, local investigation terminal, and decision system.

## Play

**Existing project version preserved: Unity 6000.5.1f1 (Unity 6.5).** This workspace already used that version; it has not been downgraded to 6.3. Windows Build Support is required only to rebuild the executable.

1. Open this project in Unity Hub with **6000.5.1f1**.
2. Let scripts and assets import.
3. Open **Assets/Checkpoint/Scenes/Checkpoint.unity**. If absent, choose **Checkpoint → 1. Create or repair scene assets**, then **Checkpoint → 2. Open starting scene**.
4. Press Unity's **Play** button, then **BEGIN SHIFT**.

The Editor bootstrap creates the scene, camera, runtime UI document, panel settings and build-list entry if missing. Existing template assets and SampleScene are retained.

The verified Windows executable lives at **Builds/Windows/NotJustAcceptButDeny.exe**. Keep the whole Windows folder together. This is a development build; the QA command-line driver is disabled unless explicitly requested.

## Controls

- Click a document tab to open it. Two documents can be open together; opening a third returns the oldest to the tray.
- Click a field to inspect it. Click another field to compare them. Click the selected field again to clear the selection.
- Drag a document header to move it. **DOCK LEFT**, **DOCK RIGHT**, and **ARRANGE** provide alternatives without dragging.
- Scroll within documents, the rulebook, sources, questions, and the evaluation.
- **Rules / Evidence / Log** switch the left panel. **F1** returns to the rulebook.
- Terminal tabs select **Search, Government, News, Social, Travel**. Enter a name, passport number, flight, or phrase; **Enter** or **GO** searches.
- Open a result and select **PIN EVIDENCE** to retain its source and unlock relevant questions.
- **INTEGRITY CHECK** uses collected provenance. It does not decide whether a traveler should enter.
- **APPROVE / DENY / SECONDARY REVIEW** open a confirmation form. Add an optional reason and explicitly confirm.
- **Tab / Shift+Tab** navigate focus; **Enter / Space** activate focused controls. **Escape** closes cancellable dialogs.
- **TEXT** increases important reading text. **SOUND** toggles generated cues.
- **RESTART** clears the shift after confirmation. **PLAY AGAIN** restarts from the evaluation.

There is no timer, required dragging, live browser, live AI service, automatic approval, or randomized outcome. The browser is a fictional local archive. Searches operate within the current case.

## Implemented

- An original UI Toolkit interface: illustrated checkpoint, procedural portraits, paper documents, terminal, rulebook, evidence notebook, dialogue log and explicit decision controls.
- Five authored cases: valid entry, expired passport, an identity collision, a grandmother role-substitution attack, and a protection dilemma.
- Date validation, field comparisons, evidence collection, evidence-dependent dialogue and qualified outcomes.
- Five searchable indexes with source attribution, full records, empty/unknown query handling and evidence pinning.
- Separate canonical and perceived identities. Integrity restoration changes the portrait and relationship without changing canonical facts.
- A compulsory AI-role revelation after the grandmother case even if the attack is missed. Automatic handoff restoration earns no detection credit.
- Structured consequences, citations, preserved rationales and a variable evaluation. No numerical morality score.
- Atomic local autosave, continue, replay, larger reading text, generated audio cues and keyboard focus.
- Scene/bootstrap/build tooling, EditMode tests and a rendered development-player smoke driver.

## Architecture

Authored content: **Assets/Checkpoint/Resources/Checkpoint/cases.json**.

| Area | Responsibility |
|---|---|
| CaseModels, CaseRepository | Serializable schema and reference validation |
| DocumentValidator, FakeBrowser | Field checks and deterministic local search |
| PerceptionManager, PromptInjectionManager | Presentation selection and evidence-based quarantine |
| ShiftManager | Briefing → Processing → Receipt → next Processing / Evaluation |
| EvaluationManager, DecisionRecord | Counts, consequences and immutable history snapshots |
| SaveManager | Versioned JSON, atomic replacement, safe read failure |
| GameManager | Runtime initialization and generated audio |
| UIController partials, Checkpoint.uss | Runtime UI Toolkit presentation |
| CheckpointIllustration | Original vector portrait/airport art |
| CheckpointBootstrap | Editor asset creation and Windows build |
| CheckpointTests, RuntimeProbe | Domain tests and real rendered UI event checks |

The UI submits commands to the state machine. Perception never rewrites canonical identity or chooses a disposition. Decision records copy discovered evidence so subsequent cases cannot change history.

Save location: **Application.persistentDataPath/checkpoint-shift-v1.json**. In the Windows build this is normally **%USERPROFILE%/AppData/LocalLow/Merrow Studio/Not Just Accept, But Deny/**. Browser query text, window positions and an unsubmitted rationale are transient. Gameplay state and submitted reasons persist.

## Authoring

See **Docs/AUTHORING.md** for the schema, prerequisite rules and an additional-case workflow. No scene or C# change is required for a normal additional traveler.

## Build and tests

With this project closed in other Editor instances:

- Editor: **Window → General → Test Runner → EditMode → Run All**.
- Build: **Checkpoint → 3. Build Windows prototype**.
- PowerShell: **.\Tools\Verify.ps1** runs tests, builds Windows and exercises the rendered player at two resolutions. Supply an explicit Editor path with **-UnityPath**.

Results go to **Artifacts/QA/editmode-results.xml**. Runtime smoke runs write **probe-results.json** and PNG screenshots beneath **Artifacts/QA/**.

If the main project is open, verification can use a separate copy of **Assets, Packages and ProjectSettings**. Never run two Editors against the same project directory.

See **Docs/VERIFICATION.md** for actual session results and qualifications.

## Limitations

- One shift, five travelers; approximately 10–20 minutes when investigating and reading. Duration is a design target, not measured with external playtesters.
- Stylized procedural portraits, text dialogue and simple generated sounds. No voice acting, authored music, full character animation or localization.
- Deterministic token search over authored local records, not general web search or natural-language reasoning.
- Evidence and consequences are authored; this does not simulate a complete legal or asylum system.
- The AI revelation occurs at the fourth traveler within this introductory shift.
- Keyboard focus, dock controls and larger reading text are included; a full screen-reader audit and arbitrary aspect-ratio support are not claimed. The target is 16:9 desktops.
- Saves have structural validation but no migration across edited case IDs or future schema versions.
- No campaign, real exploit, network request, LLM integration, AI escape, economy, elaborate morality system or model-weight mechanic.

## Technical references

The UI uses Unity's [panel scaling](https://docs.unity3d.com/6000.0/Documentation/Manual/UIE-Runtime-Panel-Settings.html) and [Painter2D API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/UIElements.Painter2D.html). Verification uses Unity's [command-line test runner](https://docs.unity.com/en-us/engine/6000.5/manual/scripting/test-framework-introduction/running-tests/run-tests-from-command-line).
