using System.Reflection;

internal static class MenuTaskRegression
{
    public static void Run(Assembly plugin)
    {
        int assertions = 0;
        void Check(bool value, string message)
        {
            if (!value) throw new Exception(message);
            assertions++;
        }
        var method = plugin.GetType("TarkovVR.Source.Compatibility.NativeTaskContinuation", true)
            .GetMethod("AfterCompletion", BindingFlags.Static | BindingFlags.NonPublic);
        Task Wrap(Task native, Action ready) => (Task)method.Invoke(null, new object[] { native, ready });

        bool called = false;
        var pending = new TaskCompletionSource<object>();
        var wrapped = Wrap(pending.Task, () => called = true);
        Check(!called && !wrapped.IsCompleted, "VR setup must wait for native menu initialization.");
        pending.SetResult(null);
        wrapped.GetAwaiter().GetResult();
        Check(called && wrapped.IsCompletedSuccessfully, "VR setup must run after native completion.");

        var failed = new TaskCompletionSource<object>();
        var failure = new InvalidOperationException("Native menu failure fixture");
        called = false;
        wrapped = Wrap(failed.Task, () => called = true);
        failed.SetException(failure);
        try { wrapped.GetAwaiter().GetResult(); throw new Exception("Native failure was swallowed."); }
        catch (InvalidOperationException ex) { Check(ReferenceEquals(ex, failure), "Original native exception must survive."); }
        Check(!called && wrapped.IsFaulted, "Failed native initialization must not run VR setup.");

        var canceled = new TaskCompletionSource<object>();
        called = false;
        wrapped = Wrap(canceled.Task, () => called = true);
        canceled.SetCanceled();
        try { wrapped.GetAwaiter().GetResult(); throw new Exception("Native cancellation was swallowed."); }
        catch (OperationCanceledException) { assertions++; }
        Check(!called && wrapped.IsCanceled, "Canceled native initialization must not run VR setup.");

        var callbackFailure = new InvalidOperationException("VR setup failure fixture");
        wrapped = Wrap(Task.CompletedTask, () => throw callbackFailure);
        try { wrapped.GetAwaiter().GetResult(); throw new Exception("Callback failure was swallowed."); }
        catch (InvalidOperationException ex) { Check(ReferenceEquals(ex, callbackFailure), "Callback failures must be observable by the caller."); }

        int immediateCalls = 0;
        wrapped = Wrap(Task.CompletedTask, () => immediateCalls++);
        wrapped.GetAwaiter().GetResult();
        Check(immediateCalls == 1, "An already completed native task must invoke VR setup once.");

        var previous = SynchronizationContext.Current;
        var context = new QueuedContext();
        var asynchronous = new TaskCompletionSource<object>();
        bool correctContext = false;
        int ownerThread = Environment.CurrentManagedThreadId;
        try
        {
            SynchronizationContext.SetSynchronizationContext(context);
            wrapped = Wrap(asynchronous.Task, () => correctContext =
                SynchronizationContext.Current == context && Environment.CurrentManagedThreadId == ownerThread);
            SynchronizationContext.SetSynchronizationContext(null);
            asynchronous.SetResult(null);
            Check(!wrapped.IsCompleted && context.Pending == 1, "Continuation must be posted back to the captured Unity context.");
            SynchronizationContext.SetSynchronizationContext(context);
            context.Drain();
            wrapped.GetAwaiter().GetResult();
            Check(correctContext, "Unity object setup must execute on the original synchronization context and thread.");
        }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
        Console.WriteLine($"Menu task regression: {assertions} assertions passed against the compiled plugin.");
    }

    private sealed class QueuedContext : SynchronizationContext
    {
        private readonly Queue<(SendOrPostCallback callback, object state)> queue = new();
        public int Pending => queue.Count;
        public override void Post(SendOrPostCallback callback, object state) => queue.Enqueue((callback, state));
        public void Drain()
        {
            while (queue.Count > 0)
            {
                var item = queue.Dequeue();
                item.callback(item.state);
            }
        }
    }
}
