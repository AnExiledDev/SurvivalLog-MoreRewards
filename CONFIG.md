# More Rewards settings

Settings live in `BepInEx\config\com.dev.survivallog.morerewards.cfg`, created the first time the game starts with the mod. Edit the file while the game is closed; changes apply at the next game start. Delete the file to get the defaults back.

With the defaults you get the game's 3 rewards and its own refresh cost, and the most interest your survival points earn in a day goes from 10 to 20. Raise `OptionCount` to get more rewards to choose from. Set `MaxInterest` to `0` to keep the game's interest as it is.

## General

| Setting | Default | What it does |
| --- | --- | --- |
| `Enabled` | `true` | Turn the mod on or off. `false` leaves the end-of-day rewards, the refresh cost and the interest on survival points as the game has them. |

## Rewards

| Setting | Default | What it does |
| --- | --- | --- |
| `OptionCount` | `3` | How many rewards you can choose from at the end of a day and after each refresh. `3` is the game's own number, and you can set any number from 3 to 20. |
| `RefreshCost` | `-1` | How many survival points a refresh costs. `-1` is automatic: the cost goes up with `OptionCount` (see below). `0` makes every refresh free, and any number from 1 to 99 is the cost of every refresh, whatever `OptionCount` is. |

### The automatic refresh cost

With `RefreshCost` at `-1`, a refresh costs the game's own cost with 3 rewards, going up by 1 at 4, 6, 8 rewards and so on. In numbers: the game's cost, minus 1, plus half of `OptionCount` rounded down.

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

A refresh that finds no new rewards at all costs nothing, whatever `RefreshCost` is.

## Interest

| Setting | Default | What it does |
| --- | --- | --- |
| `MaxInterest` | `20` | The most interest your survival points can earn when a day ends. `0` keeps the game's limit of 10, and any number from 1 to 100 sets a new limit. |

### How interest works

When a day ends, the game pays interest on the survival points you hold: 5% of them at its base rate, rounded down, at most 10, and at least 1 while you hold any points. The interest shows on the end-of-day page as its own card. Some talents and plans raise the 5%; the mod leaves that as it is and changes only the most you can get.

With the base 5%:

| Points held | Game (limit 10) | `MaxInterest` 20 |
| --- | --- | --- |
| 100 | 5 | 5 |
| 200 | 10 | 10 |
| 300 | 10 | 15 |
| 400 or more | 10 | 20 |

The tip on the end-of-day page still says the limit is 10. The interest you are paid follows `MaxInterest`.

## How many rewards fit

Up to 5 rewards sit side by side in one row. More than 5 are spread over even rows (6 rewards are 2 rows of 3, 10 are 2 rows of 5, 20 are 4 rows of 5), and the cards get smaller so more of them fit. If they still do not all fit on the screen, the reward area scrolls.

There are no keys or buttons to set.
