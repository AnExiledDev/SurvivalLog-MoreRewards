# More Rewards settings

Settings live in `BepInEx\config\com.dev.survivallog.morerewards.cfg`, created the first time the game starts with the mod. Change them in game with the 「Settings」 button on the rewards screen or on the Mod Manager's Mods screen, or edit the file while the game is closed. Changed in game, every setting takes effect at once. Delete the file to get the defaults back. The file describes each setting in English and Simplified Chinese.

Each world can keep its own value for every setting in Rewards, Interest, Locks, Cards and Shop except `Labels` and `ShopUnavailable`. Pick them for a new world in Customizable Difficulty, or change them for the world you are playing on the Mods screen. If the world has its own value for a setting, the 「Settings」 button on the rewards screen changes it for that world only. Any other setting changes for every world. `Enabled`, `Labels`, `ShopUnavailable` and `UiScale` are always the same for every world.

With the defaults you get the game's 3 rewards, its refresh cost and its interest, so the mod changes nothing about the game's balance until you change a setting. Raise `OptionCount` to get more rewards to choose from, and set `InterestRate` and `MaxInterest` to earn more interest. The 「Ban」 button and the labels are on by default, and the shop is off until you turn it on.

## General

| Setting | Default | What it does |
| --- | --- | --- |
| `Enabled` | `true` | Turn the mod on or off. `false` leaves the end-of-day rewards, the refresh cost and the interest on survival points as the game has them, and takes the mod's buttons off the rewards screen. |

## Rewards

| Setting | Default | What it does |
| --- | --- | --- |
| `OptionCount` | `3` | How many rewards you can choose from at the end of a day and after each refresh. `3` is the game's number, and you can set any number from 3 to 20. |
| `RefreshCost` | `-1` | How many survival points a refresh costs. `-1` is automatic: the cost goes up with `OptionCount` (see below). `0` makes every refresh free, and any number from 1 to 99 is the cost of every refresh, whatever `OptionCount` is. |
| `ExtraRewards` | `false` | Adds 39 new rewards to the end-of-day cards, and lets 11 rewards the game never offers show up. They are listed below. Change or turn off any of them on the Rewards tab. |

### The extra rewards

With `ExtraRewards` on, these rewards come up on the end-of-day cards. "From" is the first day they can show up. A level above I needs the level before it. A reward that needs another, such as Sunlight Collection IV after level III, only shows up once you have it.

| Reward | Cost | From | What it does |
| --- | --- | --- | --- |
| Day Planner II | 14 | day 20 | Gives you 1 more Survival Point each day. In total: +2. |
| Day Planner III | 22 | day 20 | Gives you 1 more Survival Point each day. In total: +3. |
| Mission Bonus II | 30 | day 20 | Gives you 1 more Survival Point each time you complete a mission. In total: +2. |
| Crisis Bonus II | 36 | day 20 | During a crisis, you get 2 more Survival Points at the end of each day. In total: +5. |
| Morale to Gold II | 30 | day 20 | Morale turns into Survival Points faster at the end of each day (4:1 to 3:1). |
| Know What's What II | 36 | day 24 | Your home food gives 1 Survival Point for every 300 Satiety stored, down from 500. Still up to 3 a day. |
| Pay in Blood | 1 | day 20 | Lose 15 Life and get 4 Survival Points. |
| Careful Budgeting III | 30 | day 20 | Rewards at the end of each day cost another 10% less. |
| Iron Defense IV | 36 | day 60 | Zombies do another 10% less damage to doors, windows and defenses. |
| Deadly Traps IV | 36 | day 60 | Spikes, electric traps, chainsaws and other defenses do another 15% more damage. |
| Diesel Craftsman IV | 36 | day 60 | Diesel generators make another 15% more power. |
| Sunlight Collection IV | 36 | day 60 | Solar panels make another 15% more power. |
| Dexterous IV | 36 | day 60 | Crafting takes another 5% less time. |
| Endurance Drone III | 36 | day 60 | The drone can trade 1 more time a day. In total: +3. |
| Fuel Saver II | 24 | day 24 | Generators use another 15% less fuel. |
| Capacity Master II | 30 | day 24 | Max Power Capacity goes up by another 50%. |
| Cold Resistance II | 56 | day 24 | Plant cold resistance goes up by another 1. |
| Fuel Airdrop | 8 | day 20 | Call for supplies on the radio and get 2 random fuel items the next day. |
| Big Airdrop | 12 | day 20 | Call for supplies on the radio and get 6 random supplies the next day: food, materials or bandages. |
| Care Package | 20 | day 20 | Call for supplies on the radio and get 2 random meds, 2 packs of seeds and 4 crafting materials the next day. |
| Second Wind | 5 | day 20 | Restores 30 Life right away. |
| Master Gardener | 20 | day 20 | Crop pests, weeds and drought are each 10% less likely. |
| Winter Kit | 30 | day 24 | Plant cold resistance +1, each running Air Conditioning or Electric Heater gives +1 temperature, and your Thermal Comfort goes up one level. |
| Night Owl | 25 | day 20 | Movement Speed +30% at night, night traps catch prey 15% more often, and Satiety drops 15% slower while you sleep. |
| Siege Prep | 35 | day 20 | Zombies do 10% less damage to doors, windows and defenses, and defenses do 15% more damage and have 30% more max Durability. |
| Day 30 Airdrop | 0 | day 30 only | You made it to day 30. Get 3 random supplies the next day, free. |
| Day 60 Airdrop | 0 | day 60 only | You made it to day 60. Get 4 random supplies the next day, free. |
| Day 100 Airdrop | 0 | day 100 only | You made it to day 100. Get 5 random supplies the next day, free. |
| Investor I | 15 | day 20 | Interest +2% and the interest limit +5. |
| Investor II | 20 | day 20 | Interest +2% and the interest limit +5. In total: +4% and +10. |
| Investor III | 25 | day 20 | Interest +2% and the interest limit +5. In total: +6% and +15. |
| Wider Net I | 20 | day 20 | Shows 1 more reward card at the end of each day. |
| Wider Net II | 30 | day 20 | Shows 1 more reward card at the end of each day. In total: +2. |
| Wider Net III | 40 | day 20 | Shows 1 more reward card at the end of each day. In total: +3. |
| Pin Board | 20 | day 20 | You can lock 1 more reward card. |
| Second Opinion | 25 | day 20 | Your first refresh each day is free. |
| Ban Tokens | 10 | day 20 | Your next 3 bans are free. |
| Supply Crate | 15 | day 20 | Get 2 Tempered Glass, 4 Broken Glass, 1 Electronic Components and 2 Repair Parts right away. |
| Lottery Ticket | 5 | day 20 | Win 0 to 15 Survival Points at random. |

The day 30, 60 and 100 airdrops take a card on their day, for free.

`ExtraRewards` also puts Scavenger Drone I and II and the 9 drone part blueprints in the draw from about day 20. The game has them but never offers them. They keep the game's costs.

A reward in your rewards file with the same ID replaces the one from the mod. That is what the Rewards tab saves when you change one.

### The automatic refresh cost

With `RefreshCost` at `-1` and 3 rewards, a refresh costs what the game charges. It costs 1 more at 4, 6, 8 rewards and so on. As a formula, it is the game's cost, minus 1, plus half of `OptionCount` rounded down.

With the game's usual cost of 2 survival points:

| `OptionCount` | Refresh cost |
| --- | --- |
| 3 | 2 (the game's) |
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
| `InterestRate` | `0` | The interest rate, in percent, on the survival points you hold when a day ends. `0` keeps the game's rate of 5%, and any number from 1 to 100 sets a new rate. The interest still stops at `MaxInterest`. |
| `MaxInterest` | `0` | The most interest your survival points can earn when a day ends. `0` keeps the game's limit of 10, and any number from 1 to 100 sets a new limit. |

### How interest works

When a day ends, the game pays interest on the survival points you hold: the points times the interest rate, rounded down, at least 1 while you hold any points, and at most the limit. The game's rate is 5% and its limit is 10, and `InterestRate` and `MaxInterest` change them. `InterestRate` is the whole rate for every character. It replaces the game's 5% instead of adding to it. The interest shows on the end-of-day page as its own card.

| Points held | Game and defaults (5%, limit 10) | `InterestRate` 10, `MaxInterest` 0 | `InterestRate` 10, `MaxInterest` 20 |
| --- | --- | --- | --- |
| 50 | 2 | 5 | 5 |
| 100 | 5 | 10 | 10 |
| 150 | 7 | 10 | 15 |
| 200 or more | 10 | 10 | 20 |

So at 10% and a limit of 20, holding more than 200 points earns no more interest. Raise `MaxInterest` to let it grow further.

The Savings Plan reward does not change the interest rate. It pays its own bonus on the same card: on a day you earned survival points, 5% of them, rounded down, at least 1 and at most 10. That limit of 10 belongs to the reward, and `MaxInterest` does not change it.

The tip on the end-of-day page still says the rate is 5% and the limit is 10. The interest you are paid follows `InterestRate` and `MaxInterest`.

The mod sets the rate a moment after a game is loaded, long before the day ends. Your save keeps the game's rate, so without the mod the interest is back to 5%.

## Locks

| Setting | Default | What it does |
| --- | --- | --- |
| `MaxLocked` | `1` | How many rewards you can lock at once. Locked rewards stay on the screen after a refresh and are still there the next day. `1` is the game's number. It can go up to 1 less than `OptionCount`, so a refresh always has a card to change. |

With `MaxLocked` above 1, the footer shows how many locks are in use (for example 2/3 next to a lock), and the lock button on a card adds or removes a lock. When every lock is in use, unlock a card first.

## Cards

| Setting | Default | What it does |
| --- | --- | --- |
| `NoRepeatOnRefresh` | `false` | `true` makes a refresh show only rewards you have not seen yet that day. Once the day runs out of new ones, a refresh shows fewer cards. |
| `BanButton` | `true` | A 「Ban」 button on each card. A banned reward is swapped for another and never comes back for the rest of the run. |
| `BanCost` | `2` | How many survival points a ban costs, 0 to 99. `0` makes it free. |
| `BanWholeLine` | `false` | `true` makes a ban also ban every level of the same reward. |
| `UnbanRefund` | `50` | How much of what a ban cost, in percent, comes back when you remove it from the 「Bans」 list, 0 to 100. |
| `Labels` | `true` | Shows on each card whether its reward is permanent, one-time, or lasts for a time (for example 3 DAYS). |

The buttons show only on cards you can still act on: not bought and not locked. The footer also shows the interest you will get tomorrow. With `BanButton` on, a 「Bans」 button in the footer lists this run's bans. Removing one lets its rewards come back. A ban made with `BanWholeLine` is removed as a whole, and its refund is a share of what it cost when you made it.

## Shop

| Setting | Default | What it does |
| --- | --- | --- |
| `ShopEnabled` | `false` | A 「Shop」 button on the rewards screen: search every reward you can get and buy it with survival points. |
| `ShopUnavailable` | `Grey` | What the shop does with a reward you cannot buy now. `Grey` shows it greyed out with the reason, and `Hide` leaves it out. |
| `ShopMarkup` | `25` | How much more, in percent, a reward costs in the shop than on a card, 0 to 200. |
| `ShopIncrease` | `15` | How much more, in percent of the card price, each shop purchase adds to every shop price until the prices reset, 0 to 100. |
| `ShopReset` | `Day` | When shop prices drop back after your purchases have raised them: `Day`, `Week` (every 7 days) or `Never`. |
| `ShopPerDay` | `2` | How many shop purchases you can make in a day, 0 to 99. `0` is no limit. |
| `ShopLocked` | `true` | `true` lets you buy a reward in the shop before you have the reward it needs, at double the price. |

A shop price is the card price times (100 + `ShopMarkup` + `ShopIncrease` x purchases since the last reset) / 100, rounded up. With the defaults, a reward that costs 8 on a card costs 10 in the shop, then 12 after one purchase, then 13 after two. The shop lists rewards by category, with a search box, a filter for permanent, one-time or temporary rewards, and a switch to show only what you can buy now. Rewards you cannot buy say why: you own it, it needs another reward first, it is banned this run, not available yet, and so on.

## How many rewards fit

Up to 5 rewards sit side by side in one row. More than 5 are spread over even rows (6 rewards are 2 rows of 3, 10 are 2 rows of 5, 20 are 4 rows of 5), and the cards get smaller so more of them fit. If they still do not all fit on the screen, the reward area scrolls.

There are no keys to set. The buttons on the rewards screen are set with the settings above.

## Interface

| Setting | Default | What it does |
| --- | --- | --- |
| `UiScale` | `0` | How big the windows, panels and messages of More Rewards are drawn, in percent, from 75 to 200. At `0` they follow the UI size set in Exiled's Toolkit (its `UiScale`). |

## Your own rewards

`BepInEx\config\MoreRewards.Rewards.json` adds your own end-of-day rewards or changes the game's. It is written the first time the game starts with the mod, with notes on every part and an example to copy, written as a comment so it changes nothing. Edit it while the game is closed. Changes apply the next time the game starts. Delete it to get the default back.

A reward can set its cost, name and description (in English and Chinese), the rewards it needs first, the furniture you must be able to build, the pools it is drawn from and how often, and what it gives. A new reward copies a game reward (`copyFrom`) for its icon and look, then changes what you set. It can give any effect the game has, such as more solar power, or more wind turbine power with Wind Power installed.

Anything the mod cannot use is left out and written to `BepInEx\more-rewards.log` with the reason. The rest of the file still applies. With `Enabled` off, nothing in the file applies, except that new rewards stay in the tables, never offered, so a save that owns one keeps it.
