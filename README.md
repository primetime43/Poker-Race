# Race Poker

A Windows arcade poker race inspired by the cabinet in the reference photos. Choose one to four players, build a five-card poker hand by picking from five face-up cards, and advance your horse with the points you earn.

The updated screens keep the green race track, point chart, hand row, selection row, and local high scores. The track scales with the window and configured finish distance. The countdown shows seconds remaining, completed hands stay visible between rounds, and players can save their initials after the race.

## Run

Requires Windows and the .NET 10 SDK for development.

```powershell
dotnet run --project src/RacePoker.Wpf
```

- Lobby: click a player count or press **1–4**. **Space** or **Enter** starts a solo race.
- During your turn: click a card or press **1–5 / F1–F5**. The chosen card enters your hand and is replaced in the selection row.
- **Enter** advances from round results. **F11** toggles fullscreen. **Esc** returns to the menu.
- Help and settings pause the game timer while open.

Default rules are 10 rounds and a 150,000-point finish. The point chart shows base values; each round after the first adds 15% of the base value. A race ends after all players finish the round in which a horse crosses the finish line, or after the last round. The highest total score wins; equal totals share the place.

Settings, high scores, and generated sound tones are stored under `%APPDATA%\RacePoker`. High scores are saved when leaving the results screen, when its timer expires, or when closing the game on that screen.

## Verify

```powershell
dotnet test RacePoker.sln
dotnet run --project tools/VerifyGame
```

The smoke-check tool renders the real WPF views offscreen and checks multiplayer turns, card limits, countdowns, round results, saved initials, results timeout, replay, configured finish distance, and binding errors. It writes screenshots and isolated test data into `artifacts/previews`. Leaderboard entries in the lobby preview are illustrative fixtures.

## Build a runnable folder

```powershell
dotnet publish src/RacePoker.Wpf -c Release -o artifacts/Release
```

Launch `artifacts/Release/RacePoker.Wpf.exe` on Windows with the .NET 10 Desktop Runtime installed. Keep the other files in the output folder alongside it.
