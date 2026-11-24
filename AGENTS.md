# AGENTS.md — TrafficJam (Unity)

> Repository-level instructions for Codex and other coding agents working on **TrafficJam**. Apply these rules to every task in this repository unless the user explicitly overrides a rule. The user's latest task always determines the actual scope of work.

## 1. Mission and communication

- Maintain and improve TrafficJam, a Unity mobile game primarily targeting **Android**. Verify behavior in the Unity Editor and, when available, on Android devices using Logcat; do not assume prior test coverage.
- Reply to the developer in **Vietnamese**. Keep code identifiers, Unity menu names, class names, log tags, and error messages in their original language. Explain the root cause, modified behavior, Inspector wiring, and how to verify changes.
- Focus on **working, minimal, safe changes**. Prefer adapting the actual existing implementation over rewriting systems from scratch.
- Do not guess class APIs, hierarchy paths, package versions, animation names, or game behavior. Inspect the repository before modifying it. If a file or dependency is unavailable, say what could not be verified.

## 2. Repository and branch safety (mandatory)

- Before making edits, run `git status --short --branch` and `git branch --show-current` (or equivalent read-only commands). Work on the currently checked-out branch unless the user explicitly selects another branch. There is no fixed required branch for this project.
- **Do not switch branches automatically.** If the user explicitly requests a branch different from the current checkout, stop before changes and ask how to proceed.
- Do not inspect or modify unrelated historical branches unless the user asks for a cross-branch comparison or cherry-pick.
- **Never** automatically run `git push`, force push, merge, rebase, reset --hard, clean, branch deletion, or cherry-pick. Obtain explicit approval for each such action.
- Do not automatically commit changes. Present a readable diff first; commit only if explicitly requested.
- Preserve user modifications. Check `git status` and `git diff` before changing files; never overwrite, discard, or silently format unrelated changes.
- Avoid modifying `.git/` contents, credentials, signing files, keystores, or release/distribution settings without explicit instruction.
- If Git metadata is unavailable (e.g., only a source file was supplied), clearly state that branch and dirty-tree status could not be confirmed.

## 3. Source-of-truth workflow

1. **Inspect** relevant code and its callers, event subscribers, prefabs, scene/asset references, and serialized fields before editing. Search first (`rg` where available), then read the relevant files.
2. **Summarize** the current execution path: trigger → gameplay action → event/callback → animation/UI → completion/reward. Identify what must remain unchanged.
3. **Implement** the smallest coherent change. Fix the actual cause rather than layering on extra timers, duplicate subscriptions, or speculative fallbacks.
4. **Validate** with the checks realistically available in the environment (syntax/compile if Unity or C# tools are present; otherwise static inspection and searches). Never claim Editor, Android, Logcat, or build tests ran unless they actually did.
5. **Report** changed files, important diffs, Inspector steps, test steps and results, and remaining risks/unverified assumptions. Supply complete replacement scripts when the developer requests full scripts.

If an issue involves multiple scripts, inspect and update **all required call sites** as one coherent change; avoid delivering a controller that relies on nonexistent methods in an item script.

## 4. Unity and serialization rules

- This is a **Unity 6 / 6000-series** project. Follow the versions and APIs actually present in `Packages/manifest.json`, package lock, `.asmdef` files, and source code; do not assume a newer API exists.
- Preserve public APIs, class/file names, namespaces, serialized field names and types, enum values, UnityEvents, animation string names, and Inspector compatibility wherever possible.
- Avoid renaming or deleting `[SerializeField]` fields: Unity scene/prefab references depend on them. If renaming is truly required, consider `UnityEngine.Serialization.FormerlySerializedAs` and explain migration.
- Preserve `.meta` files and GUIDs. Never delete/regenerate existing `.meta` files unnecessarily.
- Do not edit scenes, prefabs, ScriptableObjects, materials, or Project Settings unless the task genuinely requires it. If editing Unity YAML, make small targeted changes and verify that references remain consistent.
- Avoid modifying generated folders: `Library/`, `Temp/`, `Obj/`, `Build/`, `Builds/`, `.gradle/`, IDE caches, or package caches. Do not commit generated artifacts.
- Respect inactive GameObjects, `Awake`/`OnEnable`/`Start` ordering, `DontDestroyOnLoad`, canvas/camera coordinate spaces, and whether components are Unity UI (`RectTransform`, `SkeletonGraphic`) or world-space objects (`Transform`, `SpriteRenderer`, `SkeletonAnimation`).
- Do not add Editor-only APIs to runtime code; protect any Editor tooling with `#if UNITY_EDITOR` and place it in an Editor assembly/folder when needed.

## 5. C# code style and implementation quality

- Follow the style already used in the touched file (brace placement, naming, field grouping, and logging). Avoid large unrelated refactors or mechanical reformatting.
- Prefer serialized private fields for Inspector-configurable values and clear guard clauses for null/inactive/state conditions.
- Avoid unnecessary allocations, `Find*` calls, or LINQ inside `Update`/frequent drag callbacks. Cache references when possible.
- Subscribe/unsubscribe symmetrically (`OnEnable`/`OnDisable`, or equivalent). Guard against duplicate callbacks, repeated wins, duplicate reward credits, and re-entry.
- On disable/reset/destroy, stop coroutines, unregister callbacks, and kill owned DOTween animations safely. Do not accidentally kill tweens that belong to other objects/systems.
- If a tween must play while the game is paused, use DOTween `.SetUpdate(true)` only where intended; preserve existing scaled/unscaled timing behavior.
- Never assume a GameObject remains active after `SetActive(false)`; pay attention to scripts that invoke callbacks immediately before deactivation.
- Use informative debug messages with stable system prefixes (e.g., `[TrafficJam]`, `[SceneLoader]`, `[GameController]`). Avoid per-frame log spam.

## 6. Gameplay and UI invariants

- Use the actual TrafficJam source, scenes, prefabs, and level data as the source of truth. Inspect the affected gameplay flow before documenting or changing its rules.
- Preserve existing vehicle movement, parking, passenger handling, blockers, input locking, and win/lose conditions unless the user explicitly asks to change them. Verify which systems participate in the affected flow rather than assuming their behavior.
- Preserve level loading, restart/next-level behavior, save data, reward crediting, and UI completion callbacks. Guard against repeated input, duplicate callbacks, and duplicate rewards.
- Check scene names in code and serialized Inspector fields against the actual scene assets and active Build Profile/shared scene list. Preserve asset GUIDs when renaming scenes and update required references together.
- Inspect singleton initialization and `DontDestroyOnLoad` hierarchy before changing cross-scene managers. Avoid duplicate manager instances and stale references after scene changes.
- For tutorials and UI guides, preserve the existing input rules and coordinate spaces. Guide visuals must not block pointer input unless intentionally designed to do so.
- Follow animation and audio systems actually installed and used in this repository. Preserve callback timing and clean up owned animations/audio on disable or reset.

## 7. SDKs, networking, and Android release

- Inspect installed SDK versions, packages, dependencies XML, platform plugins, and enabled integration points before proposing SDK changes. Commented scripts or stale scene components do not prove that an integration is active.
- Preserve existing ad placement IDs, initialization order, fallback behavior, and event contracts unless the user requests changes.
- Prefer owned wrappers for integration fixes; avoid modifying third-party SDK source without explicit authorization.
- Do not introduce real monetization/network actions in Editor-only simulations. Explicitly label simulated ad behavior.
- Never log or expose SDK secrets, dev keys, tokens, keystore passwords, private signing files, or other credentials. Never replace production IDs with test IDs or vice versa without explicit instructions.
- Changes that could affect Gradle, Android manifest, dependencies XML, R8/ProGuard, keystore, Google Play configuration, or advertising SDK initialization must be highlighted as **Android-build risks**.

## 8. Git and asset-specific cautions

- For requested cross-branch changes, inspect the target commit, affected files, and scene/asset YAML changes before proposing an operation. Prefer a focused change over importing unrelated scene state.
- Inspect the actual level storage and numbering/index conventions before modifying level assets or tools. Preserve asset types, references, and level mappings; do not assume a directory or index formula from another project.
- When a user asks to identify changes in Unity scenes, compare YAML `GameObject`, `Transform`, `MonoBehaviour`, GUID references and prefab modifications; do not infer object changes from diff color alone.

## 9. Testing checklist (apply only what is relevant)

- **Editor compilation:** new classes/methods resolve, no duplicate symbols, namespaces and package APIs exist.
- **Inspector:** references are assigned; no missing scripts; serialized fields survived; prefabs/scenes stay intact.
- **Interaction:** relevant tap/touch or drag input, vehicle movement, parking/passenger handling, blocked actions, disabled objects, reset/replay.
- **Animations:** relevant movement/UI transitions, audio, cancellation/reset, and correct ordering of callbacks.
- **Completion:** win/lose triggers once, expected result UI appears in order, rewards/save data update once, restart and next-level loading work.
- **Mobile:** resolution/safe areas, Canvas scaling, camera conversion, touch input, Android Logcat errors, memory/performance.
- **Ads/SDKs** (if touched): Editor simulation vs device behavior, init order, real-device callbacks/load failure, test versus production IDs.

If Unity Editor or an Android device is unavailable, **do not say these tests passed**. Provide a precise manual checklist and identify the untested portions.

## 10. Response format for coding changes

Unless the user requests another format, respond in Vietnamese with:

1. **Nguyên nhân / mục tiêu** — one short explanation.
2. **Đã thay đổi** — exact file names and affected methods, including behavior preserved.
3. **Code / diff** — complete ready-to-paste code for short files or when expressly requested; otherwise a focused diff with exact insertion points.
4. **Inspector** — fields/GameObjects to assign, if any.
5. **Kiểm thử** — reproducible steps, expected log/event order, and which validations were actually performed.
6. **Rủi ro / chưa xác minh** — only material outstanding items.

Do not invent download links, pretend files were changed when they were not, or claim project-wide knowledge before inspecting the actual repository.

### Additional Mandatory Reporting Rules

- Before coding, inspect the existing call flow and reuse
  existing logic whenever possible.
- When moving or renaming Unity assets, preserve their
  original .meta files and GUIDs.
- For every new Inspector field, specify the component,
  target GameObject, reference, and recommended value.
- For gameplay and UI changes, specify the affected Scene
  and exact Unity Play Mode testing steps.
- For advertising SDK changes, explicitly identify what
  requires verification in an Android build and Logcat.
- At the end of every task, list all modified files and
  distinguish actual tests performed from tests not run.

## 11. Scope boundaries

- Do only what the current user asks; do not proactively replace working systems or add new packages.
- For highly invasive or irreversible actions (scene-wide rewrites, package upgrades, mass asset edits, delete/reset, changing publishing/signing/ads configurations), request explicit authorization before executing.
- If the task can be solved with a single C# file, avoid touching scenes or other scripts unless references require it. Conversely, if a coherent fix needs several scripts, do not knowingly leave the project in a half-integrated state.
- Follow existing source behavior over this document if the code has evolved; call out discrepancies and ask only when a decision is truly required.
