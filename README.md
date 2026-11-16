# Meowbat: The Accidental Hero

A 2D top-down Action-RPG made in **Unity 6** and **C#**. It was my Bachelor's thesis project at Politehnica Bucureşti (Piteşti University Centre, Computer Science, 2026).

Meowbat travels between a peaceful hub and a combat arena. In the arena, waves of enemies get harder as you go. Between fights you spend coins with shopkeepers, upgrade your stats and manage your inventory. All pixel art, animation and UI were drawn by me in Aseprite.

## Features

- **Combat:** melee sword and ranged bow, dash with invulnerability frames and ghost trail, sprint with stamina, knockback, damage flash and floating damage numbers.
- **Enemy AI:** state-based enemies (roaming, chasing, attacking), including melee, ranged and a support **healer** that seeks out wounded allies and heals in an area.
- **Wave system:** staggered spawning, weighted random enemy selection and stat scaling by difficulty ([`WaveManager.cs`](Assets/Scripts/WaveManager.cs)).
- **Inventory:** hotbar plus backpack window, drag and drop, stacking, slot swapping and ammo consumption. Items are **ScriptableObjects** ([`ItemData.cs`](Assets/Scripts/ItemData.cs)), and the UI listens for changes through C# events (observer pattern).
- **Economy:** paged shop with weapon, armour and consumable categories; an upgrade shopkeeper with exponential cost scaling and free level-up rewards.
- **Save system:** JSON save/load via `JsonUtility` covering stats, upgrades, inventory, position and time of day ([`SaveManager.cs`](Assets/Scripts/SaveManager.cs)).
- **Single-scene architecture:** hub and arena live in one scene. Warp gates move the player and reconfigure the Cinemachine camera bounds, so there are no loading screens ([`WarpGate.cs`](Assets/Scripts/WarpGate.cs)).
- **Atmosphere:** day/night cycle driving the global 2D light through a gradient, with fireflies at night and butterflies by day.
- **Input:** Unity **New Input System** with action maps, plus UI shielding so clicks on menus never trigger attacks.

## Tech

| | |
|---|---|
| Engine | Unity 6 (6000.0.43f1), URP 2D lighting |
| Language | C# (about 5,100 lines across 40 scripts in [`Assets/Scripts`](Assets/Scripts)) |
| Camera | Cinemachine |
| Art | Aseprite (all sprites, animation and UI) |

Code comments are in Romanian, the language of the thesis.

## Opening the project

1. Install **Unity 6000.0.43f1** (or a newer Unity 6 version) through Unity Hub.
2. Clone this repo, then use **Add → Add project from disk** in Unity Hub and pick the folder.
3. Open `Assets/Scenes/SampleScene.unity` and press Play.

### Controls

| Action | Key |
|---|---|
| Move | W A S D |
| Aim / attack | Mouse / left click |
| Sprint | Left Shift |
| Dash | Left Ctrl |
| Interact (shops, gates) | E |
| Inventory | Tab |
| Pause | Esc |

---

Personal and academic project by Tudor Marian-Vlad.
