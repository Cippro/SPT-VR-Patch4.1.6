using System.Reflection;

internal static class UiConfirmRegression
{
    public static void Run(Assembly plugin)
    {
        var stateType = plugin.GetType("TarkovVR.Source.UI.UiConfirmState", true);
        var eventType = plugin.GetType("TarkovVR.Source.UI.UiConfirmEvent", true);
        var step = stateType.GetMethod("Step", BindingFlags.Instance | BindingFlags.NonPublic);
        var reset = stateType.GetMethod("Reset", BindingFlags.Instance | BindingFlags.NonPublic);
        object state = Activator.CreateInstance(stateType, true);
        int down = Convert.ToInt32(Enum.Parse(eventType, "Down"));
        int up = Convert.ToInt32(Enum.Parse(eventType, "Up"));
        int click = Convert.ToInt32(Enum.Parse(eventType, "Click"));
        int context = Convert.ToInt32(Enum.Parse(eventType, "ContextClick"));
        int assertions = 0;
        void Check(int actual, int expected, string message)
        {
            if (actual != expected) throw new Exception(message + $" (got {actual}, expected {expected})");
            assertions++;
        }
        int Tick(bool press, bool held, bool release, bool target = true, bool same = true, bool drag = false, float dt = 0.016f) =>
            Convert.ToInt32(step.Invoke(state, new object[] { press, held, release, target, same, drag, dt }));
        void Reset() => reset.Invoke(state, null);

        Check(Tick(true, true, false, same: false), down, "Press starts on a UI target");
        Check(Tick(false, true, false, dt: 0.05f), 0, "Short hold is not a context click");
        Check(Tick(false, false, true), up | click, "Short A produces one left click and release");
        Check(Tick(false, false, true), 0, "Repeated release does not duplicate a click");

        Check(Tick(true, true, false, target: false, same: false), 0, "Press outside UI is ignored");
        Check(Tick(false, true, false, dt: 0.5f), 0, "Moving onto UI while held does not begin a press");
        Check(Tick(false, false, true), 0, "Release over UI after an outside press does not click");

        Tick(true, true, false);
        Check(Tick(false, false, true, target: false, same: false), up, "Release outside UI still releases the original press");
        Tick(true, true, false);
        Check(Tick(false, false, true, same: false), up, "Release over a different item does not activate it");

        Tick(true, true, false);
        Check(Tick(false, true, false, dt: 0.3f), context, "Holding A opens one context action");
        Check(Tick(false, true, false, dt: 0.5f), 0, "Continued hold does not repeat the context action");
        Check(Tick(false, false, true), up, "Context release does not also send a left click");
        Check(Tick(true, true, false), down, "Next short click can start after a context action");
        Check(Tick(false, false, true), up | click, "Context state does not leak into the next click");

        Tick(true, true, false);
        Check(Tick(false, true, false, drag: true, dt: 0.4f), 0, "Dragging suppresses the context action");
        Check(Tick(false, false, true), up, "A drag-canceled click remains canceled after the trigger is released");

        Check(Tick(false, true, false, dt: 1f), 0, "Opening a menu with A already held does not click");
        Tick(true, true, false);
        Reset();
        Check(Tick(false, false, true), 0, "Disabling UI discards an unfinished press");

        Tick(true, true, false);
        Check(Tick(false, true, false, dt: -1f), 0, "Invalid negative time does not produce an action");
        Check(Tick(false, false, true), up | click, "Short press still completes after a zero-time frame");

        Tick(true, true, false);
        Check(Tick(false, true, false, target: false, same: false, dt: 0.3f), 0, "Holding away from the target does not activate another item");
        Check(Tick(false, false, true, target: false, same: false), up, "Outside release after a long hold clears the press");
        Console.WriteLine($"UI confirm regression: {assertions} assertions passed against the compiled plugin.");
    }
}
