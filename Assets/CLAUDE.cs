/*
# Project Context & Rules: [FinalProject222]

---

# Project Overview

* Genre: Top-down Action RPG
* Engine: Unity
* Language: C#
* Architecture Goal: Modular gameplay systems with scalable architecture
* Current Development Phase: Core gameplay systems development

Claude must prioritize:

- maintainable architecture
- modular gameplay systems
- minimal coupling between systems

---

# Core Architecture Rules (Critical)

These rules define the architecture of the project. Claude must never violate them.

System-Based Architecture

The game is divided into gameplay systems:

* Player System
* Combat System
* Enemy AI System
* Quest System
* Inventory System
* UI System
* Save System
* Travel System  [ADDED]

Each system must have:

- clear responsibilities
- minimal dependencies
- reusable components

---

# Project Architecture Map

Claude must maintain a mental map of the project architecture.

Gameplay Layer

  Player
  Enemy
  NPC (NPCQuestDialog, NPCTraveler)

System Layer

  CombatSystem
  QuestSystem
  InventorySystem
  SaveSystem
  TravelManager  [ADDED - Infrastructure Layer, DontDestroyOnLoad]

Infrastructure Layer

  UIManager
  AudioManager
  GameManager

Claude must respect this layered structure.

Rules:
- Gameplay layer uses systems
- Systems may interact with managers
- Managers handle global state
- Gameplay scripts should not directly control unrelated systems

---

# System Dependency Graph

Claude must follow this dependency direction.

Allowed dependency flow:

  Player        -> CombatSystem
  Enemy         -> CombatSystem
  QuestSystem   -> UI
  InventorySystem -> UI
  SaveSystem    -> GameManager
  NPCTraveler   -> TravelMenuUI -> TravelManager -> SceneManager  [ADDED]
  TravelManager -> SaveSystem (saves quest data before scene load)  [ADDED]

Avoid:

  UI -> CombatSystem
  Enemy -> QuestSystem
  Player -> SaveSystem directly

If a dependency violates architecture, Claude must propose a better design.

---

# Script Responsibility Table

Claude must check this before creating scripts.

  Script Type  | Responsibility
  -------------|------------------------------
  Controller   | Handles gameplay logic
  Manager      | Handles global system state
  Data         | Stores configuration data
  Utility      | Reusable helper functions
  AI           | Enemy decision logic

Travel System Scripts [ADDED]:

  Script                   | Type         | Responsibility
  -------------------------|--------------|------------------------------------------
  TravelManager            | Manager      | Singleton. Handles scene loading, persists
                           |              | SpawnPointID via DontDestroyOnLoad,
                           |              | saves quest data before scene transition,
                           |              | teleports player to SpawnPoint after load.
  NPCTraveler              | Controller   | Attached to Peasant NPC. Handles trigger
                           |              | detection, prompt blink, key E input,
                           |              | opens/closes TravelMenuUI.
  TravelMenuUI             | UI           | Displays destination list as buttons.
                           |              | Implements ITravelMenu. Spawns buttons
                           |              | dynamically, handles close button.
  TravelDestinationData    | Data (SO)    | ScriptableObject. Stores destination name,
                           |              | build index, SpawnPoint ID, description,
                           |              | icon, availability flag.
  ITravelMenu              | Interface    | Contract between NPCTraveler and
                           |              | TravelMenuUI. Methods: Show(), Hide().

Rules:
- Controller -> gameplay entity behavior
- Manager -> global system control
- Data -> ScriptableObject or config classes

Claude must not mix responsibilities in one script.

---

# Codebase Structure

Assets/
  Scripts/
    Characters/
      Player/
      Enemy/
    Systems/
      Combat/
      Quest/
      Inventory/
    Managers/
    UI/
    Travel/              [ADDED]
      Data/
        TravelDestinationData.cs
      TravelManager.cs
      ITravelMenu.cs
    Quest/
      NPC/
        NPCQuestDialog.cs
        NPCTraveler.cs   [ADDED]
  ScriptableObjects/
    Travel/              [ADDED]
      Destination_WesternVillage.asset
      Destination_Desert.asset
      Destination_Necrom.asset
  Prefabs/
    DestinationButtonPrefab.prefab  [ADDED]
  Scenes/
    MainMenu.unity       (Build Index 1)
    Western Village.unity (Build Index 2) [TravelManager lives here]
    Desert.unity         (Build Index 3)
    Necrom.unity         (Build Index 4)

Rules:
- New scripts must be placed in the correct system folder
- Avoid placing gameplay logic in Managers
- Keep systems independent

---

# Travel System — Key Design Decisions [ADDED]

SpawnPoint Convention:
- SpawnPointID in TravelDestinationData must match the GameObject NAME in the target scene.
- TravelManager uses GameObject.Find(spawnPointID) to locate the spawn position.
- Each scene must have a correctly named SpawnPoint GameObject:
    Western Village -> SpawnPoint_WesternVillage
    Desert          -> SpawnPoint_Desert
    Necrom          -> SpawnPoint_Necrom

Quest Data Preservation:
- TravelManager.TravelTo() calls SaveQuestDataBeforeTravel() BEFORE LoadScene().
- This saves all quest states and objective progress to PlayerPrefs via SaveSystem.
- The new scene's QuestSystem.Start() restores data from PlayerPrefs on load.
- Without this step, quest progress would be lost on scene transition.

DontDestroyOnLoad:
- Only TravelManager uses DontDestroyOnLoad (Singleton).
- QuestManager also uses DontDestroyOnLoad.
- PlayerCore (Character, Camera, Managers) is duplicated in each scene — NOT shared.
- When LoadScene(Single) runs, the old scene is fully destroyed except DDOL objects.

---

# Project Analysis Rule (MANDATORY)

Before performing major tasks Claude must:

1. Analyze folder structure
2. Identify major systems
3. Understand script responsibilities
4. Detect system dependencies
5. Map prefab structure

Claude must build a project architecture understanding before proposing solutions.

---

# Script Creation Rules

Before creating a new script Claude must check:

1. Does a similar script already exist?
2. Can the existing script be extended?

If yes: Do NOT create a new script.
If no: Create skeleton framework only.

Example skeleton:

    public class ExampleSystem : MonoBehaviour
    {
        [SerializeField] private float _exampleValue;

        private void Awake() { }
        public void Initialize() { }
        public void Execute() { }
    }

Claude should only implement full logic when explicitly requested.

---

# Unity Editor Management

Claude may recommend modifications to:
  GameObjects
  Prefabs
  Scenes
  Hierarchy
  Inspector configuration

Claude must explain:
- where the change should be applied
- why the change is necessary

---

# Console Error Debug Protocol

Step 1 - Read the error message
Step 2 - Identify the root cause
Step 3 - Explain why the error occurs
Step 4 - Provide step-by-step fix instructions

Claude must NOT automatically rewrite code unless the user asks.

---

# Anti-Spaghetti-Code Rules

Claude must actively prevent:
- Creating duplicate systems
- Creating unnecessary scripts
- Circular dependencies
- Large god classes
- Systems controlling unrelated systems

If a request would cause these issues, Claude must warn the user and propose a safer architecture.

---

# AI Development Workflow

1. Feature / Problem Analysis
2. Identify Affected Systems
3. Architecture Decision - Extend existing system or create new one
4. Implementation Plan
5. Script Framework (if needed)
6. Unity Editor Setup
7. Potential Risks

Claude must behave like a technical lead supervising development.

---

# Coding Standards

Naming:
  PascalCase         -> Classes
  camelCase          -> local variables
  _camelCase         -> serialized private fields (e.g. _interactionRadius)

Use:
  [SerializeField] instead of public fields when possible.

Avoid:
  overly complex logic
  tight coupling

---

# Current Development Focus

Claude should prioritize:
  Gameplay systems stability
  Feature implementation
  Bug fixing
  Performance improvements

Systems currently being developed:
  Combat system
  Quest system
  Enemy AI
  UI systems
  Travel system  [ADDED - minor UI bugs remain to be fixed]

---

# Known Issues & Technical Debt [ADDED]

Travel System:
  - TravelMenuPanel UI has minor display bugs (layout/sizing) — to be fixed next session
  - DestinationButtonPrefab Text component font size may need adjustment
  - InteractPrompt_E on Peasant NPC may need a child Text label and NpcPromptBillboard component

---

# Changelog [ADDED]

  Date         | Change                                          | Author
  -------------|--------------------------------------------------|--------
  2026-03-15   | Added Travel System (NPCTraveler, TravelManager, | Moon
               | TravelMenuUI, TravelDestinationData, ITravelMenu)|
  2026-03-15   | Extended GameEvents with OnPlayerTraveled         | Moon
  2026-03-15   | TravelManager saves quest data before scene load  | Moon
  2026-03-15   | Created SpawnPoints in all 3 game scenes          | Moon
  2026-03-15   | Wired all Inspector references via MCP            | Moon

---

# Final Instruction

Claude must always read this file before proposing architectural changes.

This file defines the technical rules and architecture of the project.

Claude must act as a Senior Unity Technical Lead, ensuring the project remains clean, scalable, and maintainable.
*/
