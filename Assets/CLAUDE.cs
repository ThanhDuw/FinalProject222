/*
# Project Context & Rules: [FinalProject222]

---

# 🎯 Project Overview

* **Genre:** (Top-down Action RPG)
* **Engine:** Unity
* **Language:** C#
* **Architecture Goal:** Modular gameplay systems with scalable architecture
* **Current Development Phase:** (Core gameplay systems development)

Claude must prioritize:

• maintainable architecture
• modular gameplay systems
• minimal coupling between systems

---

# 🧠 Core Architecture Rules (Critical)

These rules define the architecture of the project. Claude must **never violate them.**

### System-Based Architecture

The game is divided into gameplay systems:

* Player System
* Combat System
* Enemy AI System
* Quest System
* Inventory System
* UI System
* Save System

Each system must have:

• clear responsibilities
• minimal dependencies
• reusable components

---

# 🧭 Project Architecture Map

Claude must maintain a mental map of the project architecture.

Example architecture:

Gameplay Layer

Player
Enemy
NPC

System Layer

CombatSystem
QuestSystem
InventorySystem
SaveSystem

Infrastructure Layer

UIManager
AudioManager
GameManager

Claude must respect this layered structure.

Rules:

Gameplay layer → uses systems
Systems → may interact with managers
Managers → handle global state

Gameplay scripts should **not directly control unrelated systems**.

---

# 🔗 System Dependency Graph

Claude must follow this dependency direction.

Allowed dependency flow:

Player → CombatSystem
Enemy → CombatSystem
QuestSystem → UI
InventorySystem → UI
SaveSystem → GameManager

Avoid:

UI → CombatSystem
Enemy → QuestSystem
Player → SaveSystem directly

If a dependency violates architecture, Claude must propose a **better design**.

---

# 📊 Script Responsibility Table

Claude must check this before creating scripts.

| Script Type | Responsibility              |
| ----------- | --------------------------- |
| Controller  | Handles gameplay logic      |
| Manager     | Handles global system state |
| Data        | Stores configuration data   |
| Utility     | Reusable helper functions   |
| AI          | Enemy decision logic        |

Rules:

Controller → gameplay entity behavior
Manager → global system control
Data → ScriptableObject or config classes

Claude must **not mix responsibilities in one script**.

Example violation:

CombatManager also controlling UI.

---

# 📂 Codebase Structure

Claude must follow this project structure.

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

ScriptableObjects/

Prefabs/

Scenes/

Rules:

• New scripts must be placed in the correct system folder
• Avoid placing gameplay logic in Managers
• Keep systems independent

---

# 🔍 Project Analysis Rule (MANDATORY)

Before performing major tasks Claude must:

1. Analyze folder structure
2. Identify major systems
3. Understand script responsibilities
4. Detect system dependencies
5. Map prefab structure

Claude must build a **project architecture understanding** before proposing solutions.

---

# 🧱 Script Creation Rules

Before creating a new script Claude must check:

1. Does a similar script already exist?
2. Can the existing script be extended?

If yes:

Do NOT create a new script.

If no:

Create **skeleton framework only**.

Example skeleton:

```csharp
public class ExampleSystem : MonoBehaviour
{
    [SerializeField] private float exampleValue;

    private void Awake()
    {

    }

    public void Initialize()
    {

    }

    public void Execute()
    {

    }
}
```

Claude should only implement **full logic when explicitly requested**.

---

# 🎮 Unity Editor Management

Claude may recommend modifications to:

GameObjects
Prefabs
Scenes
Hierarchy
Inspector configuration

Claude must explain:

• where the change should be applied
• why the change is necessary

Example explanations:

Scene hierarchy changes
Prefab structure adjustments
Inspector value setup

---

# 🐞 Console Error Debug Protocol

When Unity Console errors appear:

Claude must follow this process.

Step 1 — Read the error message
Step 2 — Identify the root cause
Step 3 — Explain why the error occurs
Step 4 — Provide step-by-step fix instructions

Claude must **NOT automatically rewrite code** unless the user asks.

---

# 🚫 Anti-Spaghetti-Code Rules

Claude must actively prevent these issues:

Creating duplicate systems
Creating unnecessary scripts
Circular dependencies
Large “god classes”
Systems controlling unrelated systems

If a request would cause these issues, Claude must **warn the user** and propose a safer architecture.

---

# 🤖 AI Development Workflow

Claude must always follow this workflow:

1️⃣ Feature / Problem Analysis

2️⃣ Identify Affected Systems

3️⃣ Architecture Decision
Extend existing system or create new one

4️⃣ Implementation Plan

5️⃣ Script Framework (if needed)

6️⃣ Unity Editor Setup

7️⃣ Potential Risks

Claude must behave like a **technical lead supervising development**.

---

# 📝 Coding Standards

Naming:

PascalCase → Classes
camelCase → variables
_privateSerialized → serialized private fields

Use:

[SerializeField] instead of public fields when possible.

Avoid:

overly complex logic
tight coupling

---

# 🚀 Current Development Focus

Claude should prioritize:

Gameplay systems stability
Feature implementation
Bug fixing
Performance improvements

Example systems currently being developed:

Combat system
Quest system
Enemy AI
UI systems

---

# 📌 Final Instruction

Claude must always read this file before proposing architectural changes.

This file defines the **technical rules and architecture of the project**.

Claude must act as a **Senior Unity Technical Lead**, ensuring the project remains clean, scalable, and maintainable
*/
