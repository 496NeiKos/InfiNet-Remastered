# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**InfiNet Remastered** is a Unity-based educational simulation game for teaching PC hardware assembly and troubleshooting (NC II Computer Systems Servicing). Players interactively assemble PC components, connect cables, and validate their work through a task system.

- **Engine:** Unity 6000.3.10f1
- **Language:** C# (LangVersion 9.0, .NET Standard 2.1)
- **Renderer:** Universal Render Pipeline (URP 17.3.0)
- **Platform:** Windows Standalone 64-bit

## Running the Project

Open the project folder in **Unity Editor 6000.3.10f1**. Press **Play** in the editor to run. There is no CLI build or test command — all development happens through the Unity Editor.

To build a standalone: `File → Build Settings → Build`.

C# code is compiled by Unity automatically. For IDE support, open `InfiNet-Remastered.sln` in Visual Studio 2022 or Rider.

## Scene Flow

Scenes are loaded in this order (configured in `ProjectSettings/EditorBuildSettings.asset`):

1. **LoadingScreen** → **MainMenu** → **LessonSelection**
2. Lesson content: `LessonBook`, `Introduction`, `HandTools`, `OSInstallation`, `PatchPanel`
3. User manuals: `umHardware`, `umNetworking`, `umSoftware`
4. Simulations: **Hardware.unity**, **Software.unity**, **Networking.unity** (COC I)

## Architecture

All scripts live under `Assets/Asset/Scripts/`. Key subdirectories:

- `NC II/` — hardware component controllers
- `Function/` — managers and global systems
- `HardwareState/` — save/load state persistence

### Core Systems

**Singletons (persist across scenes):**
- `GameManager` — global game state; controls which editing panels are open and coordinates workspace ↔ storage interaction
- `SceneController` — handles all scene loading/transitions
- `SoundManager` — audio playback; persists volume via `PlayerPrefs`
- `Bootstrap` — initializes audio on startup before any scene loads

**Hardware Interaction Pipeline:**
1. `HardwareHolder` manages which components are available in the storage UI
2. `DragPrefab` handles dragging components from storage into the workspace
3. Right-clicking a placed component triggers `PrefabInteraction`, which opens a detail editor panel
4. Detail editors (`DetailViewManager`, `MotherboardDetailViewManager`) let the user connect cables, tighten screws, and seat chips
5. `HardwareStateManager` / `HardwareStateData` serialize the current hardware configuration so state survives scene transitions

**Hardware Controllers all implement `IHardwareController`:**
- `SystemUnitController` — PC case; manages cover removal and front/side/back views
- `MotherboardController` — tracks installation phase (install vs. removal)
- `CPUController` / `CPUSlotController` / `CPULockController` — CPU seating and lock lever
- `HeatsinkController` — thermal component
- `MonitorController`, `AVRController` — peripherals
- Cable system: `BackCable`, `MBCable`, `MBCableDragManager`

**Hardware state** is saved via `IHardwareState` (interface) and managed by `HardwareStateManager`.

**Validation & Task Tracking:**
- `HardwareValidator` — checks that components are placed correctly
- `TaskListManager` — displays lesson objectives and tracks completion
- `SystemUnitConditionChecker` / `PowerOnConditionChecker` — validate readiness before power-on

**UI / Tooltips:**
- `HoverLabelManager` + `HoverRaycast` — context-sensitive hover labels over components
- `TroubleshootManager` — troubleshooting guide panels
- `ManualManager` — in-game reference manuals
- `LessonSelectionController`, `MainMenuController` — menu navigation

### Key Patterns

- **Singleton:** `GameManager`, `SceneController`, `SoundManager` all use a static `Instance` pattern.
- **Interface-driven hardware:** Add a new hardware type by implementing `IHardwareController` and `IHardwareState`.
- **Phase-based state:** Hardware controllers track discrete phases (e.g., `MotherboardPhase.Install`, `MotherboardPhase.Remove`) to gate interactions.
- **Right-click → editor panel:** `PrefabInteraction.cs` is the universal entry point for opening any component's detail view.
