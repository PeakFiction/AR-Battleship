#### Note: This submission uses the OPTIONAL Final Team Project submission for build `AR-Battleshipv1.0.apk`.

# AR Battleship – Core Library

## Overview

This is the pure C# domain and application logic for **AR Battleship**, structured following Domain-Driven Design (DDD) principles.  It is compiled as a `.NET Standard 2.1` library and referenced by both the Unity project and the test suite.  It has **no Unity dependencies** — all game logic can be tested with plain NUnit.

---

## How to Build and Run

### Prerequisites

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | 8.0+ | Used by the test runner |
| NUnit | 3.x (via NuGet) | Test framework |
| NUnit3TestAdapter | 4.x | Visual Studio / dotnet test integration |

### Build

```bash
# From the repository root
dotnet build Core/Domain
dotnet build Core/Application
```

### Run Tests

```bash
# From the repository root
dotnet test Core/Tests/Domain.Tests
dotnet test Core/Tests/Application.Tests
```

All 212 domain tests and 45 application tests should pass.

### Use from Unity

The compiled `Domain.dll` and `Application.dll` are placed in
`Application/bin/Debug/netstandard2.1/` and
`Domain/bin/Debug/netstandard2.1/` respectively.

---

## Project Structure

```
Core/
├── Domain/                        Domain layer (no dependencies)
│   ├── BattleshipGame.cs          Root aggregate — owns both boards, phase, turn
│   ├── Cell.cs                    Single cell entity (occupancy + shot state)
│   ├── ComputerEnemy/
│   │   ├── IEnemyStrategy.cs      AI firing strategy contract
│   │   └── HuntTargetStrategy.cs  Hunt/Target medium-difficulty AI
│   ├── Entities/
│   │   ├── Board.cs               10×10 grid — placement rules, shot processing
│   │   └── Ship.cs                Ship entity — hits, sunk detection, factory methods
│   ├── Enums/
│   │   ├── GamePhase.cs           Setup → InProgress → Finished
│   │   ├── Orientation.cs         Horizontal / Vertical + GetOffset extension
│   │   └── ShotResult.cs          Miss / Hit / Sunk (domain level)
│   ├── Results/
│   │   ├── FireResult.cs          Immutable shot outcome record (Miss/Hit/Sunk factories)
│   │   └── Result.cs              Generic domain result: Success(T) or Failure(string)
│   └── ValueObjects/
│       ├── Coordinate.cs          Readonly struct — (column, row) board address
│       ├── PlayerId.cs            Record — PlayerOne / PlayerTwo factory properties
│       └── ShipId.cs              Readonly struct — Guid-backed unique ship ID
│
├── Application/                   Application layer (depends on Domain)
│   ├── Commands/
│   │   ├── FireShotCommand.cs     Who fires and where
│   │   └── ShipPlacementCommand.cs  Who places what ship and where
│   ├── Common/
│   │   ├── GameError.cs           Legacy error enum (use GameErrorCode in new code)
│   │   └── Result.cs              Generic Result<TValue,TError> + GameResult<T>
│   ├── Enums/
│   │   ├── CellViewState.cs       Unknown/Empty/Ship/Hit/Miss/Sunk (UI projection)
│   │   ├── GameErrorCode.cs       Typed failure codes (NotPlayersTurn, ShipOverlap, …)
│   │   └── ShotOutcome.cs         None/Miss/Hit/Sunk (application / network level)
│   ├── Events/
│   │   ├── IGameEvent.cs          Marker interface for all events
│   │   ├── AnnouncementEvent.cs   Human-readable battle log message
│   │   ├── GameEndedEvent.cs      Game over — winner recorded
│   │   ├── ShipPlacedEvent.cs     Ship confirmed placed
│   │   ├── ShipSunkEvent.cs       Ship confirmed sunk
│   │   ├── ShotFiredEvent.cs      Shot result with full hit metadata
│   │   └── TurnChangeEvent.cs     New current-turn player
│   ├── Services/
│   │   ├── IBattleshipGameService.cs   Service contract
│   │   ├── BattleshipGameService.cs    Main service implementation
│   │   ├── EnemyShipPlacementService.cs  Randomly places AI fleet
│   │   └── EnemyTurnService.cs     Executes one AI turn via HuntTargetStrategy
│   └── Snapshots/
│       ├── CellView.cs            Single-cell projection for the UI
│       ├── GameSnapshot.cs        Full board state snapshot (both players)
│       ├── PlayerSnapshot.cs      One player's board cells + ships
│       └── ShipView.cs            Ship metadata projection
│
└── Tests/
    ├── Application.Tests/         NUnit tests for application services
    │   ├── BattleshipGameServiceTests.cs
    │   ├── EnemyShipPlacementServiceTests.cs
    │   └── EnemyTurnServiceTests.cs
    └── Domain.Tests/              NUnit tests for domain entities and value objects
        ├── BattleshipGameTests.cs
        ├── BoardTests.cs
        ├── CellTests.cs
        ├── CoordinateTests.cs
        ├── FireResultTests.cs
        ├── HuntTargetStrategyTests.cs
        ├── Orientation.cs
        ├── PlayerIdTests.cs
        ├── ResultTests.cs
        ├── ShipIdTests.cs
        ├── ShipTests.cs
        └── ShotResultTests.cs
```

---

## Architecture

```
Unity UI / Multiplayer
        │
        │  commands (FireShotCommand, ShipPlacementCommand)
        │  queries  (GameSnapshot)
        ▼
Application Layer (BattleshipGameService)
        │  validates phase, turn; maps errors; produces events
        ▼
Domain Layer (BattleshipGame, Board, Ship, Cell, …)
        │  enforces invariants; no external dependencies
        ▼
Domain Value Objects (Coordinate, PlayerId, ShipId)
```

All dependencies point inward.  The domain layer knows nothing about Unity,
networking, or the application layer.

---

## Design Patterns

| Pattern | Where used |
|---|---|
| Result monad | `Domain.Result<T>`, `Application.GameResult<T>` — no exceptions for flow control |
| Factory Method | `Ship.CreateCarrier`, `Ship.CreateFromType`, `PlayerId.PlayerOne` |
| Command | `FireShotCommand`, `ShipPlacementCommand` — immutable request objects |
| Domain Event | `IGameEvent` and implementations — decoupled UI notifications |
| Snapshot | `GameSnapshot` — read-only board projection avoids exposing domain objects |
| Strategy | `IEnemyStrategy` / `HuntTargetStrategy` — swappable AI difficulty |
| Value Object | `Coordinate`, `ShipId` — equality by value, not reference |

---

## External Libraries and Sources

| Library | Version | License | Purpose |
|---|---|---|---|
| .NET Standard | 2.1 | MIT | Base class library |
| NUnit | 3.x | MIT | Unit testing framework |
| NUnit3TestAdapter | 4.x | MIT | `dotnet test` integration |

No third-party game logic, AI libraries, or datasets are used.

# AR Battleship – Unity Client

## Overview

This README covers the Unity side of **AR Battleship**: the augmented-reality user interface, singleplayer scene flow, multiplayer lobby flow, audio/animation scripts, Android build process, and APK deployment.

The Unity client consumes the separate AR Battleship Core library for domain and application logic. The Core project is intentionally compiled as a `.NET Standard 2.1` library with no Unity dependencies, then referenced by the Unity project through the generated `Domain.dll` and `Application.dll` files.

---

## What the Unity Project Contains

The Unity code is organized around MonoBehaviours that bind the Core Battleship logic to scenes, buttons, AR targets, networking, and visual feedback.

```text
Assets/
└── Scripts/
    ├── AR/
    │   ├── BattleshipAR.cs                AR board interaction using Vuforia targets
    │   └── CameraMirrorFix.cs             Camera/display orientation helper
    │
    ├── Audio/
    │   ├── MusicManager.cs                Persistent music controller
    │   ├── SceneMusicTrigger.cs           Scene-based music switching
    │   └── SFXManager.cs                  Shared sound-effect playback
    │
    ├── Animations/
    │   ├── DefenderDropProjectile.cs      Projectile drop animation
    │   ├── DefenderShipCell.cs            Defender-cell visual marker
    │   ├── MissileDropMissAlt.cs          Miss animation
    │   ├── MissTileMarkerAlt.cs           Miss tile marker
    │   ├── MoveToTarget.cs                Generic movement-to-target animation
    │   └── TileFlash.cs                   Tile highlight feedback
    │
    ├── UI/
    │   ├── GameManager.cs                 Singleplayer game orchestration
    │   ├── SceneLoader.cs                 Scene navigation wrapper
    │   ├── ShipPlacementUI.cs             Ship placement UI
    │   ├── CombatUI.cs                    Singleplayer combat UI
    │   ├── ManualFireUI.cs                Developer/manual shot panel
    │   ├── GameOverUI.cs                  Runtime game-over overlay
    │   ├── ShipStatusUI.cs                Fleet status indicators
    │   ├── SplashScreenController.cs      Splash screen and scene transition flow
    │   ├── IntroVideoController.cs        Intro video playback/skip logic
    │   ├── GuidePageController.cs         Guide page navigation
    │   ├── CreditsScroll.cs               Credits scrolling
    │   └── MultiplayerUI/
    │       ├── CreateLobbyUI.cs           Host lobby UI
    │       ├── JoinLobbyUI.cs             Client lobby UI
    │       ├── MultiplayerPlacementUI.cs  Multiplayer placement UI
    │       ├── MultiplayerCombatUI.cs     Multiplayer combat UI
    │       └── MultiplayerGameOverUI.cs   Multiplayer game-over UI
    │
    └── Networking/
        ├── Battleship/
        │   ├── MultiplayerBattleshipSession.cs      Server-authoritative game session
        │   ├── NetworkBattleshipGameController.cs   Client request controller
        │   ├── NetworkBattleshipEvents.cs           Network event bridge
        │   ├── NetworkPlayerMapper.cs               Network-to-domain player mapping
        │   ├── MultiplayerARInputAdapter.cs         AR input adapter for multiplayer
        │   └── DebugBattleshipInput.cs              Debug input helper
        │
        └── Relay/
            └── RelayManager.cs            Unity Relay host/join flow
```

---

## Prerequisites

Install the following before opening or building the Unity project.

| Tool / Package | Purpose | Source |
|---|---|---|
| Unity Editor | Opens, runs, and builds the Unity client. Use the Unity version originally used by the project if available. | https://unity.com/download |
| Android Build Support | Required to export the Android `.apk`. Install it from Unity Hub together with Android SDK & NDK Tools and OpenJDK. | https://docs.unity3d.com/Manual/android-sdksetup.html |
| AR Battleship Core DLLs | Required game logic assemblies: `Domain.dll` and `Application.dll`. | Built from this repository's Core project. |
| Vuforia Engine | AR image-target / marker tracking used by `BattleshipAR.cs` and `MultiplayerARInputAdapter.cs`. | https://developer.vuforia.com/library/vuforia-engine/getting-started/development-environments/getting-started-vuforia-engine-unity/ |
| TextMeshPro (`com.unity.textmeshpro`) | Text rendering for menus, labels, lobby codes, combat UI, and guide UI. | https://docs.unity3d.com/Packages/com.unity.textmeshpro@latest |
| Netcode for GameObjects (`com.unity.netcode.gameobjects`) | Multiplayer NetworkBehaviour, NetworkManager, host/client flow, and network events. | https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest |
| Unity Transport (`com.unity.transport`) | Network transport used by Netcode and configured for Relay connections. | https://docs.unity3d.com/Packages/com.unity.transport@latest |
| Unity Authentication (`com.unity.services.authentication`) | Anonymous sign-in before creating or joining Relay sessions. | https://docs.unity.com/ugs/manual/authentication/manual/overview |
| Unity Relay (`com.unity.services.relay`) or Multiplayer Services SDK | Relay allocation and join-code based multiplayer connection. | https://docs.unity.com/relay/ |

---

## External Software and Open Source Libraries

The Unity client depends on the following external packages or services.

| Dependency | Used In | Notes |
|---|---|---|
| Unity Engine APIs | All MonoBehaviour scripts | Scene loading, GameObjects, UI, audio, animation, input, serialization, and Android builds. |
| Vuforia Engine | `Scripts/AR/BattleshipAR.cs`, `Scripts/Networking/Battleship/MultiplayerARInputAdapter.cs` | Provides AR target recognition and tracking. Requires a Vuforia license key and configured target database in the Unity project. |
| TextMeshPro | UI scripts including combat, placement, splash, guide, lobby, and multiplayer UI | Import TMP Essential Resources if prompted by Unity. |
| Unity Netcode for GameObjects | Multiplayer scripts under `Scripts/Networking/Battleship/` and multiplayer UI scripts | Provides NetworkBehaviour, NetworkManager, RPC-style networking, and host/client state. |
| Unity Transport | `RelayManager.cs` | Supplies the transport used by Netcode. Relay server data is assigned to the transport before starting host/client. |
| Unity Services Core, Authentication, and Relay | `Scripts/Networking/Relay/RelayManager.cs` | Initializes Unity Gaming Services, signs in anonymously, creates Relay allocations, generates join codes, and joins existing Relay sessions. |
| AR Battleship Core | `GameManager.cs`, combat UI, placement UI, multiplayer networking scripts | Supplies `BattleshipGame`, commands, snapshots, events, `BattleshipGameService`, and domain value objects. |

No third-party AI, game-logic, or dataset-processing libraries are used in the Unity scripts.

---

## Data and Assets the Code Depends On

The scripts do not depend on a downloaded public dataset such as CSV, JSON, image corpus, or ML model data. The project does depend on Unity project assets and service configuration:

| Data / Asset | Required By | Notes |
|---|---|---|
| Vuforia target database / image targets | AR board scripts | Must be configured in the Unity project and linked to the Vuforia scene objects. If the target database is generated in the Vuforia Developer Portal, document the specific target package used in your submission. |
| Vuforia license key | AR camera / Vuforia configuration | Add the license key in the Vuforia configuration inside Unity. Do not commit private keys to a public repository. |
| Unity scenes | `SceneLoader.cs`, splash/menu/gameplay flow | The scene names must match the strings hardcoded in `SceneLoader.cs` and `RelayManager.cs`. |
| UI prefabs and scene references | UI scripts | Buttons, panels, TMP labels, image objects, grids, projectiles, and status indicators are assigned through the Unity Inspector. |
| Audio clips | `MusicManager.cs`, `SceneMusicTrigger.cs`, `SFXManager.cs`, `TapToContinueAudio.cs` | Required for music and UI/game sound effects. |
| Intro video clip | `IntroVideoController.cs` | Optional if the intro scene is used. |
| Core assemblies | Unity gameplay and multiplayer scripts | `Domain.dll` and `Application.dll` must be present in `Assets/Plugins/` or referenced through a local package/project setup. |
| Unity Gaming Services project configuration | Relay multiplayer | Required for Relay and anonymous Authentication. The local Unity project must be linked to a Unity Cloud Project. |

If you use custom AR target artwork, UI artwork, sound effects, music, fonts, or video assets from outside sources, list those assets separately with their licenses and source URLs.

---

## Scene Names

The Unity scripts reference these scenes by name:

```text
0SplashScreen
0MainMenuScreen
1TitleScreen
2LobbyScreen
3LobbySetup
4JoinLobby
5Gameplay
6MultiplayerGameplay
7DifficultySelect
8Guide
8GuidePage2
8GuidePage3
8GuidePage4
8GuidePage5
8GuidePage6
8GuidePage7
8GuidePage8
8GuidePage9
9Credits
```

Make sure every scene used by the final build is added to:

```text
File > Build Settings > Scenes In Build
```

The first scene should normally be `0SplashScreen` or `0MainMenuScreen`, depending on whether the splash flow is included in the release build.

---

## How to Open and Run in Unity Editor

1. Open **Unity Hub**.
2. Select **Open** and choose the Unity project folder.
3. Let Unity import packages and compile scripts.
4. Confirm that the Core DLLs are present in `Assets/Plugins/`.
5. Confirm that Vuforia Engine is installed and the Vuforia license/target database are configured.
6. Confirm that TextMeshPro Essential Resources are imported if Unity prompts for them.
7. Open the start scene, usually:

   ```text
   Assets/Scenes/0SplashScreen.unity
   ```

   or:

   ```text
   Assets/Scenes/0MainMenuScreen.unity
   ```

8. Press **Play** in the Unity Editor.

For singleplayer testing, navigate to the gameplay scene from the menu or open:

```text
5Gameplay
```

For multiplayer testing, open the menu/lobby flow and use:

```text
2LobbyScreen
3LobbySetup
4JoinLobby
6MultiplayerGameplay
```

---

## How to Run Singleplayer

1. Start the game from the splash or main menu scene.
2. Choose the singleplayer path.
3. The game loads the AR gameplay scene:

   ```text
   5Gameplay
   ```

4. Place ships through the placement UI.
5. Start the game.
6. Fire shots through the AR board interaction or the combat UI.
7. The local `GameManager` owns the game session, calls the Core application services, runs the AI turn coroutine, and raises UI events.

---

## How to Run Multiplayer

Multiplayer uses Unity Netcode for GameObjects plus Unity Relay.

### Host

1. Make sure Authentication and Relay are enabled/configured in Unity Gaming Services.
2. Run the project.
3. Navigate to the multiplayer lobby flow.
4. Choose the host/create-lobby path.
5. `RelayManager` initializes Unity Services, signs in anonymously, creates a Relay allocation, starts the Netcode host, and displays a join code.
6. Share the join code with the second player.

### Client

1. Run the same build or Unity project on another device/editor instance.
2. Navigate to the join-lobby screen.
3. Enter the host's join code.
4. `RelayManager` joins the Relay allocation and starts the Netcode client.
5. Once both players are connected, the game loads:

   ```text
   6MultiplayerGameplay
   ```

6. Each player places ships, then the server-authoritative multiplayer session processes turns and broadcasts events.

---

## How to Build the Android APK

1. In Unity, open:

   ```text
   File > Build Settings
   ```

2. Select **Android**.
3. Click **Switch Platform**.
4. Add the required scenes to **Scenes In Build**.
5. Open:

   ```text
   Edit > Project Settings > Player
   ```

6. Configure Android settings:
   - Package name / Application ID.
   - Minimum API level compatible with Vuforia and the target Android devices.
   - Target API level.
   - Orientation settings.
   - Camera permission requirements for AR.
   - Internet permission if multiplayer/Relay is included.
   - Signing settings if producing a release APK.

7. Confirm Vuforia configuration:
   - Vuforia Engine package installed.
   - AR Camera present in AR gameplay scenes.
   - Vuforia license key set.
   - Image target database imported and active.

8. Click **Build**.
9. Choose an output filename, for example:

   ```text
   ARBattleship.apk
   ```

10. Unity exports the `.apk`.

---

## How to Install and Run the APK

### Option 1: Install from Unity

1. Connect an Android device with USB debugging enabled.
2. In **Build Settings**, select **Build And Run**.
3. Unity builds the APK, installs it on the connected device, and launches it.

### Option 2: Install an Existing APK

If an `.apk` has already been generated from Unity:

```bash
adb install -r ARBattleship.apk
```

Then launch the app from the Android device.

For AR gameplay, give the app camera permission when Android prompts for it.

## Troubleshooting

| Problem | Fix |
|---|---|
| Unity scripts cannot find `ARBattleship.Core.*` namespaces | Rebuild the Core project and copy `Domain.dll` and `Application.dll` into `Assets/Plugins/`. |
| TMP text is missing or pink | Import TextMeshPro Essential Resources from Unity's TMP prompt/menu. |
| AR camera does not track the board | Check Vuforia license key, target database activation, AR Camera setup, image target assignment, and Android camera permission. |
| Android build fails | Make sure Android Build Support, SDK/NDK Tools, and OpenJDK are installed through Unity Hub. |
| Multiplayer host/client fails | Confirm Unity Services is initialized, the project is linked to Unity Cloud, Authentication and Relay are enabled, and the NetworkManager has Unity Transport assigned. |
| Client cannot join lobby | Verify the join code or refresh the code, both builds use the same Unity project/service environment, and the host is still running. |
| Scene buttons do nothing or load errors occur | Confirm every named scene is in Build Settings and the scene names match the strings in `SceneLoader.cs` and `RelayManager.cs`. |