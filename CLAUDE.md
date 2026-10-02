# WhiteDragon: project rules

First-person dark-fantasy roguelite, Unity 6000.6.3f1 (URP, new Input System). Design spirit: The Binding of Isaac (few simple stats, many items that combine, room-by-room runs, meta-progression that widens what can appear). Tone: gritty and dark, toned down so assets are cheap. All visuals are placeholders. NOTHING IS FINAL.

## Most important goal

Content must be EASY to add, change, and remove in the Unity Editor, by a human or an AI, without editing existing code. Items, synergies, status effects, pools = assets or strings. Shot effects = one small class plus an asset. Keep things SIMPLE. Do not over-engineer or add features that were not asked for. If something is ambiguous, pick the simplest option and say so in one line.

## Where things live

- All code and data: Assets/WhiteDragon, namespace WhiteDragon.
- Three assemblies only: WhiteDragon.Runtime, WhiteDragon.Editor, WhiteDragon.Tests. Subfolders do not get their own asmdef.
- Content assets (ScriptableObjects) in Assets/WhiteDragon/Data/Resources/{Items,Synergies,Effects,Statuses,Enemies,Characters}.
- Recipes for adding content: Assets/WhiteDragon/Docs/HOW_TO_EXTEND.txt. Read it before adding content, and keep it up to date.
- Scene: Assets/WhiteDragon/Scenes/Sandbox.unity.
- The old prototype was removed from the project and is kept at git tag prototype-v1. If an Assets/_Project folder or an Assets/Editor folder exists, ignore it and never edit it.

## Code rules

- One main type per file. File name equals class name.
- Enums (StatType, DamageType, ItemRarity, RandomStream) are append-only. Never reorder or renumber them.
- Never rename, remove, or reorder serialized fields on existing assets. Add new fields at the end.
- All gameplay randomness goes through RunSession.Rng with a RandomStream. UnityEngine.Random is for cosmetic effects only.
- Static state resets in a [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] method, including event subscriptions.
- Projectile contains NO effect-specific code. New shot effects extend ShotEffect and are plain assets.
- Status effects are data (StatusEffectDefinition assets). Do not hard-code individual statuses.
- Pools are lowercase string IDs on items. Never add a pool enum.
- Do not use the scripting symbols DEVELOPMENT_BUILD or UNITY_64 (deprecated in Unity 6.6, removed in 6.8). Use the symbol the compiler warning names, or a runtime check such as Debug.isDebugBuild.
- Use Unity 6 APIs: FindAnyObjectByType, FindObjectsByType with a sort mode. Avoid deprecated calls.
- Keyboard and mouse only. Input actions are created in code. HUD and debug panel use OnGUI. Do not use UI Toolkit or UI Builder.
- Editor-only authoring tools go in Assets/WhiteDragon/Editor and must never change runtime behavior.
- Summary() methods are for editor and debug tools only. Never show them in player-facing UI, since they can mention unlock requirements.

## Progression rules (hidden progression)

- Unlocks widen what CAN appear in future runs. They never raise baseline stats.
- Progression is hidden from the player: no locked-item lists, silhouettes, counters, or unlock text in player-facing UI. Debug UI may show everything and is separate.
- First run (planned): identical layout, rooms, enemies, and bosses for every player, pinned by data; items stay random; it repeats until its goal (defeating the first boss) is reached. Story overrides replacing a whole run are rare. Floor seeds are authored layouts with randomly rolled slots. Story-critical content is pinned by data, never by a magic run-seed number.

## How to work

- Do ONE phase or task at a time. Do not start the next until I say "continue".
- Run the tests after every change. Never weaken, delete, or skip a test to make it pass. A new test for a bug fix must be shown to fail without the fix.
- Keep the Console free of errors and warnings you caused.
- Do not delete files without asking first. Do not leave temporary helper files. Do not add packages. Never install com.unity.pipeline.
- Do not edit files outside the task's stated scope. If the task needs more, stop and ask.
- Report in at most 12 lines: files created, files changed, tests passed/failed/skipped, anything surprising. Do not narrate.
- Commit after each completed phase with a clear message. Never force-push.
- Never put tokens, passwords, or keys in any file.

## Performance targets (locked)

- Target 60 fps (16.7 ms per frame). 30 fps is the minimum the game must stay correct and playable at.
- On the dev machine in a development build, the heaviest scene (600 live rocks, 20 active enemies, homing and burn active): average frame time under 8 ms, worst frame under 16.7 ms, game scripts under 3 ms, about zero garbage per frame.
- Gameplay must be frame-rate independent: results must not change between 30 and 60 fps (fire rate, burn damage, status duration, invincibility, homing, enemy speed). Carry leftover time forward for timers.
- Avoid per-frame allocations in anything that runs every frame or per projectile. Pool frequently spawned objects.

## Art direction (locked)
- Gritty dark fantasy, Berserk-inspired, low poly. Flat-shaded models, tiny point-filtered textures, a near-monochrome desaturated palette with blood red as the only strong accent, heavy fog, vignette, dark outlines; hatching or posterize on shadows later. Zero-cost art for now: free CC0 or clearly licensed assets, listed in LICENSES.txt. Third-party packs stay out of Git. Every art swap goes through the optional visual fields (prefabs, icons, sounds); never hard-code art.
