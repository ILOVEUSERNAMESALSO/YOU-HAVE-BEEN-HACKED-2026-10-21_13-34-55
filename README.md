# Not Just Accept, But Deny

A Unity airport-checkpoint game about an AI inspecting passengers, questioning them, and consulting a fictional rulebook.

## Start here

Use **Unity 6000.5.1f1**, exactly as recorded in `ProjectSettings/ProjectVersion.txt`. Install **Git LFS** before cloning. Use a short local path (for example `C:\Projects\checkpoint`) to avoid Windows path-length problems inside Unity's package cache.

```sh
git lfs install
git clone https://github.com/ILOVEUSERNAMESALSO/YOU-HAVE-BEEN-HACKED-2026-10-21_13-34-55.git checkpoint
cd checkpoint
git lfs pull
```

1. In Unity Hub, add the cloned project folder (the folder containing Assets, Packages, and ProjectSettings).
2. Open with **6000.5.1f1** and wait for package restoration and asset import.
3. Open **Assets/Scenes/Intro.unity** and press **Play**.
4. Continue through the intro; **BEGIN SHIFT** loads **GameIntegration**.
5. To work directly on the checkpoint, open **Assets/Scenes/GameIntegration.unity** instead.

The checked build scenes are **Intro**, then **GameIntegration**. Use **File > Build Profiles** for the current game. The old top-level **Checkpoint** menu and `Tools/Verify.ps1` belong to the legacy prototype, not this version.

## Optional live passenger chat

Document dragging, decisions, arrival audio, and the local searchable rulebook do not require an API key. The interview shows an authored greeting; free-form questions require a personal **Groq** key and internet access.

In Unity, choose **Tools > Checkpoint > Passenger Chat Settings**, enter your key, and select **Save locally**. It is stored in local Editor preferences and is not part of Git. Alternatively, set `GROQ_API_KEY` before launching Unity; it takes precedence. The project does not load `.env` files automatically. Never put a key in a scene, prefab, character card, script, or commit.

The client uses Groq's chat endpoint; the model is configured on **AIManager** (currently `openai/gpt-oss-20b`). Sending a question transmits fictional passenger context and the conversation to Groq. Missing credentials and connection failures leave the document-inspection loop usable. A public release needs a server-side service; the direct-key client is for local development.

## Current controls

- Drag the passport and boarding pass between the handover and inspection areas.
- The passport opens in inspection and closes in handover. Invalid drops return to their starting position.
- Ask the passenger questions in the interview panel with **SEND** or Enter.
- Open the staff computer/rulebook, search by keyword, and select an article. Escape closes the terminal.
- Stamp **Accept** or **Deny** while the passport is on the inspection desk.
- Return the stamped passport to handover to finish the visit. The next passenger arrives automatically; the prior result remains visible.

## Where to work

| Location | Purpose |
|---|---|
| `Assets/Scenes/Intro.unity` | Current opening sequence |
| `Assets/Scenes/GameIntegration.unity` | Current playable checkpoint with passenger chat |
| `Assets/Characters/*.asset` | Passenger identity, portrait, documents, inventory, dialogue, and arrival cue |
| `Assets/Computer/Staff Database.asset` | Searchable fictional articles and keyword aliases |
| `Assets/AIChatbot.cs` | Passenger context, requests, cancellation, and typing |
| `Assets/Decision.cs` / `CharacterCard.cs` | Visit flow and deterministic clearance checks |
| `Assets/VirtualComputer.cs` | Runtime-generated rulebook UI |
| `Assets/Music/` | Intro, gameplay, and arrival clips |
| `Assets/Scenes/Game.unity` | Earlier checkpoint scene; retained for reference |
| `Assets/Scenes/TheAndrewUnityFile.unity` | Imported reference scene; contains unrelated demo dependencies that are not all included. Do not use as the starting scene. |
| `Assets/Checkpoint/` | Separate legacy prototype; documentation in `Docs/LEGACY_PROTOTYPE.md` |

## Current work in progress

The evaluator checks name, DOB, expiry, and tools in cabin baggage. Inventory descriptions are supplied to the passenger chatbot. **Authorization references are descriptive only: searchable authorization records and approval exceptions are not implemented yet.** Empty inventory lists mean content has not been authored; fill each card before expecting detailed answers about belongings. Rulebook prose and code checks must be updated together.

The saved project may contain older UI labels or optional features still being developed. Consult the scene and scripts rather than the archived prototype documentation for current behavior.

## Team workflow

Save your Unity scenes before committing. Commit assets and their `.meta` files together; retain package manifests, the package lock file, and ProjectSettings. Never commit Library, Temp, Logs, Builds, local keys, or editor caches.

Before pulling, commit or stash your own changes and close Unity. Then run:

```sh
git pull --ff-only
git lfs pull
```

Reopen Unity and allow it to import. Coordinate before editing the same `.unity` scene or prefab; separate CharacterCard assets let writers work independently. Move or rename assets inside Unity so their GUIDs are preserved. See [collaboration notes](Docs/COLLABORATION.md).

## Checks

- With **GameIntegration** open, run **Tools > Checkpoint > Validate Passenger Chat Integration**.
- Run **Validate Character Cards** and **Validate Computer Database** from the same menu.
- In Play mode, **Test Passenger Chat Lifecycle** exercises automatic visits and simulated chat failures/cancellation without sending network requests. It advances the queue; stop and restart Play mode afterward.
- For an isolated fresh-import check, run `Tools/VerifyCurrent.ps1`. It copies source assets and project settings to a temporary folder and runs Unity there, leaving your open project alone. Results are written under ignored `Artifacts/Current/`.

Windows players require the matching Windows Build Support module. Generated builds are ignored; share the entire output folder if distributing a local test build.

## Windows Burst startup issue

If your managed Windows machine reports a Burst DLL load failure with error **4551**, close Unity and use **OpenUnityWithoutBurst.cmd** in the project root. It opens the matching editor with Burst compilation disabled for that session. It does not change Windows security settings. See the archived [troubleshooting details](Docs/LEGACY_PROTOTYPE.md#windows-burst-error-4551).
