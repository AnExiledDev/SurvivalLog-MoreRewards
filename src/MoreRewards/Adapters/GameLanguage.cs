using System;
using GameCore.HotUpdate;
using MoreRewards.Core;

namespace MoreRewards.Adapters;

/// <summary>Asks the game which language it is showing, only once the game has made its settings. Anything that goes wrong reads as English.</summary>
internal static class GameLanguage
{
    public static Language Current()
    {
        try
        {
            return GameSingleton.Existing<SettingManager>()?.IsChinese() == true ? Language.Chinese : Language.English;
        }
        catch (Exception)
        {
            return Language.English;
        }
    }
}
