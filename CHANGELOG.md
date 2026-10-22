# Changelog

## 0.5.0

- Lock more than one reward. `MaxLocked` sets how many; locked rewards stay through a refresh and are still there the next day. The footer shows how many locks are in use.
- A 「Not interested」 button on each card swaps it for another reward and keeps it away for the rest of the day.
- A 「Ban」 button on each card swaps it out and keeps it away for the rest of the run. `BanCost` sets its price in survival points, and `BanWholeLine` bans every level of the same reward.
- Each card says whether its reward is permanent or one-time (`Labels`).
- `NoRepeatOnRefresh` makes a refresh show rewards you have not seen yet that day.
- The footer shows the interest you will get tomorrow.
- `CashOut` turns the survival points left when you leave the rewards screen into cash.
- An optional shop (`ShopEnabled`, off by default): search every reward you can get, sorted by category, and buy it for survival points. Shop prices are above card prices (`ShopMarkup`) and go up with each purchase (`ShopIncrease`) until they reset (`ShopReset`). Rewards you cannot buy now are greyed out with the reason, or hidden (`ShopUnavailable`). `ShopPerDay` limits purchases a day, and `ShopLocked` lets you buy a reward before the one it needs, at double the price.
- With Exiled's Toolkit, every new setting can be changed on the Mods screen without a restart.
- Made for Survival Log 1.1.19131.

## 0.4.2

- After a game update, a hook that can no longer be placed is logged as an error, so the Mod Manager can mark the mod as maybe needing an update.
- Made for Survival Log 1.1.19131.

## 0.4.1

- Works with Survival Log 1.1.19131. That game update added card pinning and the extra rewards stopped showing; they show again, and a pinned card stays among the choices.
- The higher interest cap (MaxInterest) works again; since that game update the game's cap of 10 applied.

## 0.4.0

- The Mods screen links this changelog.
- On the Mods screen, settings that only matter while another setting is on stay hidden until it is.
- OptionCount, RefreshCost and InterestRate can be changed on the Mod Manager's Mods screen without a restart. Enabled and MaxInterest take effect at the next game start.
- The Mods screen shows the mod's name, description, category and links.
- Exiled's Toolkit 0.4.0 or newer is optional. With it, settings changed on the Mods screen take effect without a restart; without it, at the next game start.

## 0.3.0

First public release. Choose from more rewards at the end of the day, up to 20 instead of 3, with a refresh cost that scales to match, and earn 10% daily interest on your survival points instead of 5%.
