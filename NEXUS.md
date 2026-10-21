Name: More Rewards
Summary: Choose from up to 20 rewards at the end of each day instead of 3, drawn from the game's own pool with no repeats, and earn 10% daily interest on your survival points instead of 5%. The refresh cost scales with the number of cards, or make it free or set your own. Cards shrink and wrap into rows so they all fit on screen.

Description (BBCode):
At the end of each day, Survival Log offers 3 rewards you can buy with survival points, and a refresh for 2 points draws 3 new ones. More Rewards lets you raise that to any number up to 20, from the same reward pool the game draws from. The refresh cost goes up with the number of rewards on its own, or you can make it free or set any cost you like. The interest your survival points earn each day goes from 5% to 10%, and can go up to 20 instead of 10.

[size=5]Main features[/size]
[list]
[*][b]More rewards to choose from[/b]: set how many rewards the end of the day offers, from the game's 3 up to 20. A refresh draws that many too.[/*]
[*][b]The game's own rewards[/b]: the extra rewards come from the same groups the game picks that day's rewards from, never repeat a reward already on offer, skip the rewards the game has on cooldown just as its own picks do, and are bought like any other.[/*]
[*][b]A fair refresh cost[/b]: by default the cost goes up by 1 survival point at 4, 6, 8 rewards and so on (3 rewards cost 2, 4 or 5 cost 3, 6 or 7 cost 4, 10 cost 6). Or make every refresh free, or set a fixed cost of your own.[/*]
[*][b]More interest[/b]: when a day ends, the game pays interest on the survival points you hold, 5% of them, at most 10. The mod raises the rate to 10% and the limit to 20, so saving up to 200 points pays off. Set your own rate and limit up to 100, or keep the game's. The Savings Plan reward keeps paying its own bonus on top.[/*]
[*][b]Room for every card[/b]: up to 5 rewards sit in one row. More are spread over even rows with smaller cards, and the area scrolls if they still do not fit.[/*]
[*][b]Rewards stay as they are until you ask[/b]: with the default settings you get the game's 3 rewards and its own refresh cost.[/*]
[/list]
No new keys or buttons. More Rewards adds no data to your save and does not edit game files. Your save keeps the game's own interest rate. You can remove it at any time.

Works in English and Chinese.

[size=5]Known issues[/size]
[list]
[*]Some days give a fixed set of rewards instead of drawing them. Those days are left as the game has them.[/*]
[*]If a day's reward groups run out, you get fewer rewards than you asked for.[/*]
[*]A refresh that finds no new rewards at all stays empty and costs nothing, as in the game. It is not filled up with the rewards you already saw.[/*]
[*]The new refresh cost applies a moment after the game starts, once the game has loaded its own cost.[/*]
[*]The new interest rate applies a moment after a game is loaded, long before the day ends.[/*]
[*]The tip on the end-of-day page still says interest is 5% and at most 10. The interest you are paid follows InterestRate and MaxInterest.[/*]
[/list]

[size=5]Requirements[/size]
[list]
[*]Survival Log 1.1.19131. A game update can break it until the mod is updated.[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url] (BepInEx 6.0.0-be.788, IL2CPP). Install it first.[/*]
[*]Optional: [url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.4.0 or newer. With it, settings changed on the Mod Manager's Mods screen take effect without restarting the game. Without it, they take effect at the next game start.[/*]
[/list]

[size=5]Installation[/size]
[list=1]
[*]Install [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i](if no other mods were installed before, start the game once so BepInEx finishes setup, then quit)[/i].[/*]
[*]Optional, to change settings without a restart: install Exiled's Toolkit the same way. Download its zip and extract it into the game folder.[/*]
[*]Download the zip from the Files tab.[/*]
[*]Extract it into the game folder (the folder with [font=Courier New]SurvivalLog.exe[/font]). The zip holds a BepInEx folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\MoreRewards.dll[/code][/*]
[*]Start the game once, quit, and set OptionCount in the settings file (see Configuration below). Until you do, you get the game's 3 rewards; the higher interest rate and limit apply right away.[/*]
[/list]

[size=5]Uninstall[/size]
Delete [font=Courier New]MoreRewards.dll[/font] from the [font=Courier New]BepInEx\plugins[/font] folder.

[size=5]Configuration[/size]
Edit [font=Courier New]BepInEx\config\com.dev.survivallog.morerewards.cfg[/font] while the game is closed, or do it in game on the Mod Manager's Mods screen (see [url=https://github.com/AnExiledDev/SurvivalLog-MoreRewards/blob/main/CONFIG.md]CONFIG.md[/url] for every setting and the full cost table). Changed there, OptionCount, RefreshCost and InterestRate take effect at once; Enabled and MaxInterest take effect at the next game start. Without Exiled's Toolkit installed, changes made on the Mods screen take effect at the next game start instead.
[list]
[*][b]OptionCount[/b] (default 3): how many rewards to choose from, 3 to 20.[/*]
[*][b]RefreshCost[/b] (default -1): -1 scales the cost with OptionCount, 0 makes every refresh free, and 1 to 99 is the cost of every refresh.[/*]
[*][b]InterestRate[/b] (default 10): the daily interest rate on your survival points, in percent. 0 keeps the game's 5%; 1 to 100 sets a new rate.[/*]
[*][b]MaxInterest[/b] (default 20): the most interest your survival points earn in a day. 0 keeps the game's limit of 10; 1 to 100 sets a new limit.[/*]
[*][b]Enabled[/b] (default true): false turns the whole mod off.[/*]
[/list]

[size=5]Feedback[/size]
Found a bug or have an idea? Come say hi on the [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url], or post in this page's Posts tab. For bugs, please include [font=Courier New]BepInEx\LogOutput.log[/font] and [font=Courier New]BepInEx\more-rewards.log[/font].
To reach me directly, message [b]exiled.dev[/b] on Discord; you can find me in the [url=https://discord.gg/MZGg4gQAUe]official Survival Log Discord[/url]. I'm also on X as [url=https://x.com/AnExiledDev]@AnExiledDev[/url].
Coming soon: a Survival Log wiki with detailed documentation for all of my mods.

[size=5]Source and license[/size]
Open source under MIT: [url=https://github.com/AnExiledDev/SurvivalLog-MoreRewards]github.com/AnExiledDev/SurvivalLog-MoreRewards[/url]

[line]
[size=5]简体中文[/size]

每天结束时，Survival Log 会提供 3 张可以用生存点购买的规划卡，花 2 点刷新可以换 3 张新的。More Rewards 让你把数量提高到最多 20 张，全部来自游戏本身抽卡的卡池。刷新花费会随规划卡数量自动上涨，你也可以让刷新免费，或者自定任意花费。生存点的每日回息从 5% 提高到 10%，上限从 10 提高到 20。

[size=5]主要功能[/size]
[list]
[*][b]更多规划卡可选[/b]：设置每天结束时提供几张规划卡，从游戏原本的 3 张到最多 20 张。刷新时也会抽出同样多的卡。[/*]
[*][b]都是游戏自己的规划卡[/b]：额外的规划卡来自游戏当天抽卡所用的同一批卡组，不会和已经在列的卡重复，也会像游戏自己抽卡一样跳过冷却中的卡，购买方式和其他卡完全相同。[/*]
[*][b]合理的刷新花费[/b]：默认情况下，规划卡数量到 4、6、8 张等时，刷新花费各加 1 点生存点（3 张花 2 点，4 或 5 张花 3 点，6 或 7 张花 4 点，10 张花 6 点）。也可以让每次刷新都免费，或者设一个固定花费。[/*]
[*][b]更多回息[/b]：每天结束时，游戏会按持有生存点的 5% 发放回息，最多 10 点。本模组把比例提高到 10%，上限提高到 20，生存点攒到 200 都有回报。比例和上限都可以自己设，最高 100，也可以保持游戏原样。「储蓄计划」照样会在此之外发放它自己的加成。[/*]
[*][b]每张卡都放得下[/b]：5 张以内排成一行。超过 5 张时平均分成几行，卡片会缩小；如果还是放不下，卡片区域可以滚动。[/*]
[*][b]不改设置就保持原样[/b]：默认设置下，仍是游戏原本的 3 张规划卡和原本的刷新花费。[/*]
[/list]
没有新增按键或按钮。More Rewards 不会往存档里添加任何内容，也不会修改游戏文件。存档里保存的仍是游戏原本的回息比例。随时可以移除。

支持英文和中文。

[size=5]已知问题[/size]
[list]
[*]有些日子提供的是固定的一组规划卡，不是随机抽取的。这些日子保持游戏原样。[/*]
[*]如果当天的卡组已经抽完，得到的规划卡会比设置的少。[/*]
[*]如果一次刷新完全抽不到新卡，结果会是空的，也不花生存点，和游戏原本一样，不会拿已经看过的卡来补满。[/*]
[*]新的刷新花费会在游戏启动片刻后、游戏读取完它自己的花费时生效。[/*]
[*]新的回息比例会在载入游戏片刻后生效，远早于当天结束。[/*]
[*]每日结算页面上的提示仍然写着回息 5%、上限 10。实际发放的回息以 InterestRate 和 MaxInterest 为准。[/*]
[/list]

[size=5]前置要求[/size]
[list]
[*]Survival Log 1.1.19131。游戏更新后模组可能暂时失效，直到模组更新为止。[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]（BepInEx 6.0.0-be.788，IL2CPP），请先安装。[/*]
[*]可选：[url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.4.0 或更新版本。装了它，在 Mod Manager 的「模组」界面改动的设置无需重启游戏即可生效；没装的话，改动在下次启动游戏时生效。[/*]
[/list]

[size=5]安装[/size]
[list=1]
[*]安装 [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i]（如果之前没有装过其他模组，先启动一次游戏，让 BepInEx 完成初始化，然后退出）[/i]。[/*]
[*]可选（改动设置无需重启游戏）：用同样的方式安装 Exiled's Toolkit，下载它的压缩包并解压到游戏目录。[/*]
[*]在「文件」页下载压缩包。[/*]
[*]把压缩包解压到游戏目录（[font=Courier New]SurvivalLog.exe[/font] 所在的文件夹）。压缩包里带有 BepInEx 文件夹，DLL 会自动放到正确位置。如果提示合并文件夹，选择合并。解压后应为：
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\MoreRewards.dll[/code][/*]
[*]启动一次游戏后退出，然后在设置文件里设置 OptionCount（见下方「配置」）。在此之前仍是游戏原本的 3 张规划卡；更高的回息比例和上限会立即生效。[/*]
[/list]

[size=5]卸载[/size]
从 [font=Courier New]BepInEx\plugins[/font] 文件夹中删除 [font=Courier New]MoreRewards.dll[/font]。

[size=5]配置[/size]
可以在游戏内 Mod Manager 的「模组」界面修改，也可以在游戏关闭时编辑 [font=Courier New]BepInEx\config\com.dev.survivallog.morerewards.cfg[/font]。配置文件中每项设置都有中文说明，所有设置和完整的刷新花费表见 [url=https://github.com/AnExiledDev/SurvivalLog-MoreRewards/blob/main/CONFIG.md]CONFIG.md[/url]（英文）。在「模组」界面修改时，OptionCount、RefreshCost 和 InterestRate 立即生效；Enabled 和 MaxInterest 在下次启动游戏时生效。没装 Exiled's Toolkit 的话，在「模组」界面的改动改为在下次启动游戏时生效。
[list]
[*][b]OptionCount[/b]（默认 3）：可供选择的规划卡数量，3 到 20。[/*]
[*][b]RefreshCost[/b]（默认 -1）：-1 表示刷新花费随 OptionCount 自动调整，0 表示每次刷新免费，1 到 99 则是每次刷新的固定花费。[/*]
[*][b]InterestRate[/b]（默认 10）：生存点每日回息的比例（百分比）。0 保持游戏原本的 5%；1 到 100 设定新的比例。[/*]
[*][b]MaxInterest[/b]（默认 20）：生存点每天最多能得到的回息。0 保持游戏原本的上限 10；1 到 100 设定新的上限。[/*]
[*][b]Enabled[/b]（默认 true）：设为 false 则关闭整个模组。[/*]
[/list]

[size=5]反馈[/size]
发现 bug 或有好点子？欢迎来 [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url] 打个招呼，或在本页的「Posts」标签里发帖。报告 bug 时，请附上 [font=Courier New]BepInEx\LogOutput.log[/font] 和 [font=Courier New]BepInEx\more-rewards.log[/font]。
想直接联系我，可以在 Discord 上给 [b]exiled.dev[/b] 发消息，我在 [url=https://discord.gg/MZGg4gQAUe]Survival Log 官方 Discord[/url] 里。也可以在 X 上找我：[url=https://x.com/AnExiledDev]@AnExiledDev[/url]。
即将推出：Survival Log 维基，收录我所有模组的详细说明。

[size=5]源代码与许可[/size]
基于 MIT 许可开源：[url=https://github.com/AnExiledDev/SurvivalLog-MoreRewards]github.com/AnExiledDev/SurvivalLog-MoreRewards[/url]
