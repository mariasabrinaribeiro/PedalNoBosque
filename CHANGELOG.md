# Changelog

All notable changes to this project will be documented in this file.

## [1.8.0] - 2026-06-12

### Added
- Added new dynamic spawning systems in [EnemySpawner.cs](file:///Users/renato/Documents/CarrinhoSorvete_Aula1/Assets/EnemySpawner.cs) for enemies, power-ups, and speed delivery gates.
- Added boss fight against the **Barão Amargo** in [BossBitterBaron.cs](file:///Users/renato/Documents/CarrinhoSorvete_Aula1/Assets/BossBitterBaron.cs) featuring throwing Sugar Bombs (slowing zone puddles) and summoning helper minions.
- Added dynamic missions in [MissionManager.cs](file:///Users/renato/Documents/CarrinhoSorvete_Aula1/Assets/MissionManager.cs) (Deliver ice creams, defeat enemies without taking damage, maintain speed) that award score and activate Girl Power.
- Added three collectible power-ups in [PowerUpItem.cs](file:///Users/renato/Documents/CarrinhoSorvete_Aula1/Assets/PowerUpItem.cs): Magnet (attract items), Shield (blocks next damage), and Triple Shot (shoots 3 projectiles at once).
- Added procedural sound synthesis in [EngineSound.cs](file:///Users/renato/Documents/CarrinhoSorvete_Aula1/Assets/EngineSound.cs) for the vehicle engine pitch, adapting to driving speed.
- Added ambient sugar dust glitter particles in [AmbientGlitterManager.cs](file:///Users/renato/Documents/CarrinhoSorvete_Aula1/Assets/AmbientGlitterManager.cs).
- Added camera follow smoothing and screen shake effects in [GameCameraController.cs](file:///Users/renato/Documents/CarrinhoSorvete_Aula1/Assets/GameCameraController.cs) triggered on collision or explosions.
- Added custom game icons, portrait splash screens, and landscape splash screen assets.
- Added comprehensive game documentation in [doc/README.md](file:///Users/renato/Documents/CarrinhoSorvete_Aula1/doc/README.md).

### Fixed
- Fixed and optimized mobile touch drag controls in [driver.cs](file:///Users/renato/Documents/CarrinhoSorvete_Aula1/Assets/driver.cs) using frame-rate independent, absolute-position relative drag to prevent drift and stuttering.
- Fixed `NullReferenceException` in debug panels by validating UI button bindings.
- Organized all visual assets into structured artwork subfolders.

## [1.0.0] - 2026-06-08

### Added
- Added driver controller script (`driverpro`) in [driver.cs](file:///Users/renato/Documents/CarrinhoSorvete_Aula1/Assets/driver.cs) with controls for moving forward/backward and rotating using Keyboard input (W, A, S, D), along with a speed boost item mechanic.
- Added delivery script (`Delivery`) in [delivery.cs](file:///Users/renato/Documents/CarrinhoSorvete_Aula1/Assets/delivery.cs) implementing package collection and customer delivery logic with feedback systems.
- Imported delivery game asset packages and fonts (`GameDev.tv - Delivery Dash Assets`, `Pixel Adventure 1`, TextMesh Pro, etc.).
- Setup default Unity project configuration and folders (Scenes, Rua, etc.).
