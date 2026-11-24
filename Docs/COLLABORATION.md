# Collaboration notes

## First pull

- Use Unity **6000.5.1f1** and install Git LFS.
- Clone into a short local path such as `C:\Projects\checkpoint`; long paths can break imports of Unity package files on Windows.
- Pull LFS objects before opening Unity. Pink textures, unreadable audio, or tiny text files in place of binary assets can indicate missing LFS downloads.
- Start at `Assets/Scenes/Intro.unity`; work on `GameIntegration.unity` for the checkpoint.
- Configure your own local Groq key through Tools > Checkpoint > Passenger Chat Settings. Credentials are not shared.
- Allow import and compilation to finish before judging scene errors.

## Ownership and merging

Use feature branches. Assign one person at a time to a scene/prefab. Prefer separate character cards for writing and inventory edits. Before merging, save scenes and inspect the diff for unintended serialized changes. Never delete `.meta` files to resolve a conflict: these carry the identity used by scenes and assets. Unity assets are serialized as text and visible meta files are enabled.

Images, audio, and other binaries are managed by Git LFS. Uppercase `.PNG` files are included. The repository marks Unity YAML for `unityyamlmerge`; if you want Smart Merge, configure the matching Unity installation's UnityYAMLMerge tool locally. That tool installation/configuration is not portable in Git. Resolve a scene conflict with the scene owner when uncertain.

## Authoring

`CharacterCard` assets drive portraits, documents, the shuffled queue, dialogue, arrival sounds, and inventory facts. The passport may intentionally disagree with the registration data. Do not correct intentional discrepancies without checking the case design.

Inventory item category and baggage location drive the current tools rule. A reference such as MNT-204 is not yet a verified authorization. Keep occupation/purpose, inventory intended use, dialogue, and roleplay notes consistent. Test the interview after restarting Play mode so it receives updated card data.

`Staff Database.asset` supplies the local rulebook; it is not a web search or AI-generated answer. Editing prose does not change `CharacterCard.FindProblems()`. Update both when changing rules.

The original five-case UI Toolkit prototype and its verification reports are archived under `Assets/Checkpoint` and `Docs/LEGACY_PROTOTYPE.md`. Its checks do not establish that the current game works.

## Before sharing a change

1. Save scenes/assets outside Play mode.
2. Run the relevant Tools > Checkpoint validation commands.
3. Exercise Intro -> GameIntegration, one accepted visit, one denied visit, and automatic next-passenger reset.
4. Check `git status` and `git diff --check`. Include required untracked `.cs`, `.asset`, `.prefab`, `.unity`, media, and corresponding `.meta` files.
5. Ensure no keys, logs, generated caches, or builds are staged.
6. Commit, push your branch, and have another collaborator try it from their checkout.

`Tools/VerifyCurrent.ps1` runs an isolated import plus offline gameplay/chat checks. It needs the matching Unity editor, a usable Unity license, and package restoration access or a warm package cache. No Groq key is required for these checks. Temporary verification projects are retained under the operating system's temp directory for troubleshooting.
