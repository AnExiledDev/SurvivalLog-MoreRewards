# More Rewards

Choose from more rewards at the end of the day, lock more than one, ban the ones you do not want, and earn more interest on your survival points. An optional shop lets you buy any reward you can get.

At the end of each day, Survival Log offers 3 rewards you can buy with survival points, and a refresh for 2 points draws 3 new ones. More Rewards lets you raise that to any number up to 20, from the same reward pool the game draws from. The refresh cost goes up with the number of rewards on its own, or you can make it free or set any cost you like. The interest your survival points earn each day goes from 5% to 10%, and can go up to 20 instead of 10.

## Features

- **More rewards to choose from**: set how many rewards the end of the day offers, from the game's 3 up to 20. A refresh draws that many too.
- **The game's own rewards**: the extra rewards come from the same groups the game picks that day's rewards from, never repeat a reward already on offer, skip the rewards the game has on cooldown just as its own picks do, and are bought like any other.
- **A fair refresh cost**: by default the cost goes up by 1 survival point at 4, 6, 8 rewards and so on (3 rewards cost 2, 4 or 5 cost 3, 6 or 7 cost 4, 10 cost 6). Or make every refresh free, or set a fixed cost of your own.
- **More interest**: when a day ends, the game pays interest on the survival points you hold, 5% of them, at most 10. The mod raises the rate to 10% and the limit to 20, so saving up to 200 points pays off. Set your own rate and limit up to 100, or keep the game's. The Savings Plan reward keeps paying its own bonus on top.
- **Lock more than one reward**: set `MaxLocked` and lock up to 1 less than the number of rewards. Locked rewards stay through a refresh and are still there the next day. The footer shows how many locks are in use.
- **Ban**: a 「Ban」 button swaps a card for another and keeps it away for the rest of the run, for a few survival points (2 by default), and can ban every level of the same reward.
- **The ban list**: a 「Bans」 button in the footer lists this run's bans. Removing one lets its rewards come back and gives back half of what the ban cost (configurable).
- **Settings on the rewards screen**: a 「Settings」 button in the footer changes every More Rewards setting right there, and the change takes effect at once.
- **How long a reward lasts**: each card says whether its reward is permanent, one-time, or lasts for a time, such as 「3 DAYS」.
- **No repeats on refresh** (off by default): a refresh shows rewards you have not seen yet that day.
- **Tomorrow's interest**: the footer shows the interest you will get when the day ends.
- **Cash out** (off by default): survival points left when you leave the rewards screen turn into cash.
- **The shop** (off by default): a 「Shop」 button opens a searchable list of every reward you can get, by category and by permanent, one-time or temporary. Shop prices are 25% above card prices and go up 15% with each purchase until the next day, all of it configurable. Rewards you cannot buy now are greyed out with the reason, or hidden. Purchases are limited to 2 a day, and a reward can be bought before the one it needs at double the price; both are configurable.
- **UI size**: with Exiled's Toolkit 0.10.0 or newer, its UI size setting (`UiScale`) draws the shop, the ban list and the settings bigger or smaller, as far as each still fits on the screen. The buttons on the cards and in the footer keep the game's size. With Toolkit 0.11.0 or newer, More Rewards's own `UiScale` under Interface sets a size for this mod alone; at 0, the default, it follows the Toolkit's.
- **Room for every card**: up to 5 rewards sit in one row. More are spread over even rows with smaller cards, and the area scrolls if they still do not fit.
- **Rewards stay as they are until you ask**: with the default settings you get the game's 3 rewards and its own refresh cost.
- **A log file**: what the mod did is written to `BepInEx\more-rewards.log`.

There are no new keys. The new buttons are 「Ban」 on each card, and 「Shop」, 「Bans」 and 「Settings」 in the footer. 「Settings」 is always there; the others are switched by their settings.

Works in English and Chinese. The buttons and labels the mod adds follow the game's language, and the settings file describes every setting in English and Simplified Chinese. The log is in English.

More Rewards does not edit game files. Extra locks, bans and shop purchases are kept in the run's own counters, so they are saved with the run and start over with each loop; without the mod the game ignores them. Your save keeps the game's own interest rate. You can remove it at any time.

## Known issues and limits

- Some days give a fixed set of rewards instead of drawing them. Those days are left as the game has them.
- If a day's reward groups run out, you get fewer rewards than you asked for. The log says how many short it was.
- A refresh that finds no new rewards at all stays empty and costs nothing, as in the game. It is not filled up with the rewards you already saw.
- The new refresh cost applies a moment after the game starts, once the game has loaded its own cost.
- The new interest rate applies a moment after a game is loaded, long before the day ends.
- The interest shown in the footer is worked out from your points, the rate and the limit; the Savings Plan bonus is not in it.
- The shop leaves out rewards the game has no name for and rewards it never offers.
- The tip on the end-of-day page still says interest is 5% and at most 10. The interest you are paid follows `InterestRate` and `MaxInterest`.

## Requirements

- Survival Log 1.1.19611. Built for this version; a game update can break it until the mod is updated.
- The [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12), the BepInEx 6 (IL2CPP) build for the game, version `6.0.0-be.788`.
- Optional: [Exiled's Toolkit](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit) 0.4.0 or newer. With it, settings changed on the Mod Manager's Mods screen take effect without restarting the game. Without it, those take effect at the next game start; settings changed on the rewards screen take effect at once either way. With 0.10.0 or newer, its UI size setting also sizes the shop, the ban list and the settings.

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12) (if no other mods were installed before, start the game once so BepInEx finishes setup, then quit).
2. Optional, to change settings without a restart: install Exiled's Toolkit. Download `ExiledToolkit-x.y.z.zip` from [its latest release](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit/releases/latest) or from [Nexus Mods](https://www.nexusmods.com/survivallog/mods/61), and extract it into the game folder (the folder with `SurvivalLog.exe`).
3. Download `MoreRewards-x.y.z.zip` from the [latest release](https://github.com/AnExiledDev/SurvivalLog-MoreRewards/releases/latest) or from [Nexus Mods](https://www.nexusmods.com/survivallog/mods/49).
4. Extract the zip into the game folder (the folder with `SurvivalLog.exe`). It holds a `BepInEx` folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
   `C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\MoreRewards.dll`
5. Start the game once, quit, and set `OptionCount` in the settings file (see [Configuration](#configuration)). Until you do, you get the game's 3 rewards; the higher interest rate and limit apply right away.

## Uninstall

Delete `MoreRewards.dll` from the `BepInEx\plugins` folder. For a clean slate, also delete `BepInEx\config\com.dev.survivallog.morerewards.cfg` and `BepInEx\more-rewards.log`.

## Configuration

Settings live in `BepInEx\config\com.dev.survivallog.morerewards.cfg`, created the first time the game starts with the mod. Change them in game with the 「Settings」 button on the rewards screen or on the Mod Manager's Mods screen, or edit the file while the game is closed. Changed in game, every setting except Enabled takes effect at once; Enabled takes effect at the next game start. See [CONFIG.md](CONFIG.md) for every setting and the full refresh cost table. Without Exiled's Toolkit installed, changes made on the Mods screen take effect at the next game start instead; changes made on the rewards screen still take effect at once.

## Troubleshooting

Built against BepInEx `6.0.0-be.788`.

Check `BepInEx\LogOutput.log` for a line starting `===== More Rewards 0.5.2 started`. It says whether the mod is on and the interest limit. The lines after it say what took effect: the interest limit (`interest is now up to 20 survival points a day`), how many rewards and the refresh cost (`10 rewards, refresh cost automatic`), the interest rate, and `hooked the end-of-day rewards`. Changing one of these on the Mods screen writes its line again. Once a game is loaded, a line says `interest rate is now 10%`.

Each time a day ends or you refresh, the log gets a line saying how many rewards the game offered and how many the mod added. Shortly after the game starts, a line says what a refresh costs now. The reward lines are written when `OptionCount` is above 3 or a lock or ban changed the offer, and the cost line only when the mod changes the cost.

The same lines are in `BepInEx\more-rewards.log`, which survives restarts.

If you still see only 3 rewards, the refresh cost has not changed, the interest is still 5%, or it stops at 10, look in the log for a line starting `could not`, `failed` or `the game's interest code`. When the mod cannot do its part, the game keeps its own rewards, cost and interest.

## Feedback and bug reports

Found a bug, have an idea, or something feels off? Come say so on the Core Directive Discord: https://discord.gg/trTsyxUFqg . You can also post in the Posts tab of the mod's Nexus Mods page. When reporting a bug, attach `BepInEx\LogOutput.log` and `BepInEx\more-rewards.log`.

To reach me directly, message `exiled.dev` on Discord; you can find me in the [official Survival Log Discord](https://discord.gg/MZGg4gQAUe). I'm also on X as [@AnExiledDev](https://x.com/AnExiledDev).

A Survival Log wiki with detailed documentation for all of my mods is coming soon.

## Build

This is a BepInEx 6 IL2CPP plugin. It compiles against BepInEx and the game's IL2CPP interop assemblies, so a game install with BepInEx set up and started once is required. Those assemblies come from the game and are not part of this repo. The .NET 8 SDK is required.

```
dotnet build src/MoreRewards -c Release -p:GameDir="C:\path\to\Survival Log"
```

`GameDir` is the game folder. The default in the project file is a local path you will almost certainly need to override. The output DLL is at `src\MoreRewards\bin\Release\MoreRewards.dll`.

The unit tests cover the pure rules (the settings, the refresh cost, how extra rewards are drawn, the interest rate and limit, locks, bans and shop prices) and the page script that lays out the cards and draws the buttons and the shop, and need nothing from the game:

```
dotnet test tests/MoreRewards.Tests
```

## License

MIT. Copyright (c) 2026 AnExiledDev. See [LICENSE](LICENSE).
