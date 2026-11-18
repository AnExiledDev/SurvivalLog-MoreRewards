# Changelog

## 0.11.0

- Rewards tab: a search box finds rewards by name, description or number, in English or Chinese. Enter opens the first one found, Up and Down move through the list and Esc clears the search.
- Rewards tab: ADD TO A POOL opens a list you can search by pool number or by a reward in it, and it shows what each pool holds. Up and Down move, Enter adds the pool and Esc closes the list.
- Rewards tab: the card shows the level you are editing the way the game shows it, and a reward with levels says that each level is its own reward in the game.
- Rewards tab: the list and the editor fit the window, so nothing hides under the Mod Manager's buttons. A new or copied reward opens with the list scrolled to it, and the editor starts at the top when you pick another reward.
- Rewards tab: the rules and pools are easier to read. The ID boxes say how to fill them, and each pool says how many rewards it holds and which days draw from it.
- Fixed: 75 of the game's rewards were missing from the Rewards tab, and the rewards file got their levels wrong.
- Fixed: the example in the rewards file showed Small Solar Boost as changed in the Rewards tab. The example is now a comment. An older file keeps the old example until you remove the example or delete the file.
- Now needs Exiled's Toolkit 0.26.0 or newer.
- Made for Survival Log 1.1.21271.

## 0.10.2

- Fixed: the Rewards tab never showed up on the mod's page in the Mod Manager, and the log filled with "rewards tab: ...noscript" lines.
- Made for Survival Log 1.1.21271.

## 0.10.1

- The Rewards tab now uses the reward editor in Exiled's Toolkit, the same one New Loop Plus uses. It works as before.
- Now needs Exiled's Toolkit 0.24.0 or newer.
- Made for Survival Log 1.1.21271.

## 0.10.0

- Rewards tab: each reward now has its rules. Pick the first day it can be offered, whether it comes up in any run or only in Endless Mode, and which characters get it. Tick "Can be offered and bought again" to let it come back after you own it, and set how many times its effect stacks.
- Rewards tab: remove a game reward's top levels with the x on the last level, and those levels are never offered. + LEVEL puts them back.
- Rewards tab: the new DAY DRAW view shows each day's pools, in the order the game draws from them, and how many rewards each one adds. Change the amounts, add or remove pools, move them up or down, click a pool to see what is in it and how likely each reward is, and click DRAW for a test draw.
- Rewards file: new keys "fromDay" and "layers" on a reward, and "IsReget", "EndlessMode" and "PlayerSelectId" in its "fields". A new "days" list changes the day table. A game reward whose "levels" list is shorter than the game's switches off the levels above it.
- Made for Survival Log 1.1.21271.

## 0.9.0

- Rewards tab: with the Mod Manager 0.7.0 or newer, More Rewards' page gets a Rewards tab that edits the rewards file for you. Change what any reward costs, its levels, its numbers, its pools and its text, or build your own. SAVE writes the file and the changes apply at once.
- A reward in the rewards file can now have several levels, and each level can give more than one effect.
- The wind rewards are always there when Wind Power 0.9.3 or newer is installed. The `WindCards` setting is gone.
- The wind rewards now show a wind turbine icon.
- Made for Survival Log 1.1.21271.

## 0.8.0

- Your own rewards: `BepInEx\config\MoreRewards.Rewards.json` adds new end-of-day rewards or changes the game's (cost, name, description, needs, pools and what they give). It is written the first time the game starts, with notes and one example that is switched off, so nothing changes until you edit it.
- Wind rewards for Wind Power 0.9.3 or newer: Wind Collection I, II and III, built like Sunlight Collection, each giving wind turbines 15% more power. Turn them on with `WindCards`, which is off by default.
- Made for Survival Log 1.1.21271.

## 0.7.9

- Leaving a number box in the settings blank no longer sets it to the lowest value. A blank box, or one with no number in it, now goes back to what the setting is, and a number past the limit shows the limit instead.
- Made for Survival Log 1.1.20719.

## 0.7.8

- Clearer wording in the settings, the shop and the rewards screen footer.
- Made for Survival Log 1.1.20719.

## 0.7.7

- The interest cap now only looks for the two numbers it changes in the code that pays interest when a day ends, so after a game update it can't change some other part of the game by mistake.
- Made for Survival Log 1.1.20719.

## 0.7.6

- Now needs Exiled's Toolkit 0.20.0 or newer.
- Made for Survival Log 1.1.20719.

## 0.7.5

- Now uses Exiled's Toolkit for its shared parts, so it needs Exiled's Toolkit 0.19.0 or newer.
- Made for Survival Log 1.1.20719.

## 0.7.4

- `InterestRate` and `MaxInterest` now default to 0, which keeps the game's rate of 5% and limit of 10. The mod no longer changes interest until you set them. A value you already saved is kept.
- Made for Survival Log 1.1.20719.

## 0.7.3

- Fixed: without Exiled's Toolkit, turning `Enabled` off from the rewards screen's Settings left the interest line and a Settings button that did nothing on the rewards screen, that day and on later days.
- Made for Survival Log 1.1.20719.

## 0.7.2

- MaxInterest keeps working after a game update that only moves the game's code around, so most updates no longer break it.
- When a game update does break MaxInterest, Mod Manager now shows More Rewards in red, and the log says why.
- Made for Survival Log 1.1.20719.

## 0.7.1

- Fixed: MaxInterest did nothing on Survival Log 1.1.20719, so interest stopped at the game's 10 survival points a day whatever the setting.
- Made for Survival Log 1.1.20719.

## 0.7.0

- Each world can keep its own reward settings: `OptionCount`, `RefreshCost`, `InterestRate`, `MaxInterest`, `MaxLocked`, `NoRepeatOnRefresh`, `BanButton`, `BanCost`, `BanWholeLine`, `UnbanRefund`, `CashOut`, `ShopEnabled`, `ShopMarkup`, `ShopIncrease`, `ShopReset`, `ShopPerDay` and `ShopLocked`. With Exiled's Toolkit 0.17.0 or newer, pick them for a new world in Customizable Difficulty, or change them for the world you are playing on the Mod Manager's Mods screen. `Enabled`, `Labels`, `ShopUnavailable` and `UiScale` stay the same for every world.
- `Enabled` now takes effect at once. Turning it off brings back the game's rewards, refresh cost and interest, and takes the mod's buttons, labels and lock count off the rewards screen. Turning it on again puts them back.
- Made for Survival Log 1.1.20151.

## 0.6.3

- Fixed: MaxInterest did nothing on Survival Log 1.1.20151, so interest stopped at the game's 10 survival points a day whatever the setting.
- Made for Survival Log 1.1.20151.

## 0.6.2

- Fixed: typing into the shop search box and the number boxes in the settings did nothing.
- Fixed: the shop's close button was drawn as a plain box.
- Made for Survival Log 1.1.20151.

## 0.6.1

- Fixed: on Survival Log 1.1.20151 the shop could not list or sell rewards.
- Made for Survival Log 1.1.20151.

## 0.6.0

- New: `UiScale` under Interface sets a size for More Rewards's windows alone, from 75% to 200%, whatever Exiled's Toolkit's UI size is. At 0, the default, they follow the Toolkit's. Needs Exiled's Toolkit 0.11.0 or newer.
- Made for Survival Log 1.1.19611.

## 0.5.3

- The shop, the ban list, the settings and the pop-up messages can be drawn bigger or smaller with the UI size setting of Exiled's Toolkit 0.10.0 or newer. A change shows at once, and each window only grows as far as it still fits on the screen.
- The buttons and labels on the cards and in the footer keep the game's size, like the game's buttons around them.
- Made for Survival Log 1.1.19611.

## 0.5.2

- Fixed: a short stutter when the rewards screen or the shop opened.
- More Rewards now does much less work while you play, so the game runs smoother with it on.
- Made for Survival Log 1.1.19611.

## 0.5.1

- A 「Bans」 button in the footer lists this run's bans. Removing one lets its rewards be offered again and gives back part of what the ban cost (`UnbanRefund`, 50% by default). A ban made with `BanWholeLine` comes off as one.
- A 「Settings」 button in the footer changes every More Rewards setting right on the rewards screen. Every setting except `Enabled` takes effect at once, with or without Exiled's Toolkit.
- `MaxInterest` works again on Survival Log 1.1.19611. Since that update the game's cap of 10 applied whatever it was set to. It now also takes effect at once instead of at the next game start.
- The 「Not interested」 button and its `NotInterested` setting are gone. Use 「Ban」 instead, and remove the ban from the list when you want the reward back.
- Card labels now read how long each reward lasts from the game's data: 「PERMANENT」, 「ONE-TIME」, or how long it lasts, such as 「3 DAYS」. Some labels were wrong before.
- The shop can be filtered by how long a reward lasts: 「Permanent」, 「One-time」 or 「Temporary」, next to the categories.
- The footer's 「Interest tomorrow」 line no longer gets cut off.
- The shop's defaults are now 2 purchases a day (`ShopPerDay`) and rewards can be bought before the one they need (`ShopLocked`). A setting you already saved keeps its value.
- Made for Survival Log 1.1.19611.

## 0.5.0

- Lock more than one reward. `MaxLocked` sets how many. Locked rewards stay through a refresh and are still there the next day. The footer shows how many locks are in use.
- A 「Not interested」 button on each card swaps it for another reward and keeps it away for the rest of the day.
- A 「Ban」 button on each card swaps it out and keeps it away for the rest of the run. `BanCost` sets its price in survival points, and `BanWholeLine` bans every level of the same reward.
- Each card says whether its reward is permanent or one-time (`Labels`).
- `NoRepeatOnRefresh` makes a refresh show rewards you have not seen yet that day.
- The footer shows the interest you will get tomorrow.
- `CashOut` turns the survival points left when you leave the rewards screen into cash.
- An optional shop (`ShopEnabled`, off by default): search every reward you can get, sorted by category, and buy it for survival points. Shop prices are above card prices (`ShopMarkup`) and go up with each purchase (`ShopIncrease`) until they reset (`ShopReset`). Rewards you cannot buy now are greyed out with the reason, or hidden (`ShopUnavailable`). `ShopPerDay` limits how many purchases you can make a day, and `ShopLocked` lets you buy a reward before the one it needs, at double the price.
- With Exiled's Toolkit, every new setting can be changed on the Mods screen without a restart.
- Made for Survival Log 1.1.19131.

## 0.4.2

- After a game update, a hook that can no longer be placed is logged as an error, so the Mod Manager can mark the mod as maybe needing an update.
- Made for Survival Log 1.1.19131.

## 0.4.1

- Works with Survival Log 1.1.19131. That game update added card pinning and the extra rewards stopped showing. They show again, and a pinned card stays among the choices.
- The higher interest cap (MaxInterest) works again. Since that game update the game's cap of 10 had applied.

## 0.4.0

- The Mods screen links this changelog.
- On the Mods screen, settings that only matter while another setting is on stay hidden until it is.
- OptionCount, RefreshCost and InterestRate can be changed on the Mod Manager's Mods screen without a restart. Enabled and MaxInterest take effect at the next game start.
- The Mods screen shows the mod's name, description, category and links.
- Exiled's Toolkit 0.4.0 or newer is optional. With it, settings changed on the Mods screen take effect without a restart. Without it, they take effect at the next game start.

## 0.3.0

First public release. Choose from more rewards when each day ends, up to 20 instead of 3, with a refresh cost that scales to match, and earn 10% daily interest on your survival points instead of 5%.
