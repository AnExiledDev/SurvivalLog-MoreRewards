# More Rewards settings

Settings live in `BepInEx\config\com.dev.survivallog.morerewards.cfg`, created the first time the game starts with the mod. Change them in game with the 「Settings」 button on the rewards screen or on the Mod Manager's Mods screen, or edit the file while the game is closed. Changed in game, every setting except Enabled takes effect at once; Enabled takes effect at the next game start. Delete the file to get the defaults back. The file describes each setting in English and Simplified Chinese. Without Exiled's Toolkit installed, changes made on the Mods screen take effect at the next game start instead; changes made on the rewards screen still take effect at once.

With the defaults you get the game's 3 rewards and its own refresh cost, the interest rate on your survival points goes from 5% to 10%, and the most interest they earn in a day goes from 10 to 20. Raise `OptionCount` to get more rewards to choose from. Set `InterestRate` and `MaxInterest` to `0` to keep the game's interest as it is. The 「Ban」 button and the labels are on by default; the shop is off until you turn it on.

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
| `InterestRate` | `10` | The interest rate, in percent, on the survival points you hold when a day ends. `0` keeps the game's rate of 5%, and any number from 1 to 100 sets a new rate. The interest still stops at `MaxInterest`. |
| `MaxInterest` | `20` | The most interest your survival points can earn when a day ends. `0` keeps the game's limit of 10, and any number from 1 to 100 sets a new limit. |

### How interest works

When a day ends, the game pays interest on the survival points you hold: the points times the interest rate, rounded down, at least 1 while you hold any points, and at most the limit. The game's rate is 5% and its limit is 10; `InterestRate` and `MaxInterest` change them. `InterestRate` is the whole rate for every character: it replaces the game's 5% and is not added to it. The interest shows on the end-of-day page as its own card.

| Points held | Game (5%, limit 10) | `InterestRate` 10, `MaxInterest` 0 | Defaults (10%, limit 20) |
| --- | --- | --- | --- |
| 50 | 2 | 5 | 5 |
| 100 | 5 | 10 | 10 |
| 150 | 7 | 10 | 15 |
| 200 or more | 10 | 10 | 20 |

So with the defaults, holding more than 200 points earns no more interest. Raise `MaxInterest` to let it grow further.

The Savings Plan reward does not change the interest rate. It pays its own bonus on the same card: on a day you earned survival points, 5% of them, rounded down, at least 1 and at most 10. Its limit of 10 is the reward's own, and `MaxInterest` does not change it.

The tip on the end-of-day page still says the rate is 5% and the limit is 10. The interest you are paid follows `InterestRate` and `MaxInterest`.

The mod sets the rate a moment after a game is loaded, long before the day ends. Your save keeps the game's own rate, so without the mod the interest is back to 5%.

## Locks

| Setting | Default | What it does |
| --- | --- | --- |
| `MaxLocked` | `1` | How many rewards you can lock at once. Locked rewards stay on the screen after a refresh and are still there the next day. `1` is the game's own number; it can go up to 1 less than `OptionCount`, so a refresh always has a card to change. |

With `MaxLocked` above 1, the footer shows how many locks are in use (for example 2/3 next to a lock), and the lock button on a card adds or removes a lock. When every lock is in use, unlock a card first.

## Cards

| Setting | Default | What it does |
| --- | --- | --- |
| `NoRepeatOnRefresh` | `false` | `true` makes a refresh show rewards you have not seen yet that day, while the day's groups still have others to give. |
| `BanButton` | `true` | A 「Ban」 button on each card. A banned reward is swapped for another and never comes back for the rest of the run. |
| `BanCost` | `2` | How many survival points a ban costs, 0 to 99. `0` makes it free. |
| `BanWholeLine` | `false` | `true` makes a ban also ban every level of the same reward. |
| `UnbanRefund` | `50` | How much of what a ban cost, in percent, comes back when you remove it from the 「Bans」 list, 0 to 100. |
| `Labels` | `true` | Shows on each card whether its reward is permanent, one-time, or lasts for a time (for example 3 DAYS). |
| `CashOut` | `0` | Cash for each survival point you still hold when you leave the rewards screen, 0 to 1000; the points are spent. `0` is off and keeps your points. |

The buttons show only on cards you can still act on: not bought and not locked. The footer also shows the interest you will get tomorrow, and with `CashOut` on, how much cash leaving the screen gives. With `BanButton` on, a 「Bans」 button in the footer lists this run's bans; removing one lets its rewards come back. A ban made with `BanWholeLine` comes off as one, and its refund is a share of what it cost when it was made.

## Shop

| Setting | Default | What it does |
| --- | --- | --- |
| `ShopEnabled` | `false` | A 「Shop」 button on the rewards screen: search every reward you can get and buy it with survival points. |
| `ShopUnavailable` | `Grey` | What the shop does with a reward you cannot buy now. `Grey` shows it greyed out with the reason; `Hide` leaves it out. |
| `ShopMarkup` | `25` | How much more, in percent, a reward costs in the shop than on a card, 0 to 200. |
| `ShopIncrease` | `15` | How much more, in percent of the card price, each shop purchase adds to every shop price until the prices reset, 0 to 100. |
| `ShopReset` | `Day` | When the increase goes back to the markup alone: `Day`, `Week` (every 7 days) or `Never`. |
| `ShopPerDay` | `2` | How many shop purchases you can make in a day, 0 to 99. `0` is no limit. |
| `ShopLocked` | `true` | `true` lets the shop sell a reward before the reward it needs first, at double the price. |

A shop price is the card price times (100 + `ShopMarkup` + `ShopIncrease` x purchases since the last reset) / 100, rounded up. With the defaults, a reward that costs 8 on a card costs 10 in the shop, then 12 after one purchase, then 13 after two. The shop lists rewards by category, with a search box and a switch to show only what you can buy now. Rewards you cannot buy say why: you own it, it needs another reward first, it is banned this run, not available yet, and so on.

## How many rewards fit

Up to 5 rewards sit side by side in one row. More than 5 are spread over even rows (6 rewards are 2 rows of 3, 10 are 2 rows of 5, 20 are 4 rows of 5), and the cards get smaller so more of them fit. If they still do not all fit on the screen, the reward area scrolls.

There are no keys to set. The buttons on the rewards screen are set with the settings above.
