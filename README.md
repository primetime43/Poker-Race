# Race Poker

A Windows arcade game for **1–4 players**. Pick from five face-up cards to build a five-card poker hand, earn points, and race your horse to the finish.

## Play

**From source:** install the .NET 10 SDK on Windows, then run:

```powershell
dotnet run --project src/RacePoker.Wpf
```

**Built game:** open `artifacts/Release/RacePoker.Wpf.exe`. Requires the .NET 10 Desktop Runtime; keep the release folder’s files together.

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

# Build the runnable folder
dotnet publish src/RacePoker.Wpf -c Release -o artifacts/Release
```

UI verification writes screenshots and isolated test data to `artifacts/previews`.
