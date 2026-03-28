/*
# Project Context & Rules: [FinalProject222]

---

# Project Overview

* Genre: Top-down Action RPG
* Engine: Unity (URP)
* Language: C#
* Architecture Goal: Modular gameplay systems with scalable architecture
* Current Development Phase: Core gameplay systems -- functional and stable

Claude must prioritize:

- maintainable architecture
- modular gameplay systems
- minimal coupling between systems

---

# Core Architecture Rules (Critical)

These rules define the architecture of the project. Claude must never violate them.

## System-Based Architecture

The game is divided into gameplay systems:

* Player System
* Combat System
* Enemy AI System
* Quest System
* Inventory System
* UI System
* Save System
* Travel System

Each system must have:

- clear responsibilities
- minimal dependencies
- reusable components

---

# Project Architecture Map

Claude must maintain a mental map of the project architecture.

## Gameplay Layer

  Player (CharacterControl, CombatController)
  Enemy  (SimpleEnemyController, SkeletonMageBoss)
  NPC    (NPCQuestDialog, NPCTraveler)

## System Layer

  CombatSystem    (CombatController -- attached on Player)
  QuestSystem     (QuestManager, QuestTracker, ObjectiveSystem, SaveSystem)
  InventorySystem (InventorySystem, EquipmentSystem)
  TravelSystem    (TravelManager, NPCTraveler, TravelMenuUI)

## Infrastructure Layer

  UIManager / UISystem
  AudioManager (SFXManager, AmbiencePlayer, RandomBGMPlayer)
  VFXManager
  GameManager
  TravelManager   [DontDestroyOnLoad -- Singleton]
  QuestManager    [DontDestroyOnLoad -- Singleton]
  SceneTransitionUI [DontDestroyOnLoad -- Singleton, prefab on TravelManager GO]]

Claude must respect this layered structure.

Rules:
- Gameplay layer uses systems
- Systems may interact with managers
- Managers handle global state
- Gameplay scripts should not directly control unrelated systems

---

# System Dependency Graph

Claude must follow this dependency direction.

## Allowed dependency flow:

  Player          -> CombatController -> CharacterData
  Enemy           -> CharacterData (attack via CharacterData.Attack())
  NPCQuestDialog  -> QuestManager
  NPCTraveler     -> TravelMenuUI -> TravelManager -> SceneManager
  TravelManager   -> SaveSystem (saves quest + inventory + equipment + health before scene load)
  TravelManager   -> QuestManager (reads quest states)
  QuestManager    -> QuestTracker -> ObjectiveSystem
  SaveSystem      -> PlayerPrefs (persistence)
  GameEvents      -> (all systems listen, no direct dependency)

## Avoid:

  UI -> CombatSystem directly
  Enemy -> QuestSystem directly
  Player -> SaveSystem directly

If a dependency violates architecture, Claude must propose a better design.

---

# Namespace Convention

IMPORTANT: Core player and enemy scripts use the namespace CreatorKitCodeInternal.
Data classes use the namespace CreatorKitCode.

Scripts in CreatorKitCodeInternal namespace:
  - CharacterControl
  - CombatController
  - SimpleEnemyController

Scripts in CreatorKitCode namespace:
  - SkeletonMageBoss
  - FireballProjectile
  - CharacterData

Scripts WITHOUT namespace (global):
  - QuestManager, QuestTracker, ObjectiveSystem, SaveSystem
  - TravelManager, NPCTraveler, TravelMenuUI, TravelDestinationData
  - NPCQuestDialog, GameEvents
  - UISystem, VFXManager, SFXManager, DamageUI

Claude must add correct using and namespace declarations when creating scripts.

---

# Script Responsibility Table

Claude must check this before creating scripts.

  Script Type  | Responsibility
  -------------|------------------------------
  Controller   | Handles gameplay logic
  Manager      | Handles global system state
  Data         | Stores configuration data (ScriptableObject)
  Utility      | Reusable helper functions
  AI           | Enemy decision logic
  UI           | Handles display and user interaction

## Player Scripts

  Script                  | Type       | Responsibility
  ------------------------|------------|-------------------------------------------
  CharacterControl        | Controller | WASD movement, camera, click-to-attack input,
                          |            | delegates combat to CombatController
  CombatController        | Controller | Attack state machine (WindUp/Active/Recovery),
                          |            | cone-sweep hit detection, input buffering,
                          |            | knockback. Implements IAttackFrameReceiver.
  CharacterData           | Data/Logic | Health, stats, equipment, attack resolution
  PlayerInteract          | Controller | Interaction with world objects

## Enemy Scripts

  Script                    | Type | Responsibility
  --------------------------|------|-------------------------------------------
  SimpleEnemyController     | AI   | NavMeshAgent-based: IDLE/PURSUING/ATTACKING
                            |      | state machine. Raises GameEvents.RaiseEnemyKilled
                            |      | on death. Implements IAttackFrameReceiver.
  SkeletonMageBoss          | AI   | Boss with 2 skills: Lightning Strike
                            |      | (LightningStrikeController) and Dark Magic
                            |      | AoE (warning disk + VFX + damage delay).
                            |      | States: IDLE/CHASING/CASTING/DEAD.
  LightningStrikeController | AI   | Executes lightning skill sequence for Boss
  FireballProjectile        | AI   | Projectile logic

## Quest System Scripts

  Script          | Type       | Responsibility
  ----------------|------------|-------------------------------------------
  QuestManager    | Manager    | Singleton + DontDestroyOnLoad. Central
                  |            | coordinator. Start/Complete/Fail quests.
                  |            | Reads QuestDatabase (ScriptableObject).
  QuestTracker    | System     | Tracks active quest progress (objectiveCounts).
                  |            | Raises OnProgressUpdated, OnQuestTrackingStopped.
  ObjectiveSystem | System     | Processes GameEvents -> updates QuestTracker
  SaveSystem      | System     | Saves/loads quest + inventory + equipment + health via PlayerPrefsfs
  QuestData       | Data (SO)  | Quest definition: id, name, desc, objectives[]
  QuestDatabase   | Data (SO)  | Collection of all QuestData in project

## Travel System Scripts

  Script                 | Type      | Responsibility
  -----------------------|-----------|------------------------------------------
  TravelManager          | Manager   | Singleton + DontDestroyOnLoad. Handles
                         |           | scene loading, persists SpawnPointID,
                         |           | saves quest + inventory + equipment before
                         |           | scene transition, restores inventory +
                         |           | equipment after load, teleports player
                         |           | to SpawnPoint. Requires ItemRegistry ref.
    SaveSystem      | System     | Saves/loads quest + inventory + equipment +
                  |            | health + scene + metadata via PlayerPrefs.
                  |            | Keys: QuestSaveData, InventorySaveData,
                  |            | EquipmentSaveData, PlayerHealthData,
                  |            | PlayerSceneData (NEW), SaveMetadata (NEW).
                  |            | Static: HasSaveData(), ClearAllData()sitionComplete    | one frame after scene loads.
  NPCTraveler            | Controller| Attached to Peasant NPC. Trigger detection,
                         |           | prompt blink (E key), opens/closes TravelMenuUI.
  TravelMenuUI           | UI        | Pre-wired buttons (no Instantiate at runtime).
                         |           | Implements ITravelMenu. Show()/Hide() called
                         |           | by NPCTraveler. Buttons assigned in Inspector.
  TravelDestinationData  | Data (SO) | Destination name, build index, SpawnPoint ID,
                         |           | description, availability flag.
  SceneTransitionUI      | UI        | Singleton + DontDestroyOnLoad. Full-screen
                         |           | fade transition (CanvasGroup alpha lerp).
                         |           | FadeOut(callback) before LoadScene,
                         |           | FadeIn() after restore. Lives on
                         |           | TravelManager prefab. Used by both
                         |           | TravelManager and MainMenuController.
ITravelMenu            | Interface | Contract: Show(destinations, callback), Hide()

## NPC Scripts

  Script             | Type       | Responsibility
  -------------------|------------|-------------------------------------------
  NPCQuestDialog     | Controller | Quest offer/progress dialog. Trigger-based.
                     |            | Supports prerequisiteQuestIDs gating.
                     |            | Blink prompt (E key). Shared dialoguePanel.
  NpcPromptBillboard | Utility    | Keeps world-space E prompt facing camera

## Quest UI Scripts

  Script              | Type | Responsibility
  --------------------|------|-------------------------------------------
  QuestTrackerManager | UI   | HUD widget -- shows active quest + objectives.
                      |      | Listens to QuestTracker events and
                      |      | GameEvents.OnSceneTransitionComplete.
                      |      | Auto-builds panel if not assigned in Inspector.
  QuestLogUI          | UI   | Full quest log panel
  QuestTrackerUI      | UI   | Single quest tracker row display
  TrackQuestButton    | UI   | Button to toggle QuestTrackerManager panel
  MenuController      | UI   | Controls main menu panels
MiMinimapController   | UI   | Attached to MinimapCamera (child of Character).
                      |      | Locks camera height=30 and rotation=90 in
                      |      | LateUpdate. Camera settings: orthographic,
                      |      | orthoSize=15, depth=-2, clearFlags=SolidColor
                      |      | (black), cullingMask=Everything.lways looks straight down.

---

# Codebase Structure

Assets/
  Scripts/
    Characters/
      Player/
        CharacterControl.cs      [namespace CreatorKitCodeInternal]
        CombatController.cs      [namespace CreatorKitCodeInternal]
        CombatState.cs
        AttackState.cs
        PlayerInteract.cs
      Boss/
        SkeletonMageBoss.cs      [namespace CreatorKitCode]
        LightningStrikeController.cs
        FireballProjectile.cs
      SimpleEnemyController.cs   [namespace CreatorKitCodeInternal]
      TrainingDummy.cs
    CharacterSystem/
      CharacterData.cs
      StatSystem.cs
      EquipmentSystem.cs
      InventorySystem.cs
      BaseElementalEffect.cs
    Quest/
      Core/
        QuestManager.cs          [Singleton, DontDestroyOnLoad]
        QuestTracker.cs
        ObjectiveSystem.cs
        SaveSystem.cs
        GameEvents.cs            [Static event bus -- no MonoBehaviour]
      Data/
        QuestData.cs
        QuestDatabase.cs
      NPC/
        NPCQuestDialog.cs
        NPCTraveler.cs
        NpcPromptBillboard.cs
      UI/
        QuestTrackerManager.cs
        QuestTrackerUI.cs
        QuestLogUI.cs
        TrackQuestButton.cs
        MenuController.cs
    Travel/
      Data/
        TravelDestinationData.cs
      TravelManager.cs           [Singleton, DontDestroyOnLoad]
      ITravelMenu.cs
    UI/
      TravelMenuUI.cs
      SceneTransitionUI.cs   [Singleton, DontDestroyOnLoad -- fade transiti
      MinimapController.cs    [Minimap camera controller -- child of Character]ns]
      UISystem.cs
      DamageUI.cs
      InventoryUI.cs
      EquipmentUI.cs
      ItemTooltip.cs
      LootUI.cs
      MainMenuController.cs
    Audio/
      SFXManager.cs
      AmbiencePlayer.cs
      RandomBGMPlayer.cs
      CharacterAudio.cs
    Items/
      Item.cs, Weapon.cs, UsableItem.cs, EquipmentItem.c
      ItemRegistry.cs                [ScriptableObject registry -- item lookup by name]me]s
      Effects/ (ApplyBurnWeaponEffect, VampiricWeaponEffect, etc.)
      ItemEffect/ (AddHealthEffect, etc.)
    GameplayObject/
      BreakableObject.cs, Container.cs, Loot.cs, LootSpawner.cs, SpawnPoint.cs
    Utility/
      Helpers.cs, RandomLoopOffset.cs, SceneLinkSMB.cs, UIAlphaRaycast.cs
    Managers/ (global managers)
    Editor/ (editor-only tools)
    AnimationControllerDispatcher.cs
    VFXManager.cs, VFXDatabase.cs, VFXTypes.cs
    CameraController.cs
    ResourceManager.cs
    HighlightableObject.cs, InteractableObject.cs
  ScriptableObjects/
    Travel/
      Destination_WesternVillage.asset
      Destination_Desert.asset
      Destination_Necrom.asset
  Prefabs/
    (DestinationButtonPrefab obsolete -- TravelMenuUI now uses pre-wired buttons)
    ItemDatabase/ItemRegistry.asset    [ScriptableObject -- assign in TravelManager Inspector]
    Systems/TravelManager.prefab       [DDOL prefab: TravelManager + SceneTransitionUI + Canvas/FadePanel])
  ScenesRenderTextures/
    MinimapRT.renderTexture      [256x256, used by MinimapCamera Target Texture]
  /
    MainMenu.unity        (Build Index 0)
    Western Village.unity (Build Index 1)  [primary dev scene]
    Desert.unity          (Build Index 2)
    Necrom.unity          (Build Index 3)

Rules:
- New scripts must be placed in the correct system folder
- Avoid placing gameplay logic in Managers
- Keep systems independent

---

# Scene Hierarchy -- Western Village (Reference)

Root GameObjects in Western Village scene:
  Directional Light
  Global Volume
  PlayerCore              [CharacterControl, CombatController, CharacterData...]
  Ground                  [Tilemap / terrain children]
  Peasant                 [NPCTraveler -- Travel NPC]
  Cowboy                  [NPCQuestDialog -- Quest NPC]
  Nolant                  [NPCQuestDialog -- Quest NPC]
  QuestSystem             [QuestManager + QuestTracker + ObjectiveSystem]
  TravelManager           [TravelManager + SceneTransitionUI -- prefab instance]]
  SpawnPoint_WesternVilla
  PlayerCore/Character/MinimapCamera [Camera ortho, depth=-2, renders to MinimapRT]
  -- MinimapPanel (GameUI) + MinimapCamera present in ALL 3 game scenes

---

# Travel System -- Key Design Decisions

## SpawnPoint Convention:

- SpawnPointID in TravelDestinationData must match the GameObject NAME in the target scene.
- TravelManager uses GameObject.Find(spawnPointID) to locate spawn position.
- Each scene has a correctly named SpawnPoint GameObject:
    Western Village -> SpawnPoint_WesternVillage
    Desert          -> SpawnPoint_Desert
    Necrom          -> SpawnPoint_Necrom

## Quest Data Preservation:

- TravelManager.TravelTo() calls SaveQuestDataBeforeTravel() BEFORE LoadScene().
- Saves all quest states and objective progress to PlayerPrefs via SaveSystem.
- New scene QuestSystem.Start() restores data from PlayerPrefs on load.

## TravelMenuUI -- Pre-wired Buttons (NOT runtime Instantiate):

- Buttons assigned in Inspector: Button_WesternVillage, Button_Desert, Button_Necrom.
- Hierarchy: Canvas > TravelMenuPanel > DestinationList > [buttons]
- Buttons with no matching destination are hidden via SetActive(false) at runtime.

## DontDestroyOnLoad Objects:

- TravelManager (Singleton)
- QuestManager  (Singleton)
- SceneTransitionUI (Singleton -- lives on TravelManager GO)
- PlayerCore is DUPLICATED per scene -- NOT shared across scenes.
- When LoadScene(Single) runs, the old scene is destroyed except DDOL objects.

---

# Combat System -- Key Design Decisions

## Architecture:

- CharacterControl handles INPUT and MOVEMENT only.
- CombatController handles all ATTACK LOGIC (separate component, same GameObject).
- CharacterControl calls m_CombatController.TryAttackAt(target) on left mouse click.
- CombatController implements IAttackFrameReceiver -- AttackFrame() fired by animation event.

## Attack Flow:

  Left Click -> CharacterControl.GetClickedCharacterData()
             -> CombatController.TryAttackAt(target)
             -> CombatState: Idle -> WindUp -> Active -> Recovery -> Idle
             -> AttackFrame() [animation event] -> cone sweep hit detection
             -> CharacterData.Attack(target)

## CombatState enum: Idle | WindUp | Active | Recovery

## Performance:

- Pre-allocated RaycastHit[8] and Collider[8] -- no per-frame GC allocation.
- SphereCastNonAlloc + OverlapSphereNonAlloc for hit detection.

---

# Enemy AI -- Key Design Decisions

## SimpleEnemyController:

- NavMeshAgent pathfinding. States: IDLE -> PURSUING -> ATTACKING.
- Raises GameEvents.RaiseEnemyKilled(characterName) on death.
- Implements IAttackFrameReceiver (animation-event-driven damage).

## SkeletonMageBoss:

- States: IDLE -> CHASING -> CASTING -> DEAD.
- Skill 1: Lightning Strike (via LightningStrikeController, range-based).
- Skill 2: Dark Magic AoE (warning disk VFX -> delay -> AoE damage).
- Both skills have independent cooldown timers.
- Raises GameEvents.RaiseEnemyKilled on death.

---

# Event Bus -- GameEvents (Static)

GameEvents is a STATIC class (not MonoBehaviour). No instance needed.

Events:
  OnEnemyKilled            (string enemyID)
  OnItemCollected          (string itemID, int amount)
  OnNPCTalkCompleted       (string npcID)
  OnLocationReached        (string locationID)
  OnQuestProgressChanged   (string questID)
  OnPlayerTraveled         (string destinationName)
  OnSceneTransitionComplete ()

Usage pattern:
  Raise  -> GameEvents.RaiseEnemyKilled("GoblinA");
  Listen -> GameEvents.OnEnemyKilled += MyHandler;
  Unsub  -> GameEvents.OnEnemyKilled -= MyHandler;  // in OnDisable/OnDestroy

---

# Project Analysis Rule (MANDATORY)

Before performing major tasks Claude must:

1. Analyze folder structure
2. Identify major systems
3. Understand script responsibilities
4. Detect system dependencies
5. Map prefab / scene structure

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
  GameObjects, Prefabs, Scenes, Hierarchy, Inspector configuration

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
3. Architecture Decision -- Extend existing system or create new one
4. Implementation Plan
5. Script Framework (if needed)
6. Unity Editor Setup
7. Potential Risks

Claude must behave like a technical lead supervising development.

---

# Coding Standards

Naming:
  PascalCase    -> Classes, Methods, Properties
  camelCase     -> local variables
  _camelCase    -> serialized private fields  (e.g. _interactionRadius)
  m_PascalCase  -> private runtime fields in CreatorKitCode/CreatorKitCodeInternal scripts
                   (legacy convention from original codebase -- keep consistent)

Use:
  [SerializeField] instead of public fields when possible.
  [Header("...")] to organize Inspector fields.
  XML summary comments on public methods.

Avoid:
  overly complex logic in Update()
  tight coupling between systems
  Instantiate/Destroy for frequently spawned objects (use pooling)

---

# Current Development Focus

Systems COMPLETE and stable:
  Travel System      [fully implemented -- persistence + fade transitions complete]]
  Minimap System     [fully implemented -- MinimapCamera + RenderTexture + UI widget]
  Quest System       [fully implemented]
  Combat System      [fully implemented -- CombatController refactor complete]
  Enemy AI           [SimpleEnemyController + SkeletonMageBoss implemented]
  NPC Dialog System  [NPCQuestDialog with prerequisite gating]

Systems that may need expansion:
  Save System        Steps 1-4 complete. Steps 5-8 pending.ing.]]]]
  Inventory / Equipment UI
  Audio wiring per scene (BGM / SFX)

---

# Known Issues & Technical Debt

No critical issues as of 2026-03-20.

Minor notes:
  - SimpleEnemyController is Update()-heavy -- acceptable for current enemy count.
    If enemy count scales, consider coroutine-based patrol or event-driven triggers.
  - SkeletonMageBoss uses Instantiate for VFX -- acceptable for boss frequency.
    If adding more projectile-heavy enemies, consider object pooling.
  - QuestManager and TravelManager both use DontDestroyOnLoad.
    Ensure they are NOT duplicated in Desert / Necrom scenes.
    Singleton guards handle duplicates, but correct scene setup avoids log spam.

---

# Changelog

  Date         | Change                                            | Author
  -------------|---------------------------------------------------|--------
  2026-03-15   | Added Travel System (NPCTraveler, TravelManager,  | Moon
               | TravelMenuUI, TravelDestinationData, ITravelMenu) |
  2026-03-15   | Extended GameEvents with OnPlayerTraveled          | Moon
  2026-03-15   | TravelManager saves quest data before scene load   | Moon
  2026-03-15   | Created SpawnPoints in all 3 game scenes           | Moon
  2026-03-15   | Wired all Inspector references via MCP             | Moon
  2026-03-15   | TravelMenuUI refactored to pre-wired buttons        | Moon
               | (removed runtime Instantiate, fixed layout bugs)   |
  2026-03-16   | CLAUDE.md full rewrite -- reflects actual codebase  | Moon
               | Added: namespace map, combat system design,        |
               | enemy AI details, scene hierarchy, complete        |
               | script responsibility table, known issu
2026-03-20   | Fixed bug: Inventory reset on scene transition      | Moon
               | Root cause: InventorySystem was in-memory only,    |
               | destroyed with PlayerCore on LoadScene().          |
2026-03-20   | Created ItemRegistry ScriptableObject               | Moon
               | (Assets/Scripts/Items/ItemRegistry.cs)             |
               | Assign asset in TravelManager Inspector.           |
2026-03-20   | Extended SaveSystem: added Inventory + Equipment    | Moon
               | persistence via PlayerPrefs (3 separate keys).     |
2026-03-20   | Extended TravelManager: saves inventory + equipment | Moon
               | before LoadScene(), restores after OnSc
2026-03-20   | Fixed bug: QuestTracker not restored after scene    | Moon
               | transition -- QuestManager.RestoreQuestStateAfter  |
               | SceneLoad() now re-tracks Active quests + restores |
               | objective counts via OnSceneTransitionComplete.    |
2026-03-20   | Fixed bug: Weapon (Basic Rake) duplicated in        | Moon
               | inventory on every scene change -- moved Restore   |
               | logic into RestoreAndNotify coroutine (yield null)  |
               | so CharacterData.Init() runs first, m_DefaultWeapon |
               | is set before RestoreEquipment() executes.         |
2026-03-20   | Fixed bug: Player health resets to full on scene    | Moon
               | transition -- SaveSystem now saves/loads health as  |
               | a percentage (PlayerHealthData key in PlayerPrefs). |
               | TravelManager saves before travel, restores after  |
               | equipment is applied in RestoreAndNotify().        |
2026-03-20   | Fixed bug: Desert/Necrom had no QuestSystem/Save    | Moon
               | System -- added QuestSystem to both scenes.        |
               | Removed duplicate TravelManager from Ne
2026-03-21   | Created SceneTransitionUI.cs (Singleton DDOL)       | Moon
               | Full-screen CanvasGroup fade. FadeOut(callback)     |
               | before LoadScene, FadeIn() after restore.           |
               | Placed on TravelManager GO alongside TravelManager. |
2026-03-21   | Added TravelFromMainMenu(sceneName) to TravelManager | Moon
               | Sets _isTraveling=true so OnSceneLoaded() does not  |
               | early-return when entering from MainMenu.           |
               | FadeIn() called after restore completes.            |
2026-03-21   | Updated MainMenuController: removed _transitionUI    | Moon
               | field, now calls TravelManager.Instance.             |
               | TravelFromMainMenu(). FadeIn via                    |
               | SceneTransitionUI.Instance on Start().              |
2026-03-21   | Created TravelManager prefab                         | Moon
               | (Assets/Prefabs/Systems/TravelManager.prefab)       |
               | Contains TravelManager + SceneTransitionUI +        |
               | Canvas (Sort Order 999) + FadePanel (CanvasGroup).  |
               | Placed in MainMenu scene as DDOL entry point.       |
2026-03-21   | Updated CLAUDE.md: SceneTransitionUI added to       | Moon
               | Infrastructure Layer, Script Responsibility Table,  |
               | Codebase Structure, DontDestroyOnLoad section,      |
               | Prefabs section, and Changelog.         
2026-03-21   | Implemented Minimap System                          | Moon
               | - Added layer 17 (Minimap)                          |
               | - Created MinimapController.cs (Scripts/UI/)        |
               | - Created MinimapRT.renderTexture (256x256)         |
               | - MinimapCamera: child of Character, orthographic,  |
               |   depth=-2, culls UI layer, renders to MinimapRT   |
               | - MinimapPanel UI in GameUI (bottom-right, 180x180) |
               |   MinimapMask > MinimapImage (RawImage=MinimapRT)   |
               |   + PlayerDot (red dot, center) + Minim
2026-03-21   | Fixed Minimap: Desert + Necrom missing camera       | Moon
               | targetTexture and wrong depth (was 0, fixed to -2). |
               | Fixed MinimapPanel placed in UIRoot instead of      |
               | UIRoot/GameUI in Desert and Necrom scenes.          |
               | All 3 scenes verified: Camera OK, Panel in GameUI,  |
               | texture=MinimapRT, ortho=True, depth=-2
2026-03-21   | Fixed Minimap blank/yellow display (all 3 scenes)   | Moon
               | Root cause: clearFlags=Skybox filled RenderTexture   |
               | with Desert's yellow skybox before rendering terrain.|
               | Fix: clearFlags=SolidColor (black), orthoSize 25->15 |
               | (more detail), cullingMask=Everything (all layers).  |.            |apBorder    |            |crom.       |eneLoaded
2026-03-22   | Fixed Minimap yellow square (Western Village)       | Moon
               | Root cause: MinimapBorder UI GameObject (Image with |
               | solid yellow color) was layered on top of           |
               | MinimapImage (RawImage), fully blocking the          |
               | RenderTexture output from being visible.             |
               | Fix: Deleted MinimapBorder GameObject from scene.   |
               | Also hardened: clearFlags=SolidColor, depth=-2,     |
               | MinimapRT re-assigned to Camera + RawImage.         |
               | Western Village scene saved via MCP.    
2026-03-22   | Implemented Minimap dot color system (all 3 scenes) | Moon
               | Player=Green, NPC=Blue, Enemy=Red.                  |
               | Extended MinimapController.cs: SpawnDotsForTag()    |
               | + UpdateDots() + RegisterEnemy() for runtime spawn. |
               | Fixed _mapHalfSize=0 bug: moved Canvas.ForceUpdate  |
               | Canvases() + rect.width read into Start() (was in    |
               | Awake() before Canvas layout ran). Added safety      |
               | recalc guard in UpdateDots().                       |
               | Tagged NPCs (Peasant/Cowboy/Nolant) as "NPC" and    |
               | Enemies as "Enemy" in all 3 scenes.                 |
               | Wired MinimapController Inspector in Desert+Necrom. |            
2026-03-22   | Refactored Minimap dot system (all 3 scenes)         | Moon
               | Replaced UI overlay + script math approach with      |
               | world-space MinimapMarker.cs (quad on layer Minimap). |
               | Created MinimapMarker.cs (Scripts/UI/) -- attaches   |
               | flat quad child above entity, URP Unlit material,    |
               | only visible to MinimapCamera (cullingMask).         |
               | Simplified MinimapController.cs to camera-lock only. |
               | Added MinimapMarker to all NPCs (blue) and Enemies   |
               | (red) in all 3 scenes. Main Camera cullingMask       |
               | updated to exclude Minimap layer in all 3 scenes.   |
2026-03-22   | Implemented Quest Item Reward system                | Moon
               | Extended QuestData.cs: added itemReward (Item SO)   |
               | field + using CreatorKitCode namespace.             |
               | Extended QuestManager.CompleteQuest(): grants item  |
               | reward via CharacterData.Inventory.AddItem() after  |
               | quest completes. Fixed syntax error in CompleteQuest|
               | caused by bad text edit (anchor_replace used).      |
               | Assigned rewards in Inspector:                      |
               | Quest_01 -> MetalAxe.asset                          |
               | Quest_02 -> WarriorHelmet.asset                     |
               | Quest_03 -> LegendaryRake.asset                     ||(). |es updated  |
2026-03-27   | Planned Save/Load System for game progress          | Moon
               | Audit confirmed: SaveSystem was scene-transition     |
               | bridge only. Extended to full game-progress save.   |
               | Step 1 (Audit): Confirmed 4 existing keys valid.    |
               |   Added 2 new keys: PlayerSceneData, SaveMetadata.  |
               | Step 2 (Models): Added SceneSaveModel,              |
               |   SaveMetadata classes to SaveSystem.cs.            |
               | Step 3 (Methods): Added SaveSceneData/LoadSceneData,|
               |   SaveMetadataEntry/LoadMetadata, HasSaveData()     |
               |   static, ClearAllData() static, ClearSceneData,   |
               |   ClearMetadata. Updated ClearAllSaveData() to      |
               |   include all 6 keys. No existing code changed.     |
   | Step 4 (SaveAll): Added SaveAll(CharacterData, sceneIndex,  |
               |   spawnPointID) to SaveSystem.cs. Updated TravelTo() in  |
               |   TravelManager to call SaveSceneData + SaveMetadataEntry |
               |   after existing saves. No existing save logic changed.   |
               | Steps 5-8 pending: Manual Save, New Game flow,           |
               |   Continue button, Testing.                               | button.      |
2026-03-27   | SAVE/LOAD SYSTEM PLAN -- full game progress save    | Moon
               | Scope confirmed: scene index + spawnPoint (new),   |
               | quest/inventory/equipment/health (existing).        |
               | Architecture decision: extend SaveSystem.cs only,  |
               | no new scripts, PlayerPrefs + JSON retained.        |
               | Plan broken into 8 steps:                           |
               |   Step 1 (DONE) -- Audit & scope definition         |
               |   Step 2 (DONE) -- Added SceneSaveModel,           |
               |     SaveMetadata models + SceneSaveKey,            |
               |     MetadataSaveKey constants to SaveSystem.cs      |
               |   Step 3 (DONE) -- Added SaveSceneData(),          |
               |     LoadSceneData(), ClearSceneData(),             |
               |     SaveMetadataEntry(), LoadMetadata(),           |
               |     ClearMetadata(), HasSaveData() (static),       |
               |     ClearAllData() (static) to SaveSystem.cs.      |
               |     Updated ClearAllSaveData() to include          |
               |     ClearSceneData() + ClearMetadata().            |
               |   Step 4 (PENDING) -- SaveAll() wrapper +          |
               |     TravelManager saves scene before LoadScene()   |
               |   Step 5 (PENDING) -- Manual Save trigger          |
               |     (Pause Menu -- can be deferred)                |
               |   Step 6 (PENDING) -- New Game: ClearAllData()     |
               |     before TravelFromMainMenu()                    |
               |   Step 7 (PENDING) -- Continue button:             |
               |     HasSaveData() check + LoadSceneData() +        |
               |     TravelFromMainMenu(savedScene)                 |
               |   Step 8 (PENDING) -- Testing & edge cases         |

---

# Final Instruction

Claude must always read this file before proposing architectural changes.

This file defines the technical rules and architecture of the project.

Claude must act as a Senior Unity Technical Lead, ensuring the project remains
clean, scalable, and maintainable.
-

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
