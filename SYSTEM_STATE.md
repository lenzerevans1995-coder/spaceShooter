# SYSTEM_STATE — Space Shooter (Bullet Hell)

> Living state file. Updated at the end of every phase. Source of truth for what
> exists, what's decided, and what's pending. Read this first on any new session.

---

## 0. ENVIRONMENT (verified)

| Item            | Value                                            |
|-----------------|--------------------------------------------------|
| Unity version   | 6000.4.10f1 (Unity 6.4)                           |
| Render pipeline | URP 17.4.0                                        |
| Input           | Input System 1.19.0 (new) — NOT legacy            |
| Shader Graph    | 17.4.0 installed                                  |
| VFX Graph       | NOT installed                                     |
| AI Navigation   | 2.0.12 installed                                  |
| MCP             | UnityMCP per-project @ http://127.0.0.1:8080/mcp  |

---

## 1. ASSET LEDGER (verified on disk)

### Spaceships — Assets/Project/Prefabs/SpaceShips (23 prefabs, Synty SciFiSpace)
- Bomber ×4 — `SM_Ship_Bomber_01..04`
- Cruiser ×3 — `SM_Ship_Cruiser_01..03`
- Fighter ×5 — `SM_Ship_Fighter_01..05`
- Fighter Heavy ×4 — `SM_Ship_Fighter_Heavy_01..04`
- Stealth ×5 — `SM_Ship_Stealth_01..05`
- Colossal ×1 — `SM_Ship_Colossal_01`  (boss candidate)
- Transport ×1 — `SM_Ship_Transport_01` (carrier/objective candidate)

### Skill tree — Assets/AssetPacks/SimpleTalentTreeUi
- Runtime: `Talent`, `TalentTree`, `TalentManager` (singleton, ISaveable)
- Authoring: `TalentTreeSO`, `TalentTreeNodeSO`, `TalentTreeConnectionSO` (ScriptableObjects)
- Point pools enum: `TalentPointType { Global, Dark, Fire }`
- Events: OnTalentApplied, OnTalentPointsChanged, OnPointPoolChanged
- Layouts: circular + square-grid prefabs; Tooltip prefab

### UI — Assets/ModularGameUIKit
- 4-tier prefab library (Foundations / Components / Layouts / Popups)
- 18 demo screen templates incl. Game, Skills, Stage-Selection, Shop, Settings
- Key scripts: ModularPopup, TabMenu, CircularProgressBar, SceneTransition, ColorSwapper

### Styling — Assets/AssetPacks/FlatKit
- URP package present as `.unitypackage` (96 MB) — **NOT yet imported/extracted**
- ColorSwapper + flat shaders usable for ship team-color differentiation

### Synty — Assets/AssetPacks/Synty
- PolygonSciFiSpace (full: environment, FX, props), PolygonGeneric, SyntyPackageHelper

---

## 2. DECISIONS LOG

| # | Decision | Status |
|---|----------|--------|
| D1 | Render pipeline = URP | LOCKED (project default) |
| D2 | Input = new Input System | LOCKED |
| D3 | Bullet logic = custom batched code movers (BulletManager); pack art is visual-only. Primary art = **Master Stylized Projectile v1.0** (Shuriken, 43 MB) | LOCKED (user-approved) |
| D4 | Skill tree = SimpleTalentTreeUi as-is, mapped to 3 pools | PROPOSED |
| D5 | UI = ModularGameUIKit, single persistent UI scene + additive gameplay | PROPOSED |
| D6 | Styling = **both** FlatKit + PotaToon (PotaToon imported, compiles clean on URP 17.4) | LOCKED (user-approved) |
| D7 | Gameplay code in default **Assembly-CSharp** (no asmdef) — packs lack asmdefs and an asmdef can't reference Assembly-CSharp | LOCKED |

---

## 3. OPEN BLOCKERS / NEEDS FROM USER

- ~~B1 — PotaToon missing.~~ RESOLVED — found at `…\Shaders & Rendering\PotaToon v1.4.2.unitypackage`,
  imported to `Assets/PotaToon`, compiles clean.
- **B2 — Projectile pack import (deferred to Phase 8/art pass).** `Master Stylized Projectile v1.0.unitypackage`
  is on the OneDrive drive; not needed until we swap real art onto bullets. I'll instruct import then.
- **B3 — FlatKit URP** package present in-project but still packed (`[Render Pipeline] Universal (URP).unitypackage`).
  Needed before FlatKit shaders/ColorSwapper can tint ships (Phase 5/8).
- Note: a **newer Synty SciFiSpace v1.14** and **Flat Kit v4.9.9 (full)** also exist on the drive if we want upgrades.
- ~~B4 — PotaToon needs its Renderer Feature.~~ RESOLVED. PotaToonFeature added to PC_Renderer + Mobile_Renderer.
  Working approach: `CombatBootstrap.ApplyPotaToon` CLONES a real PotaToon material (`potaToonReference` = body.mat)
  so shader keywords carry over (a bare `new Material(PotaToon/Toon)` renders black). Albedo transferred from Synty
  `_Albedo_Map`/`_Texture_Map` → `_MainTex`; outline 0.15. `usePotaToon=true`. All hulls cel-shade correctly.
- **B5 — Space Combat Kit: DO NOT import wholesale.** Package bundles a full `ProjectSettings/` overwrite
  (Graphics/Quality/Input/Tags/Physics/ProjectSettings) + `Packages/manifest.json` → would clobber our URP +
  new-Input-System config. Harvest VFX via a scratch project instead; never let it touch ProjectSettings/manifest.

---

## 4. PHASE TRACKER

| Phase | Title | Status |
|-------|-------|--------|
| 1 | Architecture & GDD | APPROVED |
| 2 | Core loop + object pooler (foundation code) | **COMPLETE — awaiting review** |
| 3 | Bullet-hell pattern engine | **COMPLETE — awaiting review** |
| 4 | Player ship + combat loop (vertical slice) | **COMPLETE — awaiting review** |
| 5 | Skill tree integration | not started |
| 6 | UI/HUD wiring | not started |
| 7 | Planets/rounds/boss content | not started |
| 8 | VFX/polish/audio | not started |

---

## 5. CODE MAP (Phase 2)
All under `Assets/Project/Scripts/` (namespace `SpaceShooter.*`):
- `Core/IGameState.cs`, `Core/GameDirector.cs` (singleton, DontDestroyOnLoad), `Core/GameStates.cs` (Boot→GalaxyMap→Combat stubs)
- `Combat/Faction.cs` (Player/Enemy/Neutral), `Combat/ITarget.cs` (radius-collision target interface)
- `BulletHell/Bullet.cs` (no Update; ticked centrally), `BulletHell/BulletPool.cs` (prefab-keyed pool), `BulletHell/BulletManager.cs` (batched Update + cull + collision)
- `Emitters/EmitterPatternSO.cs` (abstract), `Emitters/RingEmitter.cs` (CreateAssetMenu), `Emitters/EmitterController.cs` (timer driver)
- `Test/BulletHellStressTest.cs` + scene `Scenes/BulletHellStressTest.unity`

## 5b. CODE MAP (Phase 3 — attack engine)
- Unified base: `Emitters/AttackPatternSO.cs` (abstract, `Emit(in AttackContext)`), `Emitters/AttackContext.cs` (carries both managers), `Emitters/ProjectilePatternSO.cs` (shared bullet stats + Fire helper)
- Projectile patterns: `RingEmitter`, `SpiralEmitter`, `AimedShot` (line), `FanEmitter` (arc), `WallEmitter` (curtain/vertical spread, optional gap), `ScatterEmitter` (random)
- Per-bullet behaviors: `BulletHell/BulletSpawnParams.cs` (`BulletBehavior` Straight/Homing/SineWeave); steering in `Bullet.Tick`
- Beams: `BulletHell/Beam.cs` (LineRenderer, telegraph→active→fade, sweep), `BulletHell/BeamManager.cs` (pool + point-to-segment collision), `Emitters/BeamPatternSO.cs`
- Shared `Combat/TargetRegistry.cs` (static, queried by both managers; reset on play)
- `Emitters/EmitterController.cs` → attack timeline sequencer (`AttackStep` list, loop, coroutine; `Configure`/`SetTimeline` APIs)
- Test: `Test/TestDummyTarget.cs` (ITarget), `Test/PatternShowcase.cs` + scene `Scenes/PatternShowcase.unity`

## 5c. CODE MAP (Phase 4 — playable slice)
- `Combat/CombatField.cs` (circular arena bounds/clamp), `Combat/ShipActor.cs` (ITarget base: health/death/registration)
- `CameraSystem/CameraRig.cs` (perspective tilted follow cam, landscape)
- `Inputs/ShipInput.cs` (twin-stick: gamepad/keyboard/mouse + joysticks + autoDemo), `Inputs/VirtualJoystick.cs` (touch UI)
- `Ships/ShipWeapon.cs` (fires AttackPatternSO, faction-aware), `Ships/PlayerShip.cs` (free move + clamp + fire), `Ships/EnemyShip.cs` (approach/strafe AI + EmitterController)
- `Flow/RoundController.cs` (win/lose tracking), `Flow/CombatBootstrap.cs` (assembles slice; mesh-measured inscribed hitboxes; mixed small/large wave; HUD + restart)
- Scene `Scenes/CombatSlice.unity`. Hulls: player Fighter_01; small Fighter_03/Stealth_01; large Bomber_01/Cruiser_01.
- DESIGN: free-roam circular arena (field > screen), twin-stick, landscape, mobile-first. Hitbox = inscribed circle of hull mesh.

## 6. CHANGELOG
- 2026-06-12 — Phase 1 drafted; environment + asset ledger verified via project scan.
- 2026-06-12 — Phase 1 APPROVED. PotaToon located + imported (clean). Phase 2 foundation written
  (12 scripts, no asmdef). Stress test: **~3,000 bullets @ 154 FPS** (target was 1,000 @ 60) — PASS.
- 2026-06-12 — Phase 2 APPROVED. Phase 3 attack engine built: 6 projectile patterns + Homing/SineWeave
  behaviors + telegraph/sweep beam subsystem + shared TargetRegistry + EmitterController timeline.
  Verified in PatternShowcase: spiral & scatter & beam render correctly, collision applies damage
  (target 100000→99963 HP), 200+ bullets @ 268 FPS. Compiles clean. Screenshots in Assets/Screenshots.
- 2026-06-12 — Phase 3 APPROVED (twin-stick, landscape, follow cam). Phase 4 vertical slice built:
  free-roam circular arena, player ship (twin-stick + autoDemo), follow cam, mixed small/large enemy
  wave w/ AI + emitter patterns, round win/lose. Verified end-to-end: round WON 0/6, player survived
  45 HP, ~60 bullets, 290 FPS. User feedback applied: reverted to small ship scale, hitbox = inscribed
  mesh circle, lowered ground, enlarged field, small+large hull variety. See phase4_slice_E.png.
