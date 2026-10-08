# Race Poker

A Windows arcade poker race inspired by the cabinet in the reference photos. Choose one to four players, build a five-card poker hand by picking from five face-up cards, and advance your horse with the points you earn.

The updated screens keep the green race track, point chart, hand row, selection row, and local high scores. The track scales with the window and configured finish distance. The countdown shows seconds remaining, round results show every player’s hand and awarded points, and players can save their initials after the race. Horses gallop as they advance, with hoofbeats, hand and result cues, and a brief finish celebration.

## Run

Requires Windows and the .NET 10 SDK for development.

```powershell
dotnet run --project src/RacePoker.Wpf
```

- Lobby: click a player count or press **1–4**. **Space** or **Enter** starts a solo race.
- During your turn: click a card or press **1–5 / F1–F5**. The chosen card enters your hand and is replaced in the selection row.
- **Enter** advances from round results. **P** or the Pause button pauses the race; **P**, **Enter**, **Space**, or Resume continues it. **F11** toggles fullscreen. **Esc** returns to the menu.
- Holding a game shortcut does not repeat its action. Unavailable player counts are disabled, and player-number keys do nothing on results screens.
- Help and settings pause the game timer while open. Minimizing or switching away also pauses play and waits for Resume when you return. Timers use a monotonic clock; an interruption longer than two seconds pauses the race without charging that gap as picking time.
- Results show a visible idle countdown. Tap a player’s initials to pause it, then use the physical keyboard or the on-screen letter/number keypad. The first keypad character replaces the default initials. Backspace and Clear are available; Done or Enter in the field finishes editing and gives you a fresh countdown.

Default rules are 10 rounds and a 150,000-point finish. Settings let you type an exact finish distance or adjust it in 1,000-point steps, change the round limit, disable the picking timer, and select manual or automatic round advancement. Invalid distance entries show an error and cannot be applied. Sliders, toggles, and action buttons have larger touch targets. With auto-advance off, results wait for Continue indefinitely. Each completed hand awards the fixed points shown in the photo’s point chart, with no round multiplier or time bonus. A race ends after all players finish the round in which a horse crosses the finish line, or after the last round. The highest total score wins; equal totals share the place. An optional first-to-finish rule ends the race after the crossing horse’s movement, skipping remaining turns. Race rules are locked during play; presentation settings remain available.

The exact payouts, photo evidence, and remaining uncertainties are recorded in [the scoring notes](docs/reference-scoring.md). “Face Value” defaults to the highest card’s rank (J=11, Q=12, K=13, A=14). An optional sum-of-ranks rule is available. Neither high-card formula, the original finish distance, nor the timing of the cabinet’s multiplayer finish check is verified by the photos.

Settings and high scores are stored under `%APPDATA%\RacePoker`. High scores are saved when leaving the results screen, when its timer expires, or when closing the game on that screen. Each leaderboard contains up to ten records for its scoring version, payouts, player count, rounds, distance, picking time, high-card rule, and finish rule. Use the lobby’s leaderboard selector to view **Current rules**, numbered **Saved rules**, or **Legacy records**. The selected board’s full rules appear beneath the selector. Existing records are preserved without being treated as scores under the new rules.

JSON saves replace files atomically and retain the previous version in `.bak`. If a file is damaged, the game preserves a `.corrupt-…` copy and tries the backup before using defaults. Invalid settings are constrained to supported values. A visible notice reports recovery or a failed save. Failed writes remain pending in memory, expose a Retry save action, and retry automatically every five seconds and before closing. If closing still cannot save, the window stays open with Retry saving and Exit without saving choices. Exiting without saving discards pending changes.

Sounds are synthesized in memory without writing placeholder files at startup. Optional custom sound files under `%APPDATA%\RacePoker\Sounds` use names such as `card_pick.v2.wav`, `hand_complete.v2.wav`, `racing.v2.wav`, `round_end.v2.wav`, `game_win.v2.wav`, and `game_over.v2.wav`.

## Verify

```powershell
dotnet test RacePoker.sln
dotnet run --project tools/VerifyGame
```

The smoke-check tool renders the real WPF views offscreen and checks multiplayer turns, card limits, countdowns, all-player round results, manual advancement, saved initials, results timeout, replay, leaderboard separation, settings locks, live animation changes, sound-file validity, configured finish distance, limited-seat keyboard handling, repeated-key rejection, monotonic timing, interruption pause/resume, initials focus and keypad input, numeric validation, pending-save retries, safe closing, and binding errors. It writes screenshots and isolated test data into `artifacts/previews`. Leaderboard entries in the lobby preview are illustrative fixtures.

## Build a runnable folder

```powershell
dotnet publish src/RacePoker.Wpf -c Release -o artifacts/Release
```

Launch `artifacts/Release/RacePoker.Wpf.exe` on Windows with the .NET 10 Desktop Runtime installed. Keep the other files in the output folder alongside it.
