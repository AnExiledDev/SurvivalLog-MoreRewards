namespace MoreRewards.Core;

/// <summary>What the rewards screen's setup check should do this frame.</summary>
public enum SetupStep
{
    /// <summary>Nothing to read from the game this frame.</summary>
    Skip,

    /// <summary>Nothing to send; only see whether the run is still going (outside a run the screen's state is let go).</summary>
    CheckRun,

    /// <summary>The page is owed a setup: send it if the run is going and the screen is open.</summary>
    TrySend,
}

/// <summary>
/// Decides when the plugin reads the game to send the rewards page its setup, so a frame with nothing new reads
/// nothing. The page is owed a setup when the screen's card list was rebuilt, a setting it draws from changed, or the
/// page asked (it reloaded). Right after that it is tried every frame for <see cref="EagerSeconds"/>, so the buttons
/// show at once, then every <see cref="RetrySeconds"/> until it goes out (the screen may be closed until tomorrow).
/// With nothing owed, the run is checked every <see cref="RunCheckSeconds"/>. Nothing owed is ever dropped.
/// </summary>
public sealed class SetupWatch
{
    public const float EagerSeconds = 1f;
    public const float RetrySeconds = 0.5f;
    public const float RunCheckSeconds = 1f;

    private int _sentList = -1;
    private int _sentSettings = -1;
    private bool _isAsked;
    private int _asks;
    private (int List, int Settings, int Asks) _owed = (-1, -1, -1);
    private float _eagerUntil;
    private float _nextTryAt;
    private float _nextRunCheckAt;

    /// <summary>The page lost what it was told and asks for it again.</summary>
    public void Ask()
    {
        _isAsked = true;
        _asks++;
    }

    /// <param name="hasState">The screen's state is kept (it has opened this run); without it there is nothing to send or let go.</param>
    /// <param name="listVersion">Goes up with each rebuild of the screen's card list.</param>
    /// <param name="settingsVersion">Goes up with each change to a setting the page draws from.</param>
    public SetupStep Step(float now, bool hasState, int listVersion, int settingsVersion)
    {
        if (!hasState) return SetupStep.Skip;

        if (_isAsked || listVersion != _sentList || settingsVersion != _sentSettings)
        {
            var owed = (listVersion, settingsVersion, _asks);

            if (owed != _owed)
            {
                _owed = owed;
                _eagerUntil = now + EagerSeconds;
            }

            if (now < _eagerUntil) return SetupStep.TrySend;

            if (now < _nextTryAt) return SetupStep.Skip;

            _nextTryAt = now + RetrySeconds;

            return SetupStep.TrySend;
        }

        if (now < _nextRunCheckAt) return SetupStep.Skip;

        _nextRunCheckAt = now + RunCheckSeconds;

        return SetupStep.CheckRun;
    }

    /// <summary>The setup for these versions went out.</summary>
    public void Sent(int listVersion, int settingsVersion)
    {
        _isAsked = false;
        _sentList = listVersion;
        _sentSettings = settingsVersion;
    }
}
