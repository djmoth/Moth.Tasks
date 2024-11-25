using System.Collections.Generic;
using System.Threading;

namespace Moth.Tasks
{
    public class TaskHandleManager
    {
        private readonly Dictionary<int, ManualResetEventSlim> taskHandles = new Dictionary<int, ManualResetEventSlim> ();
        private int nextTaskHandle = 1;

        public TaskHandle CreateTaskHandle ()
        {
            lock (taskHandles)
            {
                int handleID = nextTaskHandle++;
                TaskHandle handle = new TaskHandle (this, handleID);

                taskHandles.Add (handleID, null);

                return handle;
            }
        }

        /// <summary>
        /// Check if a task has completed.
        /// </summary>
        /// <param name="handleID">ID of handle.</param>
        /// <returns><see langword="true"/> if task has completed, otherwise <see langword="false"/>.</returns>
        public bool IsTaskComplete (int handleID)
        {
            lock (taskHandles)
            {
                return !taskHandles.ContainsKey (handleID);
            }
        }

        public void Clear ()
        {
            lock (taskHandles)
            {
                foreach (ManualResetEventSlim waitEvent in taskHandles.Values)
                {
                    waitEvent?.Dispose ();
                }

                taskHandles.Clear ();
            }
        }

        /// <summary>
        /// Used by <see cref="TaskHandle.WaitForCompletion ()"/> to wait until task is complete.
        /// </summary>
        /// <param name="handleID">ID of handle.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or <see cref="System.Threading.Timeout.Infinite"/> (-1) to wait indefinitely.</param>
        /// <returns><see langword="true"/> if task was completed, <see langword="false"/> if timeout was reached.</returns>
        internal bool WaitForCompletion (int handleID, int millisecondsTimeout)
        {
            ManualResetEventSlim waitEvent;
            bool complete;

            lock (taskHandles)
            {
                complete = !taskHandles.TryGetValue (handleID, out waitEvent);

                if (!complete && waitEvent == null)
                {
                    waitEvent = new ManualResetEventSlim ();
                    taskHandles[handleID] = waitEvent;
                }
            }

            if (!complete)
            {
                complete = waitEvent.Wait (millisecondsTimeout);
            }

            return complete;
        }

        /// <summary>
        /// Used by <see cref="TaskWithHandle{T}"/> to notify callers of <see cref="WaitForCompletion(int, int)"/> that the task is done.
        /// </summary>
        /// <param name="handleID">ID of handle.</param>
        internal void NotifyTaskCompletion (int handleID)
        {
            lock (taskHandles)
            {
                ManualResetEventSlim waitEvent = taskHandles[handleID];

                if (waitEvent != null)
                {
                    waitEvent.Set ();
                    waitEvent.Dispose ();
                }

                taskHandles.Remove (handleID);
            }
        }
    }
}
