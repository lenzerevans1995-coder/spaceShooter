# Space Combat Kit — Harvested Art Reference

SCK was harvested **art-only** (scripts/asmdefs removed — they broke tooling and conflict with our
architecture; see SYSTEM_STATE.md "Space Combat Kit" decision). All art lives under
`Assets/AssetPacks/SpaceCombatKit/` (gitignored). Prefabs have harmless missing-script components.

## ⚠️ Pipeline note
SCK particle **materials** use Built-in "Standard Particle" shaders → **render pink in URP**. Use the
**textures** (`.png`) directly on our own URP-Unlit/transparent materials instead. There is a
`SpaceCombatKit/SpaceCombatKit/SRP/SRP_Shaders_ClickMe.unitypackage` that may hold URP shader variants —
evaluate if we want SCK's materials as-is.

## Directly usable (pipeline-agnostic) — textures, sprites, FBX, audio

### Projectile / glow sprites (for bullets, muzzle, impacts)
- `VehicleCombatKits/MeshesTextures/Projectile_Energy/Projectile_Energy.png`  ← primary energy bolt
- `VehicleCombatKits/MeshesTextures/Projectile_Energy/Spark.png`
- `VehicleCombatKits/MeshesTextures/MuzzleFlash/MuzzleFlash.png`, `MuzzleSpit.png`, `Spiky Glow.png`
- `VehicleCombatKits/MeshesTextures/Explosions/ExplosionFlash.png`, `ExplosionAnimated.png` (spritesheet), `ExplosionShockwave.png`
- `VehicleCombatKits/MeshesTextures/Effects/Flare_Exhaust.png`, `Lightning_Curly.png`

### HUD / targeting
- `SpaceCombatKit/Prefabs/HUD/HUDTargetBoxes/SCK_TargetBox_Ship.prefab` (+ Waypoint, Subsystem, TargetIcon)
- `VehicleCombatKits/MeshesTextures/HUD/HUDRadar/Icons/` — HUDRadarIcon_Circle/Triangle/Spaceship.png
- `SpaceCombatKit/MeshesTextures/Ships/CapitalShip/HUD Health/` — Hull/Shield/Outline health bar art

### Mobile UI / touch controls (great for our VirtualJoystick + buttons)
- `SpaceCombatKit/MeshesTextures/Mobile/Textures/TouchJoystickMobile.png` (handle)
- `SpaceCombatKit/MeshesTextures/Mobile/Textures/TouchJoystickBackground.png` (base ring)
- `.../FireButtonMobile.png`, `SecondaryFireButtonMobile.png`, `Boost.png`, `PauseButton.png`
- `SpaceCombatKit/Prefabs/Mobile/MobileControls.prefab` (full canvas; strip missing scripts)

### Ship models (FBX, modular)
- `SpaceCombatKit/MeshesTextures/Ships/SpaceFighter/*.fbx` (9 parts: fuselage/wing/engine/shield)
- `SpaceCombatKit/MeshesTextures/Ships/CapitalShip/*.fbx` (9 parts) — boss-scale hull
- `SpaceCombatKit/MeshesTextures/Ships/ShipWithInterior/Ship.fbx`
- `VehicleCombatKits/MeshesTextures/Orb/Orb.fbx`

### Audio (49 wav)
`VehicleCombatKits/Audio/{Weapons,Explosions,Engines,Impacts,Collisions,Ambiences,Menu,Radar,Stingers}/`

## Suggested integration order (future passes)
1. Bullets → `Projectile_Energy.png` on a URP-Unlit additive billboard (replace emissive sphere). +`MuzzleFlash.png` on fire.
2. Mobile UI → swap `VirtualJoystick` square Images for `TouchJoystickBackground/Mobile.png`; add fire/boost buttons.
3. HUD → target boxes + health/shield bars sprites into the Phase-6 ModularGameUIKit HUD.
4. Impacts → `ExplosionAnimated.png` spritesheet on bullet/ship death.
5. Audio → weapon/explosion SFX hooks.
