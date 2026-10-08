# Race Poker

A Windows arcade game for **1–4 players**. Pick from five face-up cards to build a five-card poker hand, earn points, and race your horse to the finish.

## Play

**From source:** install the .NET 10 SDK on Windows, then run:

```powershell
dotnet run --project src/RacePoker.Wpf
```

**Built game:** download `RacePoker.Wpf.exe` from [Releases](https://github.com/primetime43/Poker-Race/releases), or build it below. Requires Windows x64 and the **.NET 10 Desktop Runtime (x64)**. The game and its resources are bundled into one executable; the runtime is installed separately.

| Action | Control |
| --- | --- |
| Start a race | Click a player count or press **1–4** |
| Start solo | **Space** or **Enter** in the lobby |
| Pick a card | Click it or press **1–5 / F1–F5** |
| Continue after a round | **Enter** or Continue |
| Pause / resume | **P** or the on-screen button |
| Fullscreen | **F11** |
| Return to menu | **Esc** |

Switching away pauses the race. On the results screen, tap your initials and use the keyboard or touch keypad; **Done** finishes editing.

## Rules and saves

- **Defaults:** 10 rounds, a 150,000-point finish, and 30 seconds to pick. Change these in Settings before starting.
- Hands award the fixed points in the chart. The default race ends after the round in which a horse crosses the finish, or after the final round. Highest total wins; ties share the place.
- Settings and scores save to `%APPDATA%\RacePoker`. Leaderboards keep scores with different rules separate.

See [scoring notes](docs/reference-scoring.md) for the photo-based payouts and cabinet rules that remain unverified.

## Screenshots

![Four-player game with a hand in progress and five card choices](docs/screenshots/race.png)

| Lobby | Results and touch initials |
| --- | --- |
| ![Player selection and leaderboard](docs/screenshots/lobby.png) | ![Race results with the on-screen initials keypad](docs/screenshots/results.png) |

## Development

```powershell
# Tests and UI verification
dotnet test RacePoker.sln
dotnet run --project tools/VerifyGame

# Build one executable (no bundled runtime)
./scripts/Publish-Release.ps1
```

Output: `artifacts/Release/RacePoker.Wpf.exe`. UI verification writes screenshots and isolated test data to `artifacts/previews`.

## Create a release

After committing and pushing your changes, push a version tag:

```powershell
git tag v1.0.0
git push origin v1.0.0
```

The [Release workflow](.github/workflows/release.yml) tests the game, builds the single executable, and attaches it directly to a GitHub release. The tag sets the version shown in the game header. You can also run it from **Actions → Release → Run workflow** with an existing tag. No ZIP is needed.
