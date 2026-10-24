using System;
using System.Threading.Tasks;

namespace TarkovVR.Source.Compatibility
{
    internal static class NativeTaskContinuation
    {
        // Keep Unity's synchronization context and the native task's failures.
        // Harmony postfixes run when an async method returns its Task, not when it finishes.
        internal static async Task AfterCompletion(Task nativeTask, Action onCompleted)
        {
            await nativeTask;
            onCompleted();
        }
    }
}
