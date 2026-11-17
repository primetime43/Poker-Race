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

### Godot playable prototype

A separate **player vs. computer** Godot screen reuses `RacePoker.Core.GameEngine` for dealing,
card picks, payouts, round progression, and the finish rule. Build a five-card
hand, watch the horse gallop and advance by your score, then continue through a ten-round
race. Click cards or press **1–5 / F1–F5**; **Enter** continues, **P** pauses,
**M** toggles sound, and **F11** toggles fullscreen. Losing focus pauses the game.

The horse uses an eight-pose `AnimatedSprite2D` gallop with articulated legs and
rider movement, a faster scoring burst, dust particles, and scrolling turf.
Cards lift on hover, deal in sequence, and fly into your hand when selected.
Scoring adds a floating payout, animated score counter, gold horse shader flash,
speed streaks, and a faster turf scroll. Native Godot audio cues respond to card
picks and payouts; a confetti burst and fanfare mark the end of the race.
Pausing freezes the gallop, particles, scenery, card animations, audio, and score
movement together. Starting a new race clears any animations in progress.

Race a blue-tinted rival in a second animated lane. Both runners use the same
core rules, separate decks, and the same payouts. The computer goes first each
round, revealing five cards one at a time in the full-size main hand area. Its
completed hand remains visible while its horse advances. The display then switches
to **Your Hand** and deals your five selectable cards. The header retains the
computer's hand result and payout, without miniature cards. Track labels, turn
messages, and results explicitly identify the opponent as the computer. The
rival favors matching ranks and possible flushes, then picks the highest payout
available on its last card. Live standings show the point gap, and results show
the winner or shared first place and the final margin. Finish and round-limit
rules still come from the core. Pause freezes the rival's decisions and movement.

Install **Godot 4.5.1 .NET** and the **.NET 8 SDK**. Open
`src/RacePoker.Godot/project.godot`, build, and press **F6** or **F5**. Alternatively:

```powershell
./scripts/Start-Godot.ps1 -GodotPath 'C:\path\to\Godot_v4.5.1-stable_mono_win64_console.exe'
# Uses the portable editor in artifacts/godot-tools automatically, when present:
./scripts/Start-Godot.ps1
# Run two rounds against the rival, including pause, input, and restart checks:
./scripts/Start-Godot.ps1 -SmokeTest
```

The prototype is separate from the WPF solution and release workflow. Its
assets are copied from the existing game, and it never writes WPF settings or
leaderboards. The shared core targets .NET 8 and .NET 10 when built with a modern
SDK; the pinned .NET 8 SDK builds only its .NET 8 target for Godot. WPF continues
to target .NET 10 and uses the core's .NET 10 target. The Godot folder pins the .NET 8 SDK
to avoid editor tooling conflicts with newer SDKs. This prototype has no
multiplayer, settings screen, or leaderboard UI.

### WPF development

```powershell
# Tests and UI verification
dotnet test RacePoker.sln
dotnet run --project tools/VerifyGame

# Build one executable (no bundled runtime)
./scripts/Publish-Release.ps1
```

Output: `artifacts/Release/RacePoker.Wpf.exe`. UI verification writes screenshots and isolated test data to `artifacts/previews`.

## Create a release

Commit and push your changes first. On GitHub, choose **Releases → Draft a new release**, create a tag such as `v1.0.1`, and publish. The [Release workflow](.github/workflows/release.yml) tests and builds the game, then attaches `RacePoker.Wpf.exe` to **that same release**, preserving your title, notes, and release settings. Reruns replace the EXE. No ZIP is needed.

Alternatively, push a version tag to build and create a release automatically:

```powershell
git tag v1.0.1
git push origin v1.0.1
```

Tags with or without `v` set the game’s displayed version. To rebuild an existing tag, use **Actions → Release → Run workflow**.
