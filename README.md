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
11. [Documentation & Resources](#documentation--resources)
12. [License](#license)
13. [Acknowledgments](#acknowledgments)

---

## Project Overview

* **What is the project?**
    * [Provide a concise, high-level summary of the project. What problem does it aim to solve? What is the main purpose or outcome?]
* **Background:**
    * 
* **Intended Users/Audience:**
    * Defense industry

## Goals and Objectives

* **Primary Goal:**
    * [State the main overarching goal of the project.]
* **Key Objectives:**
    * [List the specific, measurable objectives your team set out to achieve. Use bullet points.]
    * Objective 1: [e.g., Develop a functional prototype capable of X.]
    * Objective 2: [e.g., Achieve Y performance metric.]
    * Objective 3: [e.g., Integrate components A and B.]
    * ...

## Technology Stack

* **Programming Languages:** C#
* **Frameworks/Libraries:** Unity MIRROR 35.1 
* **Development Tools:** GitHub, Unity 2021.1.4f1

## Key Features

* [List the main functionalities implemented in the project.]
* **Feature 1:** [Brief description of the feature.]
* **Feature 2:** [Brief description of the feature.]
* **Feature 3:** [Brief description of the feature.]
* ...
* [Optional: Include screenshots, GIFs, or links to videos demonstrating key features if helpful.]

## Project Setup & Installation

* **Prerequisites:**
    * [List any software, hardware, or accounts needed before setup. e.g., Python 3.9+, Node.js v16+, Specific OS, AWS Account]
* **Software Setup Steps:**
    1.  Clone the repository: `git clone [repository URL]`
    2.  Navigate to the project directory: `cd [project-directory-name]`
    3.  [Install dependencies: e.g., `pip install -r requirements.txt` or `npm install`]
    4.  [Database setup/migrations: e.g., `python manage.py migrate`]
    5.  [Configuration: Explain any necessary environment variables or configuration files (e.g., `.env`, `config.json`). Mention if there's an example file (`.env.example`). DO NOT commit sensitive keys.]
    6.  [Any other specific setup steps]
* **Hardware Setup (if applicable):**
    * [Describe how to connect any hardware components. Include wiring diagrams or links to them if available.]
    * [Mention any specific firmware flashing steps.]

## Usage

* **Running the Project:**
    * [How do you start the application/system? e.g., `python main.py`, `npm start`, `docker-compose up`]
* **Basic Workflow/Example Usage:**
    * [Provide a simple walkthrough of how to use the core functionality. Step-by-step instructions are best.]
    * 1. [Start the application/system as described above.]
    * 2. [Perform Action A (e.g., Navigate to URL, press button X).]
    * 3. [Perform Action B (e.g., Input data Y, observe result Z).]
* **Testing:**
    * [How can someone run the tests (if any exist)? e.g., `pytest`, `npm test`]

## Project Structure

* [Provide a brief overview of the repository's directory structure. Highlight key folders and their purpose.]
    ```
    .
    ├── data/             # Raw or processed data files
    ├── docs/             # Project documentation, reports, diagrams
    ├── hardware/         # Schematics, PCB designs, CAD files (if applicable)
    ├── src/ or [app_name]/ # Main source code directory
    │   ├── modules/      # Core logic modules
    │   ├── api/          # API endpoints (if applicable)
    │   ├── ui/           # User interface components (if applicable)
    │   └── main.py       # Main entry point script
    ├── tests/            # Unit tests, integration tests
    ├── scripts/          # Helper scripts (e.g., deployment, data processing)
    ├── .gitignore        # Files ignored by Git
    ├── requirements.txt  # Python dependencies (or package.json, etc.)
    └── README.md         # This file
    ```
    * [Adjust the example structure to match your project.]

## Challenges and Problems Encountered

* [This is a critical section for the next team. Be honest and detailed.]
* **Challenge 1: [Brief Title, e.g., Sensor Integration Issues]**
    * [Describe the challenge. What was the problem? When did it occur (especially note issues towards the end)?]
    * [What approaches did you try to solve it?]
    * [What was the outcome? Was it resolved, partially resolved, or remains an open issue?]
    * [Any insights or specific difficulties encountered (e.g., library incompatibility, hardware limitations, noisy data)?]
* **Challenge 2: [Brief Title, e.g., Performance Bottlenecks]**
    * [Description, attempts, outcome, insights.]
* **Challenge 3: [Brief Title, e.g., Difficulties with Algorithm X]**
    * [Description, attempts, outcome, insights.]
* **End-of-Project Hurdles:**
    * [Specifically mention any problems that arose late in the project timeline that might not be fully resolved or documented elsewhere. e.g., last-minute integration failures, unexpected bugs during final testing, difficulties meeting a specific requirement under pressure.]

## Current Status and Remaining Work (To-Do)

* **Project Status:** [Overall assessment - e.g., Functional prototype, Proof-of-concept, Partially complete, Meets X out of Y objectives]
* **Completed Work:**
    * [Briefly summarize what has been successfully implemented and tested.]
* **Known Bugs / Issues:**
    * [List specific bugs or limitations you are aware of.]
    * [Issue 1: Description, steps to reproduce if known.]
    * [Issue 2: Description.]
* **Future Work / To-Do List:**
    * [This is the primary handover list for the next team. Be specific.]
    * [ ] **Task 1:** [e.g., Implement user authentication.]
    * [ ] **Task 2:** [e.g., Improve accuracy of sensor readings (potential methods: X, Y).]
    * [ ] **Task 3:** [e.g., Refactor the data processing module for better efficiency.]
    * [ ] **Task 4:** [e.g., Complete integration of component Z.]
    * [ ] **Task 5:** [e.g., Add comprehensive unit tests for module A.]
    * [ ] **Task 6:** [e.g., Deploy the application to [Platform].]
    * [ ] **Task 7:** [e.g., Resolve known bug #1 mentioned above.]

## Recommendations for Future Teams

* [Offer advice based on your experience.]
* **Technical Suggestions:**
    * [e.g., "Consider using library X instead of Y for feature Z, as we found Y had limitations."]
    * [e.g., "Focus on optimizing the database queries in file `abc.py` early on."]
    * [e.g., "Be careful with the power requirements for hardware component Q."]
* **Process Suggestions:**
    * [e.g., "Allocate more time for integration testing than initially planned."]
    * [e.g., "Document configuration changes meticulously."]
* **Potential Enhancements (Beyond To-Do):**
    * [Ideas for features or improvements that were outside your scope but could be valuable additions.]

## Documentation & Resources

* **Final Report:** [Link to your final project report PDF/document]
* **Presentation Slides:** [Link to final presentation slides]
* **Design Documents:** [Link to system architecture diagrams, UI mockups, requirements documents]
* **Datasheets (for hardware):** [Links to important datasheets]
* **External Tutorials/References:** [Links to helpful articles, tutorials, or documentation used]

## License

* [Specify the project's license. If unsure, check with your university/department. Common options include MIT, Apache 2.0, GPL, or specific university licenses. Example:]
* This project is licensed under the [Your License Name] - see the `LICENSE.md` file (if applicable) for details. If no `LICENSE.md` file is present, contact the original team or faculty advisor regarding usage rights.

## Acknowledgments

* [Thank anyone who provided significant help - advisors, sponsors, TAs, other students, open-source projects you relied on.]
* Special thanks to [Name/Group] for their guidance and support.
* This project utilized the following open-source libraries: [List key libraries if desired].

---
