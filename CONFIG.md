# More Rewards settings

Settings live in `BepInEx\config\com.dev.survivallog.morerewards.cfg`, created the first time the game starts with the mod. Edit the file while the game is closed; changes apply at the next game start. Delete the file to get the defaults back.

With the defaults the game plays exactly as it does without the mod: 3 rewards and the game's own refresh cost. Raise `OptionCount` to get more rewards to choose from.

## General

| Setting | Default | What it does |
| --- | --- | --- |
| `Enabled` | `true` | Turn the mod on. `false` leaves the end-of-day rewards and the refresh cost as the game has them. |

## Rewards

| Setting | Default | What it does |
| --- | --- | --- |
| `OptionCount` | `3` | How many rewards you can choose from at the end of a day and after each refresh. `3` is the game's own number. Any number from 3 to 20. |
| `RefreshCost` | `0` | How many survival points a refresh costs. `0` is automatic: the cost goes up with `OptionCount` (see below). Any other number from 1 to 99 is the cost of every refresh, whatever `OptionCount` is. |

### The automatic refresh cost

With `RefreshCost` at `0`, a refresh costs the game's own cost with 3 rewards, plus 1 for every 2 rewards more. In numbers: the game's cost, minus 1, plus half of `OptionCount` rounded down.

With the game's usual cost of 2 survival points:

| `OptionCount` | Refresh cost |
| --- | --- |
| 3 | 2 (the game's own) |
| 4 | 3 |
| 5 | 3 |
| 6 | 4 |
| 7 | 4 |
| 8 | 5 |
| 9 | 5 |
| 10 | 6 |
| 20 | 11 |

Halves always round down, so an odd number of rewards costs the same as one fewer.

If you want the game's cost with more rewards, set `RefreshCost` to the game's cost (`2`).

## How many rewards fit

Up to 5 rewards sit side by side in one row. More than 5 are spread over even rows (6 rewards are 2 rows of 3, 10 are 2 rows of 5, 20 are 4 rows of 5), and the cards get smaller so more of them fit. If they still do not all fit on the screen, the reward area scrolls.

There are no keys or buttons to set.
