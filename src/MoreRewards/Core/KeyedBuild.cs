using System;
using System.Collections.Generic;

namespace MoreRewards.Core;

/// <summary>A build done in steps (see <see cref="SlicedJob"/>), with what it made once the steps have all run.</summary>
public interface ISlicedBuild<out T>
{
    IEnumerable<bool> Steps();

    /// <summary>Read only after every step has run.</summary>
    T Result { get; }
}

/// <summary>
/// Which game table a cache was read from (<paramref name="Table"/>), and which of the game's text tables its names
/// came from (<paramref name="Text"/>): the game swaps in another text table when the language changes. Both are the
/// tables' addresses, which stay put while the game keeps them.
/// </summary>
public readonly record struct TableKey(long Table, long Text);

/// <summary>
/// Something built once for a key (the game's table and language) and kept until the key changes: another table or
/// another language throws it away and builds it again. The build can run a slice a frame (<see cref="Advance"/> and
/// <see cref="Continue"/>), or all at once when it is needed now (<see cref="Get"/>). A step that throws drops the
/// build, so the next ask starts a fresh one.
/// </summary>
public sealed class KeyedBuild<TKey, TValue> where TValue : class
{
    private static readonly Func<bool> Never = () => false;

    private readonly Func<TKey, ISlicedBuild<TValue>> _start;
    private readonly EqualityComparer<TKey> _same = EqualityComparer<TKey>.Default;
    private TKey _valueKey = default!;
    private TValue? _value;
    private TKey _buildKey = default!;
    private ISlicedBuild<TValue>? _build;
    private SlicedJob? _job;

    public KeyedBuild(Func<TKey, ISlicedBuild<TValue>> start) => _start = start;

    public bool IsBuilding => _job != null;

    /// <summary>How many builds have been started.</summary>
    public int Builds { get; private set; }

    /// <returns>What was built for <paramref name="key"/>, or null when it is not built yet. Never builds anything.</returns>
    public TValue? Ready(TKey key) => _value != null && _same.Equals(_valueKey, key) ? _value : null;

    /// <summary>Starts the build for <paramref name="key"/> if it is not built or building, then runs one slice of it.</summary>
    /// <returns>What was built for <paramref name="key"/>, or null while it is still building.</returns>
    public TValue? Advance(TKey key, Func<bool> isSliceSpent)
    {
        if (Ready(key) is { } ready) return ready;

        if (_job == null || !_same.Equals(_buildKey, key)) Start(key);

        RunSlice(isSliceSpent);

        return Ready(key);
    }

    /// <summary>Runs one slice of the build in progress, without asking for the key again.</summary>
    public void Continue(Func<bool> isSliceSpent)
    {
        if (_job != null) RunSlice(isSliceSpent);
    }

    /// <summary>What was built for <paramref name="key"/>, finishing (or starting and finishing) its build now.</summary>
    public TValue Get(TKey key)
    {
        var value = Advance(key, Never);

        // A step can ask to stop for the frame; here there is no next frame to wait for.
        while (value == null && _job != null)
        {
            RunSlice(Never);
            value = Ready(key);
        }

        return value ?? throw new InvalidOperationException("the build ended without a result");
    }

    private void Start(TKey key)
    {
        _value = null;
        _buildKey = key;
        _build = _start(key);
        _job = new SlicedJob(_build.Steps());
        Builds++;
    }

    private void RunSlice(Func<bool> isSliceSpent)
    {
        bool isDone;

        try
        {
            isDone = _job!.Advance(isSliceSpent);
        }
        catch (Exception)
        {
            _job = null;
            _build = null;

            throw;
        }

        if (!isDone) return;

        _value = _build!.Result;
        _valueKey = _buildKey;
        _job = null;
        _build = null;
    }
}
