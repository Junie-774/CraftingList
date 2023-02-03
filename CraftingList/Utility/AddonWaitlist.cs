using Dalamud.Logging;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CraftingList.Utility
{
    public class AddonWaitlist : IDisposable
    {
        public struct AddonWaitInfo
        {
            public string Name;
            public bool NeedsToBeVisible;
            public ulong TimeOut;
            public TaskCompletionSource<IntPtr> TaskCompletionSource;
            public Func<string, IntPtr> PollResult;
            public Func<IntPtr, bool, bool> CheckResult;

            public AddonWaitInfo(string name, bool needsVisible, ulong start, int timeoutMS, Func<string, IntPtr> pollResult, Func<IntPtr, bool, bool> checkResult,
                TaskCompletionSource<IntPtr> completionSource)
            {
                Name = name;
                NeedsToBeVisible = needsVisible;
                TimeOut = start + (ulong)timeoutMS;
                PollResult = pollResult;
                CheckResult = checkResult;
                TaskCompletionSource = completionSource;
            }
        }

        List<AddonWaitInfo> Waitlist = new();

        public Task<IntPtr> Add(string name, bool needsVisible, int timeoutMs, Func<string, IntPtr> pollResult, Func<IntPtr, bool, bool> checkResult)
        {
            var initialResult = pollResult(name);
            var completionSource = new TaskCompletionSource<IntPtr>();

            //Initial check, skip doing waitlisting if the addon is already ready
            if (checkResult(initialResult, needsVisible))
            {
                completionSource.SetResult(initialResult);
                return completionSource.Task;
            }

            var start = (ulong)DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            var waitInfo = new AddonWaitInfo(name, needsVisible, start, timeoutMs, pollResult, checkResult, completionSource);
            lock (Waitlist)
            {
                if (Waitlist.Count == 0)
                {
                    Service.Framework.Update += OnFrameworkUpdate;
                }
                Waitlist.Add(waitInfo);
            }

            return completionSource.Task;
        }

        public void Dispose()
        {
            Service.Framework.Update -= OnFrameworkUpdate;
            foreach (var waitInfo in Waitlist)
            {
                waitInfo.TaskCompletionSource.SetCanceled();
            }
            Waitlist.Clear();
        }

        void OnFrameworkUpdate(object _)
        {
            foreach (AddonWaitInfo waitInfo in Waitlist)
            {
                var currTime = (ulong)DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

                if (waitInfo.TimeOut < currTime)
                {
                    PluginLog.Error($"Addon {waitInfo.Name} timed out");
                    waitInfo.TaskCompletionSource.SetCanceled();
                    continue;
                }
                var res = waitInfo.PollResult(waitInfo.Name);

                if (waitInfo.CheckResult(res, waitInfo.NeedsToBeVisible))
                {
                    waitInfo.TaskCompletionSource.SetResult(res);
                }
            }

            Waitlist.RemoveAll(waitInfo => waitInfo.TaskCompletionSource.Task.IsCompleted || waitInfo.TaskCompletionSource.Task.IsCanceled);
                        if (Waitlist.Count == 0)
            {
                Service.Framework.Update -= OnFrameworkUpdate;
            }
        }
    }
}
