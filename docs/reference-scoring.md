# Scoring reconstructed from the reference images

## Evidence

The point chart in [IMG_6727.jpeg](../IMG_6727.jpeg) is the source for these payouts. The hand shown beneath it is 2♦, 5♦, 2♣, 5♣, 4♠ and is labeled "2 Pair" in round 9.

| Hand | Chart payout |
| --- | ---: |
| High card | Face Value |
| Any pair | 4,500 |
| Two pair | 6,700 |
| Three of a kind | 9,000 |
| Straight | 12,000 |
| Flush | 15,000 |
| Full house | 18,700 |
| Four of a kind | 22,500 |
| Straight flush | 28,500 |
| Royal flush | 37,500 |

The photo also clearly shows five large card positions across the lower row. The user confirmed that this is the intended layout, matching the recreation. Keep the existing five-slot layout; it is not a defect to reduce to three slots.

The other photos show a game-selection preview and the high-score list, rather than before-and-after hand scores.

## Implemented rule

Evaluate the completed five-card hand using standard poker categories and award its configured point-chart value once. Add that value to the player's cumulative total. The round number and elapsed picking time do not modify the payout. For the photographed two-pair hand, this means +6,700 points in round 1, round 9, or any other round.

This is a conservative reconstruction of the visible chart. The old 15% per-round multiplier had no support in the images and has been removed from the engine, display, and help. The shipped JSON chart and core defaults retain the photographed category values; custom point-table overrides still work.

## Limits of what the images establish

- **High-card formula:** The label "Face Value" does not distinguish highest card, sum of cards, face-card treatment, or scaling. The default remains highest-card rank: 2–10 at their numeric ranks, J=11, Q=12, K=13, A=14. Settings also offer the sum of those five ranks. Both are reconstruction options; neither is a verified cabinet formula.
- **Cumulative totals:** The pictured totals are 38,500 and 37,999. No prior total or complete hand history is supplied, and a still image cannot exclude score animation. Those totals do not establish a hidden bonus, penalty, or high-card formula. We do not invent such adjustments to fit one snapshot.
- **Round and race modifiers:** No round multiplier or speed bonus is displayed. Fixed payouts are the implemented interpretation; the images cannot prove the absence of every hidden modifier.
- **Finish and round limit:** The track has markers through 48,000 and the current round is 9. The images do not establish whether that is the complete distance scale or how the maximum round count is configured. The default remains 150,000 points / 10 rounds. Finish distance and round limit are adjustable in settings. A value such as 48,000 is a selectable experiment, not a verified reference preset.
- **Deck terminology:** Merit's [general game description](https://www.meritgames.com/Support_Center/GAMEDESCRIPTIONS.PDF) mentions choosing cards from three decks. That wording does not establish that the photographed version should have three on-screen card positions. The earlier recommendation to replace the five-slot layout with three slots was incorrect.
- **Win condition:** The same description says the first horse to cross the finish wins, but does not establish exactly when to check for a finish during multiplayer turns. The default checks at the end of the round. The optional first-to-finish rule ends the race after the crossing horse’s movement and skips remaining players’ turns. The public manufacturer description was rechecked on 2026-10-07 and does not resolve the high-card calculation, exact finish distance, or multiplayer check timing.

A high-card hand with its score before and after completion, or a photograph of the cabinet's detailed help, would resolve the remaining scoring uncertainty.

## Leaderboard compatibility and settings

New records include a rules profile covering the fixed-payout scoring version, custom payout values, player count, round limit, finish distance, picking timeout, high-card calculation, and finish-check rule. Scores compete only within matching profiles. Older records without metadata remain available in a separate legacy board; their original scoring cannot be inferred reliably. Each board retains its ten highest scores.

Race rules can be changed from the lobby and stay fixed during a race. Manual advancement waits for Continue with no fallback timeout. Presentation settings can be changed during play, and lobby sway/glow changes take effect on saving settings, including stopping animations when set to zero.

## Input and interruption handling

The five selection slots remain the intended layout. Keyboard repeats are ignored for game actions. Pausing, minimizing, switching applications, or a dispatcher interruption longer than two seconds preserves remaining picking time; returning to the window requires an explicit Resume. Timing uses a monotonic clock instead of wall-clock time.

The results screen shows its idle timeout. Selecting initials pauses that timeout; Done or Enter in the field restarts a full idle period. The on-screen keypad supports letters, digits, backspace, and clear. These are recreation usability improvements and do not establish undocumented cabinet behavior.

Failed settings and score writes remain pending, can be retried manually, retry every five seconds, and retry before closing. A persistent failure keeps the window open until saving succeeds or the player explicitly exits without saving.
