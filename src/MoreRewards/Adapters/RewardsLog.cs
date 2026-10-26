using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Logging;
using MoreRewards.Core;

namespace MoreRewards.Adapters;

/// <summary>
/// Writes to the BepInEx log and to an append-only file that survives restarts
/// (LogOutput.log and Player.log are both overwritten on the next launch).
/// The file is written on a worker thread, so a frame that logs never waits for the disk: each line keeps the time it
/// was logged and waits in a queue, and one worker at a time writes whatever is queued in one go. The worker never logs.
/// Never throws: it is called from inside Load and the game's hooks, where an escaping exception would reach the game.
/// </summary>
internal sealed class RewardsLog
{
    private const long TrimAboveBytes = 1024 * 1024;
    private const int KeepBytes = 200 * 1024;

    private readonly ManualLogSource _bepinex;
    private readonly string _path;
    private readonly object _gate = new();
    private readonly ConcurrentQueue<(DateTime At, string Message)> _queue = new();
    private int _isWriting;
    private Task? _writer;

    public RewardsLog(ManualLogSource bepinex, string path)
    {
        _bepinex = bepinex;
        _path = path;
    }

    public void Write(string message)
    {
        if (HookFailure.Is(message)) _bepinex.LogError(message);
        else _bepinex.LogInfo(message);

        WriteFileOnly(message);
    }

    /// <summary>Writes only to the mod's own file, never the BepInEx log; safe from a worker thread.</summary>
    public void WriteFileOnly(string message)
    {
        _queue.Enqueue((DateTime.Now, message));
        StartWriter();
    }

    /// <summary>Waits up to <paramref name="timeout"/> for the lines logged so far to reach the file; for the game closing.</summary>
    public void Finish(TimeSpan timeout)
    {
        try
        {
            _writer?.Wait(timeout);

            // A line queued as the last write ended may not have a writer yet; the game is closing, so write it here.
            if (!_queue.IsEmpty) WriteQueued();
        }
        catch (Exception)
        {
            // Closing the game must not wait on, or fail for, the log file.
        }
    }

    private void StartWriter()
    {
        if (Interlocked.CompareExchange(ref _isWriting, 1, 0) != 0) return;

        try
        {
            _writer = Task.Run(WriteAll);
        }
        catch (Exception)
        {
            Interlocked.Exchange(ref _isWriting, 0);
        }
    }

    private void WriteAll()
    {
        while (true)
        {
            WriteQueued();
            Interlocked.Exchange(ref _isWriting, 0);

            // A line queued after the last write but before the flag went down found a writer still running; take it on.
            if (_queue.IsEmpty || Interlocked.CompareExchange(ref _isWriting, 1, 0) != 0) return;
        }
    }

    private void WriteQueued()
    {
        lock (_gate)
        {
            try
            {
                var text = new StringBuilder();

                while (_queue.TryDequeue(out var line)) text.Append($"{line.At:yyyy-MM-dd HH:mm:ss.fff} {line.Message}").Append(Environment.NewLine);

                if (text.Length > 0) File.AppendAllText(_path, text.ToString());
            }
            catch (Exception)
            {
                // A locked or read-only file costs the file copy, never the plugin.
            }
        }
    }

    /// <summary>Past 1 MB only the newest ~200 KB of whole lines is kept.</summary>
    public void TrimIfLarge()
    {
        lock (_gate)
        {
            try
            {
                var file = new FileInfo(_path);

                if (!file.Exists || file.Length <= TrimAboveBytes) return;

                var bytes = File.ReadAllBytes(_path);
                var cut = bytes.Length - KeepBytes;
                var nextLine = Array.IndexOf(bytes, (byte)'\n', cut) + 1;

                File.WriteAllBytes(_path, bytes[(nextLine > 0 ? nextLine : cut)..]);
            }
            catch (Exception)
            {
                // An oversized log is harmless; failing to trim it must not stop the plugin loading.
            }
        }
    }
}
