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

### Godot game

A separate Godot game for **1–4 local human players** reuses `RacePoker.Core.GameEngine` for dealing,
card picks, payouts, round progression, and the finish rule. Build a five-card
hand, watch the horse gallop and advance by your score, then continue through a ten-round
race. Click cards or press **1–5 / F1–F5**; **Enter** continues, **P** pauses,
**M** toggles sound, and **F11** toggles fullscreen. Losing focus pauses the game.

Choose the lineup on **Set up a race** at launch or through **New race**.
Single player always includes the computer. With two or three humans, **Add
computer** is optional. Four humans fill all four places, so the computer is
disabled. Everyone takes turns using the same screen and controls: the computer
first when added, followed by Player 1, Player 2, and so on. Horse lanes, turn
labels, recaps, results, and saved history adapt to the lineup. Your last lineup
is remembered.

Choose **Computer difficulty** directly on the setup screen before starting a
race. It is enabled when a computer is included and disabled for human-only
lineups. Starting saves the choice and keeps the Settings difficulty in sync;
closing setup without starting leaves your saved difficulty unchanged.

The horse uses an eight-pose `AnimatedSprite2D` gallop with articulated legs and
rider movement, a faster scoring burst, dust particles, and scrolling turf.
Cards lift on hover, deal in sequence, and fly into your hand when selected.
Scoring adds a floating payout, animated score counter, gold horse shader flash,
speed streaks, and a faster turf scroll. Native Godot audio cues respond to card
picks and payouts; a confetti burst and fanfare mark the end of the race.
Pausing freezes the gallop, particles, scenery, card animations, audio, and score
movement together. Starting a new race clears any animations in progress.

Add a blue-tinted computer to the race. All runners use the same
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

Animated banners announce each computer and player turn. Your picking timer shows
seconds remaining and turns red in the last five seconds. On the fifth pick,
hover a choice to preview its resulting hand and exact core payout. Press **G**
or click **Payouts** for the hand chart; the guide pauses the race and preserves
its previous pause state when closed with **G**, **Esc**, or the Back button.

Open **Settings** in the header or press **S** to toggle **Sound effects** and
**Show last-card hints**. Disabling hints removes both the suggested-hand note
and choice tooltips. Both preferences save between sessions in Godot's
`user://preferences.cfg`; **M** also updates the saved sound preference. Settings
pauses the race and preserves its previous pause state when closed with **S**,
**Esc**, or the Back button.

Rounds advance automatically after **6 seconds** by default. Change **Next-round
countdown** in Settings (1–10 seconds); the preference saves between sessions and
applies immediately. The between-round timer shows the time remaining, and
**Enter / Next round** skips the wait. The final round counts down to results
instead. Pausing or opening a dialog freezes the countdown.

Completed hands outline the scoring cards in gold and dim unused kickers. Pairs
highlight only their matching cards; straights, flushes, and full houses use all
five. High-card highlights follow the configured highest-rank or rank-sum rule.
During the round break, a recap replaces the choices with every hand, their actual
payouts, and who gained points on the other runner. It also calls out lead changes
and ties. The six-second countdown and Enter-to-continue still work from the recap.

Install **Godot 4.5.1 .NET** and the **.NET 8 SDK**. Open
`src/RacePoker.Godot/project.godot`, build, and press **F6** or **F5**. Alternatively:

```powershell
./scripts/Start-Godot.ps1 -GodotPath 'C:\path\to\Godot_v4.5.1-stable_mono_win64_console.exe'
# Uses the portable editor in artifacts/godot-tools automatically, when present:
./scripts/Start-Godot.ps1
# Run two rounds against the rival, including pause, input, and restart checks:
./scripts/Start-Godot.ps1 -SmokeTest
```

The Godot game is separate from the WPF solution and release workflow. Its
assets are copied from the existing game, and it never writes WPF settings or
leaderboards. The shared core targets .NET 8 and .NET 10 when built with a modern
SDK; the pinned .NET 8 SDK builds only its .NET 8 target for Godot. WPF continues
to target .NET 10 and uses the core's .NET 10 target. The Godot folder pins the .NET 8 SDK
to avoid editor tooling conflicts with newer SDKs. The Godot game has no
online multiplayer or leaderboard UI.

### Computer difficulty and race history

In **Settings → Computer → Difficulty**, choose **Easy** (random card picks),
**Normal** (matching ranks and suits), or **Hard** (samples possible future hands
to compare expected payouts). Hard uses only its visible cards and a standard
unknown deck. It does not read your cards or its hidden deck. Difficulty is saved
and applies when you start the next race; the computer's header shows the current
race's level.

Open **History** in the footer or press **H** for lifetime completed-race count,
wins, draws, best player score, and the 20 most recent results with lineup and
scores. Statistics refer to you in solo and Player 1 in local multiplayer; winning
requires beating every other racer. All racers' scores are saved and shown in each
history row's tooltip. Draws are separate from wins. Restarted or abandoned races do not
count. Results save to Godot's `user://race-history.json`, with a previous-save
backup and a retry button if saving fails. The History dialog pauses the race
and restores its previous pause state when closed.

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

Push a version tag after committing your changes:

```powershell
git tag v1.0.1
git push origin v1.0.1
```

The [Release action](.github/workflows/release.yml) tests and builds both editions,
then creates or updates the GitHub release with `RacePoker.Wpf.exe` and
`RacePoker-<version>-Windows-x64.zip`. Extract the Godot ZIP and run `RacePoker.exe`;
its runtime is included. Tags set both editions' versions, including prereleases.
Rerun an existing tag through **Actions → Release → Run workflow**.
