# Prompt log

1. **Goal:** "Can you do a general check of the project and see whether it works correctly?"
   **Planned activity:** examine the existing structure, configuration and code, then run build and tests and fix any problems found.

2. **Goal:** "I would remove the kpi and oeekpi libraries... I saved them. I want to make the project modular. The application must be called IndustrialSimApp. ConveyorSimApp must become a lib. I must be able to start IndustrialSimApp as a Conveyor or, in the future, as something else, for which I create other specific libs. I must be able to expose that machine as OPC UA, but in the future also Modbus or S7, so that all the relevant simulations can be done. Can you update the solution accordingly? Can you create all the relevant documentation files?" (translated from Italian)
   **Planned activity:** remove the KPI/OEE modules, turn Conveyor into a reusable machine module and introduce IndustrialSimApp as a modular host with interchangeable industrial protocols, tests and complete documentation.

3. **Goal:** "I found some nice infographics about VFD drives: I would update the project with this information. Save the images in the documentation. Then access the project D:\Dev6\IoTHub\OpcUaExperiments and put it in here and integrate it. Make a work plan so that another GenAI can do it: you plan and check. Put all the log and progress status in D:\Dev\BuildArtifacts\IndustrialSimExperiments." Follow-up: everything in English, including the imported course; then "proceed yourself" (translated from Italian).
   **Planned activity:** keep the (third-party, local-only) infographics as reference, document VFD concepts and model coverage, improve the VFD model in C# and Python, import the OPC UA course as `src/OpcUaCourse` translated to English, integrate it with ForgeSim, and keep plan, status and logs in `D:\Dev\BuildArtifacts\IndustrialSimExperiments`.
