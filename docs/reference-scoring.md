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

- **High-card formula:** The label "Face Value" does not distinguish highest card, sum of cards, face-card treatment, or scaling. The implementation retains highest-card rank: 2–10 at their numeric ranks, J=11, Q=12, K=13, A=14. This is an explicit assumption, not a verified cabinet formula.
- **Cumulative totals:** The pictured totals are 38,500 and 37,999. No prior total or complete hand history is supplied, and a still image cannot exclude score animation. Those totals do not establish a hidden bonus, penalty, or high-card formula. We do not invent such adjustments to fit one snapshot.
- **Round and race modifiers:** No round multiplier or speed bonus is displayed. Fixed payouts are the implemented interpretation; the images cannot prove the absence of every hidden modifier.
- **Finish and round limit:** The track has markers through 48,000 and the current round is 9. The images do not establish whether that is the complete distance scale or how the maximum round count is configured. Existing race settings (default 150,000 points / 10 rounds) are preserved; this change concerns scoring.
- **Deck terminology:** Merit's [general game description](https://www.meritgames.com/Support_Center/GAMEDESCRIPTIONS.PDF) mentions choosing cards from three decks. That wording does not establish that the photographed version should have three on-screen card positions. The earlier recommendation to replace the five-slot layout with three slots was incorrect.
- **Win condition:** The same description says the first horse to cross the finish wins, but does not establish exactly when to check for a finish during multiplayer turns. The recreation currently checks at the end of the round.

A high-card hand with its score before and after completion, or a photograph of the cabinet's detailed help, would resolve the remaining scoring uncertainty.
