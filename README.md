# HTTX (Holotable table top exercise) - Senior Design Capstone Spring '25

**Team Members:**

* Hagen Farrell - AI navigation developer / Project manager
* Kirk Lefevre - Game phase developer
* Craig Fisher - Game Phase developer / Animation developer
* Zoe Krishtul - Multiplayer developer
* Dario Antonio - Multiplayer developer / Tooling developer
* Avery Millard - Tooling developer / Role developer

**Faculty Advisor:** Dr. Matthew Gerber
**Sponsor:** IST METIL

---

## Table of Contents

1.  [Project Overview](#project-overview)
2.  [Goals and Objectives](#goals-and-objectives)
3.  [Technology Stack](#technology-stack)
4.  [Key Features](#key-features)
5.  [Project Setup & Installation](#project-setup--installation)
6.  [Usage](#usage)
7.  [Project Structure](#project-structure)
8.  [Challenges and Problems Encountered](#challenges-and-problems-encountered)
9.  [Current Status and Remaining Work](#current-status-and-remaining-work-to-do)
10. [Recommendations for Future Teams](#recommendations-for-future-teams)
11. [License](#license)
12. [Acknowledgments](#acknowledgments)

---

## Project Overview

* **What is the project?**
    * HTTX (Holotable Table Top Exercise) is an instructor-led, multiplayer tactical training exercise designed for 6+ participants. It simulates an alarm response scenario, specifically a radiological theft within a hospital setting. The primary goal is for participants, playing various roles, to collaboratively position law enforcement (LLE) and fire department (FD) assets to prevent adversaries from successfully removing a radiological source from the premises.
* **Background:**
    * This project was developed for the sponsor, IST METIL, to provide a dynamic, role-specific training environment for tactical response teams. It aims to simulate critical decision-making in a no-fault environment during a high-stress event like a radiological material theft. The scenario progresses through distinct phases, from the initial hostile takeover to the adversaries' egress attempt.
* **Intended Users/Audience:**
    * Primarily personnel involved in emergency and tactical response within the defense industry and related fields, including Local Law Enforcement (LLE), Fire Department (FD),  and Dispatchers. Instructors utilize the tool to guide the exercise, and spectator roles are also available.

## Goals and Objectives

* **Primary Goal:**
    * To develop a robust, multiplayer, instructor-led tactical training simulation (HTTX) for alarm response scenarios involving radiological threats, enabling participants to practice coordinated responses and prevent threat egress.
* **Key Objectives:**
    * Develop a stable multiplayer environment supporting 6+ participants simultaneously within the Unity/Mirror framework.
    * Implement distinct, role-specific views, controls, and tools (e.g., LLE/FD character/vehicle control, RADeye tool access, Dispatcher camera views).
    * Create a controllable, phased narrative engine allowing the instructor to guide the scenario through predefined stages of a radiological theft.
    * Implement core interactive tools, including character/vehicle movement for LLE/FD and a functional RADeye tool displaying simulated dose rates with object shielding considerations.
    * Ensure consistent, repeatable narrative progression and character placement for non-player characters (NPCs) across different simulation sessions.
    * Develop comprehensive instructor controls, including narrative progression, egress point selection, simulation settings (audio/visuals), and initiating LLE firing actions.

## Technology Stack

* **Programming Languages:** C#
* **Frameworks/Libraries:** Unity Mirror Networking (Mirror)
* **Development Tools:** GitHub, Unity Editor (Version 2021.1.4f1)

## Key Features

* **Multiplayer Sessions:** Supports 6+ participants joining a session via KCP protocol (local network), with one acting as host/instructor and others as clients.
* **Role-Based Access:** Participants select roles (LLE, FD, Dispatcher, RSO, OSS, Instructor, Spectator) which determine their viewpoint, available tools, and interactions within the simulation.
* **Instructor-Led Narrative:** The instructor controls the progression of the simulation through predefined phases of a radiological theft scenario (7 phases implemented).
* **Character/Vehicle Movement:** LLE and FD participants can command their respective units to move to specific locations on the exterior of the building using a point-and-click interface. Vehicles require a character to be "inside" to move.
* **RADeye Simulation:** LLE, FD, and Instructor roles can use a RADeye tool to check the simulated radiation dose rate on specific characters, which accounts for distance and shielding from objects like vehicles.
* **Dispatcher View:** The Dispatcher role has access to a unique view simulating security camera feeds from inside the building.
* **Instructor Controls:** The instructor has overarching control, including managing narrative phases, selecting adversary egress points, triggering LLE weapon fire, and adjusting simulation settings (visuals/audio).

## Project Setup & Installation

* **Prerequisites:**
    * Unity Editor (Version **2021.1.4f1** specifically)
    * Git client
    * A computer with a dedicated graphics card is recommended for adequate performance.
* **Software Setup Steps:**
    1.  Clone the repository: `git clone https://github.com/HagenFarrell/HospitalScenario.git`
    2.  Navigate to the project directory: `cd HospitalScenario`
    3.  Open the `HospitalScenario` project using the Unity Hub (ensure Unity version 2021.1.4f1 is installed and selected).
    4.  Once the project is open in the Unity Editor, locate and open the main scene file within `Assets/_HTTX/Scenes/`
    5.  No additional package installations or configurations are typically required beyond letting Unity import the project assets.
* **Hardware Setup:**
    * it runs on a standard PC. Ensure PC meets Unity's minimum requirements, with a dedicated GPU recommended.

## Usage

* **Running the Project (Editor):**
    1.  Open the main project scene (e.g., `HospitalScene`) in the Unity Editor.
    2.  Click the "Play" button at the top center of the editor interface.
* **Running Multiplayer Tests:**
    1.  **Host (Instructor):** Start the project in the Unity Editor. Select the "Instructor" role. Choose to "Host" the game.
    2.  **Client (Participants):**
        * Option 1 (Second Editor Instance): You can run a second instance of the Unity editor with the same project (if your machine can handle it).
        * Option 2 (Build and Run): Use Unity's `File -> Build and Run` option to create a standalone executable. Run this executable.
        * In the client instance (either editor or build), select the desired role (e.g., LLE, FD) and choose "Client". Enter the IP address of the host machine (must be on the same local network) and connect.
* **Basic Workflow/Example Usage:**
    1.  The Instructor starts the application and hosts a server session, selecting the "Instructor" role.
    2.  Other participants start the application, select their roles (LLE, FD, Dispatcher, etc.), and connect as clients to the Instructor's IP address.
    3.  Once all participants are connected, the Instructorinitiates Phase 1 of the narrative scenario using "0" and "9" to move forward and backwards. Respective of the keys location.
    4.  Participants interact based on their roles (e.g., LLE moves units, Dispatcher observes cameras) as the Instructor advances through the phases.
    5.  The session continues until the Instructor ends the simulation.
* **Testing:**
    * Testing is primarily done through manual interaction and running multiplayer sessions as described above. There are no automated test suites currently configured.

## Project Structure

* The core project assets specific to this simulation are organized within the `Assets/_HTTX` folder. The structure inside `_HTTX` is as follows:
    ```
    HospitalScenario/       # Root folder cloned from Git
    ├── Assets/
    │   ├── _HTTX/          # Main folder for project-specific assets
    │   │   ├── Animation/
    │   │   ├── Audio/
    │   │   ├── Materials/      # Also Asphalt_materials, etc.
    │   │   ├── Mesh/
    │   │   ├── Mirror-35.1.0/  # Mirror Networking assets
    │   │   ├── Resources/
    │   │   ├── Scenes/         # Main simulation scene(s) located here
    │   │   ├── scripts/        # All C# scripts for the project
    │   │   ├── shaders/
    │   │   ├── Textures/
    │   │   ├── prefabs/        # Core prefabs (Characters, vehicles, UI, tools)
    │   │   ├── ui/             # UI specific assets
    │   │   └── ...             # Other folders like images, nappin, stuff for radeye
    │   └── ...             # Other standard Unity folders & third-party assets outside _HTTX (e.g., TextMeshPro)
    ├── Packages/           # Unity package manager manifest (package.json)
    ├── ProjectSettings/    # Unity project configuration
    ├── .gitignore
    └── README.md           # This file
    ```

## Challenges and Problems Encountered

* **Challenge 1: Integration with Raydiance Package / Avalon Holographics Template**
    * **Description:** A primary goal towards the end of the project was to port the HTTX simulation to a specific hardware template provided by Avalon Holographics, which utilizes their "Raydiance" Unity package. This package includes its own networking components.
    * **Problem:** Significant difficulties arose when trying to integrate our existing multiplayer functionality (built using Mirror Networking) with the Raydiance network manager. The Raydiance codebase was found to be complex, and its network manager required substantial refactoring to work alongside or replace our Mirror-based player prefabs and scripts.
    * **Attempts:** Initial attempts were made to understand the Raydiance networking code and identify points of integration or replacement.
    * **Outcome:** **Unresolved.** Due to the complexity and the extensive refactoring required for the Raydiance network manager, the integration was not successfully completed. The project currently remains functional using the standard Mirror Networking implementation.
    * **Insights/Hurdle:** This represents a major end-of-project hurdle. The next team tasked with porting to the Avalon Holographics hardware will need to dedicate significant time to either deeply refactor the Raydiance networking components to work with the existing HTTX structure OR potentially rebuild the HTTX multiplayer logic using the Raydiance networking system. Understanding the Raydiance package will be critical.

## Current Status and Remaining Work (To-Do)

* **Project Status:** **Fully Functional (Mirror Version)**. The current version of the project, utilizing Mirror for networking, is fully functional and implements the core requirements outlined in the scenario document.
* **Completed Work:**
    * All specified roles (LLE, FD, Dispatcher, RSO, OSS, Instructor, Spectator) are implemented with role-specific views and controls.
    * The complete 7-phase narrative scenario is implemented and controllable by the Instructor.
    * Multiplayer functionality via Mirror Networking is stable on a local network (LAN).
    * LLE/FD character movement is functional.
    * LLE/FD vehicle movement (with character present) is functional.
    * RADeye tool is implemented and functional.
    * Instructor-triggered LLE firing interaction is implemented.
* **Known Bugs / Issues:**
    * **Intermittent Animation Bug:** An issue has occurred *once* where Fire Department (FD) units became unanimated during Phase 7. The trigger for this is unknown, and it has not been reliably recreated. Most of the time, animations function correctly through all phases.
    * **Raydiance Incompatibility:** The Mirror-based implementation is incompatible with the target Avalon Holographics/Raydiance template environment (See Challenges section).
* **Future Work / To-Do List:**
    * [ ] **Major Task: Investigate/Implement Raydiance Integration:** Address the challenges outlined above to port the simulation to the Avalon Holographics Raydiance environment. This involves resolving network manager conflicts and player instantiation issues.
    * [ ] **Investigate/Fix FD Animation Bug:** Attempt to identify the cause of the rare Phase 7 FD unit animation issue and implement a fix.

## Recommendations for Future Teams

* **Focus on Raydiance Integration:** The primary technical hurdle is integrating with the Avalon Holographics template, specifically its "Raydiance" package and associated light field camera hardware/software.
    * **Understand Raydiance Networking:** Dedicate significant time upfront to thoroughly understand the networking manager included in the Raydiance package. This was the main blocker previously.
    * **Player Instantiation:** Investigate player prefab instantiation within the Raydiance template. Currently, player prefabs based on the Mirror setup appear to be destroyed upon loading in the Raydiance environment. Solving this is critical.
    * **Role Selection Integration:** Even if instantiation is fixed, ensure the `Player.cs` role selection logic correctly registers the player's chosen role within the Raydiance networking system. This connection needs to be established.
* **UI Interaction Workaround (Light Field Camera):**
    * Be aware of potential conflicts between standard Unity UI interaction (mouse clicks) and the light field camera's input handling.
    * A partial workaround was implemented by Hagen Farrell:
        * A script was created to detect if the cursor is over a UI element.
        * If so, it forwards the mouse click directly to Unity's EventSystem, bypassing the light field camera's input capture for that click.
        * The script execution order was modified so this detection script runs *before* the light field camera's input scripts, ensuring UI clicks are intercepted correctly when needed.
        * *(Future teams may need to refine or adapt this workaround depending on further Raydiance integration progress).*
* **General Advice:**
    * Allocate extra time specifically for network testing, especially if attempting Raydiance integration.
    * Document any changes made during the Raydiance integration process meticulously.

## Acknowledgments

* Special thanks to our Faculty Advisor, **Dr. Matthew Gerber**, for guidance and support throughout this project.
* We also thank our sponsor, **IST METIL**, for providing the project opportunity and context.

---
