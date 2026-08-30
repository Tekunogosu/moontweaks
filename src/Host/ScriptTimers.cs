using System;
using System.Collections.Generic;
using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Server;

namespace MoonTweaks.Host;

/// <summary>What a handler is told when its own timer comes round.</summary>
/// <param name="seconds">How long since it last ran.</param>
[LuaTable("TimerEvent", Given = true)]
public sealed class TimerPayload(float seconds)
{
    /// <summary>
    /// Seconds since this last ran, which the server measures rather than assumes: a
    /// tick that ran late says so here, so work paced against real time stays paced.
    /// </summary>
    [LuaField("dt")]
    public double Dt { get; } = seconds;
}

/// <summary>
/// The timers a script asked for, and the only place their handlers are called.
/// Sole owner of what happens when one of them fails.
/// </summary>
/// <remarks>
/// A script's body runs while the server is loading, so a timer it asks for then is
/// recorded and started once the run is known to have succeeded — a check therefore
/// starts nothing. A handler asking for one is already past that point and starts it
/// at once, which is what lets a command hand a long job to the ticks that follow it.
/// </remarks>
public sealed class ScriptTimers(ICoreServerAPI api)
{
    private readonly List<Action> pending = [];
    private bool live;

    /// <summary>
    /// How many timers this run asked for, for the startup report. What was asked for
    /// rather than what is still going: a timer that stops itself or fails does not
    /// come back off this, and the report is written the moment they all start.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Runs a handler over and over, waiting this long between each. The handler stops
    /// it by answering false, which is what a job spread over several ticks does once
    /// it has finished.
    /// </summary>
    public void Every(int milliseconds, ScriptOrigin origin, ScriptValue.Func handler)
    {
        Waitable(milliseconds, origin, "every");

        Start(() =>
        {
            long listener = 0;
            listener = api.World.RegisterGameTickListener(
                seconds => Tick(handler, origin, seconds, () => api.World.UnregisterGameTickListener(listener)),
                milliseconds);
        });
    }

    /// <summary>Runs a handler once, this long from now.</summary>
    public void After(int milliseconds, ScriptOrigin origin, ScriptValue.Func handler)
    {
        Waitable(milliseconds, origin, "after");

        Start(() => api.World.RegisterCallback(
            seconds => Tick(handler, origin, seconds, null), milliseconds));
    }

    /// <summary>
    /// Refuses a wait a timer could never come round from. The game is handed the
    /// number as written and has no answer for a negative one, so a script that meant
    /// to subtract two times and got the order wrong is told what it asked for rather
    /// than left with a timer that silently never fires.
    /// </summary>
    private static void Waitable(int milliseconds, ScriptOrigin origin, string called)
    {
        if (milliseconds < 0)
        {
            throw new ScriptError(origin,
                $"{called} was asked to wait {milliseconds}ms, and a wait cannot be negative");
        }
    }

    /// <summary>
    /// Starts a timer now, or remembers to once the run it was asked for is known to
    /// have succeeded.
    /// </summary>
    private void Start(Action begin)
    {
        Count++;

        if (live) begin();
        else pending.Add(begin);
    }

    /// <summary>
    /// Calls one handler, and stops the timer if it asked to be stopped or failed.
    /// </summary>
    /// <remarks>
    /// A handler that throws is stopped rather than left running: a timer fires on its
    /// own with nobody watching, so one that failed once would go on failing every
    /// interval for as long as the server ran.
    /// </remarks>
    private void Tick(ScriptValue.Func handler, ScriptOrigin origin, float seconds, Action? stop)
    {
        try
        {
            var answer = handler.Call([PayloadWriter.Table(new TimerPayload(seconds))]);
            if (answer is ScriptValue.Bool { Value: false }) stop?.Invoke();
        }
        catch (Exception failure)
        {
            stop?.Invoke();
            api.Logger.Error("[moontweaks] {0}: timer failed and was stopped: {1}", origin, failure.Message);
        }
    }

    /// <summary>
    /// Starts every timer this run asked for. Called once, by the run whose handlers
    /// are meant to be live, and never by one whose results are discarded.
    /// </summary>
    public void Activate()
    {
        live = true;
        foreach (var begin in pending) begin();
        pending.Clear();
    }
}
