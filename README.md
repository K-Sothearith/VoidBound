# VOIDBOUND

*A first-person platforming expedition through floating ruins in a dark, futuristic void.*

VoidBound is a single-player Unity game created for the CADT Game Development group project. Cross seven obstacle-course sections, survive hazards, solve a physics puzzle, and reach the final extraction portal.

## Objective and rules

Reach the extraction portal at the end of Section 7.

- Start each run with **three lives**. Touching spikes or active lasers, or falling into the void, costs one life.
- Swinging hammers knock you away; recover your footing or risk falling.
- With lives remaining, respawn at your latest checkpoint. Before activating a checkpoint, respawn at the starting platform.
- There are **two checkpoints**: the entrances to **Section 4** and **Section 6**.
- Time laser cycles, ride moving platforms, and leave collapsing stones before they fall.
- Push both crates onto the two pressure plates to open the puzzle chamber's exit. Use its reset terminal if needed.
- Losing all lives ends the run. Restart restores lives, time, checkpoints, puzzle and obstacle states.
- Entering the final portal completes the game and displays your completion time and death count. The timer measures your run; it is not a countdown.

## The seven sections

| Section | Challenge |
|---|---|
| 1 — Movement Tutorial | Learn movement, jumping and interaction |
| 2 — Hammer Swing | Cross the bridge between three swinging hammers |
| 3 — Moving Platform | Summon a platform using a pressure plate, then ride across the gap |
| 4 — Spike Path | Navigate narrow platforms and spike formations |
| 5 — Laser Corridor | Time pulsing, moving and rotating laser hazards |
| 6 — Physics Chamber | Push two crates onto pressure plates to unlock the exit |
| 7 — Final Gauntlet | Survive lasers, platform transfers and collapsing stones to reach extraction |

## Controls

| Input | Action |
|---|---|
| **W / A / S / D** | Move |
| **Mouse** | Look |
| **Space** | Jump |
| **Left Shift** | Sprint |
| **E** | Interact with a nearby terminal, including the crate reset terminal |
| **Esc** | Pause / resume |
| **On-screen buttons** | Start, resume, restart or quit when available |

Push crates by walking into them. Follow the on-screen interaction prompts near terminals.

## Unity version and requirements

| Component | Version / requirement |
|---|---|
| Unity Editor | **6000.3.8f1** (Unity 6) |
| Render pipeline | Universal Render Pipeline **17.3.0** |
| Input | Unity Input System **1.18.0** |
| Controls | Keyboard and mouse |
| Available standalone build | **macOS Development build** |

Package versions are recorded in `Packages/manifest.json` and `Packages/packages-lock.json`.

## Setup and play in Unity

1. Install **Unity Hub** and Unity Editor **6000.3.8f1**. Include **Mac Build Support** if you plan to create a macOS build.
2. Clone the repository:

   ```bash
   git clone https://github.com/K-Sothearith/CyberEscape.git
   cd CyberEscape
   ```

3. In Unity Hub, choose **Add project from disk** and select the cloned folder containing `Assets`, `Packages` and `ProjectSettings`.
4. Open the project with the specified Editor version and wait for assets and packages to finish importing.
5. Open **`Assets/Scenes/Voidbound.unity`**.
6. Press **Play**, then select **Initiate Expedition**.
7. Follow the tutorial and controls above. Press **Esc** to pause or restart the run.

Open the existing scene directly; scene-generation tools are not required to play. Unity generates its local cache folders on first import.

## Playable build

**[Download VoidBound for macOS](BUILD_DOWNLOAD_URL)**

> **Placeholder:** Replace `BUILD_DOWNLOAD_URL` with the public download link to the packaged macOS build, such as a GitHub Release or shared download. No public download has been linked yet.

1. Download and extract the complete build archive.
2. Open **`VoidBound.app`** on macOS.
3. Select **Initiate Expedition** and play using a keyboard and mouse.

The locally generated development build is located at `Builds/PassB/VoidBound.app`. Build files are ignored by Git and are distributed separately. A Windows build is not supplied.

## Gameplay video

**[Watch the gameplay recording on YouTube](https://www.youtube.com/watch?v=VIDEO_ID)**

> **Placeholder:** Replace `VIDEO_ID` with the ID of the uploaded gameplay recording.

## Team members and contributions

| Team member | Role / responsibilities | Contributions |
|---|---|---|
| Kong Sothearith | Project Leader | [Sections, mechanics, art, testing or integration contributed] |
| Kov Cheaching | Scene Maker | [Sections, mechanics, art, testing or integration contributed] |
| Sereywath Rathanak | Scene Maker | [Sections, mechanics, art, testing or integration contributed] |
| Sakal Munyreach | Scene Maker | [Sections, mechanics, art, testing or integration contributed] |

## Asset credits

| Asset | Creator / source | Use in VoidBound |
|---|---|---|
| **LED Light Blocks** | **Tomerinio**, Unity Asset Store ([store reference](https://assetstore.unity.com/popular-assets/lighting-assets)) | LED accents and decorative elements, especially in Section 2; imported under `Assets/Tomerinio/LEDLightBlocks` |

Third-party asset ownership remains with its creator.

## Tutorial references and scripting assistance

The primary scripting assistance came from **Antigravity** and **OpenAI ChatGPT / Codex**.

| Reference / assistant | Contribution |
|---|---|
| **Antigravity** | Initial C# gameplay implementation, section mechanics, integration and automated validation assistance |
| **OpenAI ChatGPT / Codex** | Planning and documentation; independent QA; collision, respawn, platform and restart repairs; visual/UI polish; regression testing and build verification |

The team reviewed, integrated and manually tested the game. Team members remain responsible for understanding and explaining the submitted implementation.

## Project status

All seven sections are implemented. Final validation and testing are conducted. A successful macOS Development build. The project owner subsequently confirmed successful personal manual testing and accepted the result.
