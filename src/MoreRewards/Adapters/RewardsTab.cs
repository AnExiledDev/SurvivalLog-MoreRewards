using System;
using System.Collections.Generic;
using System.IO;
using ExiledToolkit.Game;
using ExiledToolkit.Web;
using MoreRewards.Core;
using ToolkitPage = ExiledToolkit.Web.WebPage;

namespace MoreRewards.Adapters;

/// <summary>
/// The plugin's side of the Rewards tab in the Mod Manager. It keeps rewards-tab.js in the web UI, hands the tab the
/// game's rewards and the rewards file, and writes the file back when the tab saves. A save applies at once: the file
/// is read again and written into the game's tables.
/// </summary>
internal sealed class RewardsTab
{
    private const string Resource = "MoreRewards.rewards-tab.js";
    private const string Global = "window.__slmrRewards";
    private const string NoScript = "noscript";

    private static readonly LocalText Saved = new("Saved. The changes apply now.", "已保存，修改立即生效。");
    private static readonly LocalText NotSaved = new("Not saved:", "未保存：");

    private readonly string _path;
    private readonly RewardTables _tables;
    private readonly string? _builtIn;
    private readonly Action _reload;
    private readonly Action<string> _log;
    private readonly ToolkitPage _page;
    private readonly PageMailbox _inbox = PageInbox.Listen(RewardsPage.Prefix);

    /// <param name="reload">Reads the rewards file again after a save, so what it holds now applies.</param>
    /// <param name="builtIn">The rewards the mod adds itself, as file text, or null when it adds none.</param>
    public RewardsTab(string path, RewardTables tables, string? builtIn, Action reload, Action<string> log)
    {
        _path = path;
        _tables = tables;
        _builtIn = builtIn;
        _reload = reload;
        _log = log;
        // The tab draws with the Toolkit's reward editor, which goes first.
        _page = new ToolkitPage(typeof(RewardsTab).Assembly, Resource, Global, RewardsPage.Prefix + NoScript, log)
        {
            ToolkitScripts = new[] { ToolkitPage.Editor },
        };
    }

    public void Tick(float now)
    {
        _page.KeepInstalled(now);

        while (_inbox.TryTake(out var message))
        {
            if (message == RewardsPage.Load) Load();
            else if (message.StartsWith(RewardsPage.Save, StringComparison.Ordinal)) Save(message.Substring(RewardsPage.Save.Length));
            else if (message == NoScript) _page.Install();
            else _log("rewards tab: " + message);
        }
    }

    private void Load()
    {
        string? text = null;

        try
        {
            text = File.ReadAllText(_path);
        }
        catch (Exception ex)
        {
            _log("rewards tab: could not read the rewards file: " + ex.Message);
        }

        _page.Run(RewardsPage.DataCall(_tables.Game, text, _tables.Problems, _builtIn));
    }

    private void Save(string json)
    {
        _page.Run(RewardsPage.CheckSave(json) switch
        {
            SaveCheck.Write write => Write(write.Text),
            SaveCheck.Refused refused => Refuse(refused.Problems),
            _ => throw new InvalidOperationException("unknown save check"),
        });
    }

    private string Write(string text)
    {
        try
        {
            if (File.Exists(_path)) File.Copy(_path, _path + ".bak", overwrite: true);

            File.WriteAllText(_path, text);
            _log($"rewards tab saved the rewards file ({text.Length} characters); the old one is {_path}.bak");
            _reload();

            return RewardsPage.SavedCall(true, Pick(Saved));
        }
        catch (Exception ex)
        {
            _log("rewards tab: could not save the rewards file: " + ex.Message);

            return RewardsPage.SavedCall(false, Pick(NotSaved) + " " + ex.Message);
        }
    }

    private string Refuse(IReadOnlyList<string> problems)
    {
        foreach (var problem in problems) _log("rewards tab save refused: " + problem);

        return RewardsPage.SavedCall(false, Pick(NotSaved) + " " + string.Join("; ", problems));
    }

    private static string Pick(LocalText text) => GameLanguage.IsChinese() ? text.Zh : text.En;
}
