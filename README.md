# More Rewards

Choose from more rewards at the end of the day, set what a refresh costs, and earn more interest on your survival points.

At the end of each day, Survival Log offers 3 rewards you can buy with survival points, and a refresh for 2 points draws 3 new ones. More Rewards lets you raise that to any number up to 20, from the same reward pool the game draws from. The refresh cost goes up with the number of rewards on its own, or you can make it free or set any cost you like. The interest your survival points earn each day can go up to 20 instead of 10.

## Features

- **More rewards to choose from**: set how many rewards the end of the day offers, from the game's 3 up to 20. A refresh draws that many too.
- **The game's own rewards**: the extra rewards come from the same groups the game picks that day's rewards from, never repeat a reward already on offer, and are bought like any other.
- **A fair refresh cost**: by default the cost goes up by 1 survival point for every 2 rewards more (3 rewards cost 2, 4 cost 3, 6 cost 4, 10 cost 6). Or make every refresh free, or set a fixed cost of your own.
- **More interest**: when a day ends, the game pays 5% interest on the survival points you hold, at most 10. The mod raises that limit to 20, so saving up to 400 points pays off. Set your own limit up to 100, or keep the game's.
- **Room for every card**: up to 5 rewards sit in one row. More are spread over even rows with smaller cards, and the area scrolls if they still do not fit.
- **Rewards stay as they are until you ask**: with the default settings you get the game's 3 rewards and its own refresh cost.
- **A log file**: what the mod did is written to `BepInEx\more-rewards.log`.

There are no new keys or buttons.

More Rewards adds no data to your save and does not edit game files. You can remove it at any time.

## Known issues and limits

- Some days give a fixed set of rewards instead of drawing them. Those days are left as the game has them.
- If a day's reward groups run out, you get fewer rewards than you asked for. The log says how many short it was.
- A refresh that finds no new rewards at all stays empty and costs nothing, as in the game. It is not filled up with the rewards you already saw.
- The new refresh cost applies a moment after the game starts, once the game has loaded its own cost.
- The tip on the end-of-day page still says interest is at most 10. The interest you are paid follows `MaxInterest`.
- The bigger layouts are the least tested part, and not tested on every screen size. If cards are cut off or overlap, please tell me (see [Feedback](#feedback-and-bug-reports)) and include your screen resolution and `BepInEx\more-rewards.log`.
- If something odd happens with the end-of-day rewards or the interest, try it with `Enabled` set to `false` to see whether the mod caused it, and please tell me either way.
- The mod's own text, in the log, is in English.

## Requirements

- Survival Log 1.1.18317. Built for this version; a game update can break it until the mod is updated.
- The [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12), the BepInEx 6 (IL2CPP) build for the game, version `6.0.0-be.788`.

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12) (if no other mods were installed before, start the game once so BepInEx finishes setup, then quit).
2. Download `MoreRewards.dll` from the [latest release](https://github.com/AnExiledDev/SurvivalLog-MoreRewards/releases/latest) and put it in the `BepInEx\plugins` folder inside the game folder (the folder with the game .exe). Full path example: `C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\MoreRewards.dll`
3. Start the game once, quit, and set `OptionCount` in the settings file (see [Configuration](#configuration)). Until you do, you get the game's 3 rewards; the higher interest limit applies right away.

## Uninstall

Delete `MoreRewards.dll` from the `BepInEx\plugins` folder. For a clean slate, also delete `BepInEx\config\com.dev.survivallog.morerewards.cfg` and `BepInEx\more-rewards.log`.

## Configuration

Settings live in `BepInEx\config\com.dev.survivallog.morerewards.cfg`, created the first time the game starts with the mod. Edit it while the game is closed; changes apply at the next game start. See [CONFIG.md](CONFIG.md) for every setting and the full refresh cost table.

## Troubleshooting

Built against BepInEx `6.0.0-be.788`.

Check `BepInEx\LogOutput.log` for a line starting `===== More Rewards 0.2.0 started`. It says whether the mod is on, how many rewards it offers, the refresh cost, and the interest limit. The next lines should say the changes went in, for example `interest is now up to 20 survival points a day` and `hooked the end-of-day rewards: up to 10 to choose from`.

Each time a day ends or you refresh, the log gets a line saying how many rewards the game offered and how many the mod added. Shortly after the game starts, a line says what a refresh costs now. The reward lines are only written when `OptionCount` is above 3, and the cost line only when the mod changes the cost. When the rewards and the refresh cost match the game's own, the log says so once at the start and stays quiet about them after that.

The same lines are in `BepInEx\more-rewards.log`, which survives restarts.

If you still see only 3 rewards, the refresh cost has not changed, or the interest stops at 10, look in the log for a line starting `could not`, `failed` or `the game's interest code`. When the mod cannot do its part, the game keeps its own rewards, cost and interest.

## Feedback and bug reports

Found a bug, have an idea, or something feels off? Come say so on the Core Directive Discord: https://discord.gg/trTsyxUFqg . You can also leave a comment on the Nexus page or [open an issue](https://github.com/AnExiledDev/SurvivalLog-MoreRewards/issues). When reporting a bug, attach `BepInEx\LogOutput.log` and `BepInEx\more-rewards.log`.

## Build

This is a BepInEx 6 IL2CPP plugin. It compiles against BepInEx and the game's IL2CPP interop assemblies, so a game install with BepInEx set up and started once is required. Those assemblies come from the game and are not part of this repo. The .NET 8 SDK is required.

```
dotnet build src/MoreRewards -c Release -p:GameDir="C:\path\to\Survival Log"
```

`GameDir` is the game folder. The default in the project file is a local path you will almost certainly need to override. The output DLL is at `src\MoreRewards\bin\Release\MoreRewards.dll`.

The unit tests cover the pure rules (the settings, the refresh cost, how extra rewards are drawn, the interest limit) and the page script that lays out the cards, and need nothing from the game:

```
dotnet test tests/MoreRewards.Tests
```

## License

MIT. Copyright (c) 2026 AnExiledDev. See [LICENSE](LICENSE).
