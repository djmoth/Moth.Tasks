namespace Moth.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <inheritdoc />
    public class TaskHandleManager : ITaskHandleManager
    {
        private readonly Dictionary<int, ManualResetEventSlim> taskHandles = new Dictionary<int, ManualResetEventSlim> ();
        private int nextTaskHandle = 1;

        /// <inheritdoc />
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

        /// <inheritdoc />
        public bool IsTaskComplete (TaskHandle handle)
        {
            ThrowIfInvalidHandle (handle);

            lock (taskHandles)
            {
                return !taskHandles.ContainsKey (handle.ID);
            }
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public bool WaitForCompletion (TaskHandle handle, int millisecondsTimeout)
        {
            ThrowIfInvalidHandle (handle);

            ManualResetEventSlim waitEvent;
            bool complete;

            lock (taskHandles)
            {
                complete = !taskHandles.TryGetValue (handle.ID, out waitEvent);

                if (!complete && waitEvent == null)
                {
                    waitEvent = new ManualResetEventSlim ();
                    taskHandles[handle.ID] = waitEvent;
                }
            }

            if (!complete)
            {
                complete = waitEvent.Wait (millisecondsTimeout);
            }

            return complete;
        }

        /// <inheritdoc />
        public void NotifyTaskCompletion (TaskHandle handle)
        {
            ThrowIfInvalidHandle (handle);

            lock (taskHandles)
            {
                ManualResetEventSlim waitEvent = taskHandles[handle.ID];

                if (waitEvent != null)
                {
                    waitEvent.Set ();
                    waitEvent.Dispose ();
                }

                taskHandles.Remove (handle.ID);
            }
        }

        private void ThrowIfInvalidHandle (TaskHandle handle)
        {
            if (!handle.IsValid || handle.ID >= nextTaskHandle)
                throw new InvalidOperationException ($"{nameof (TaskHandle)} is invalid.");

            if (handle.Manager != this)
                throw new InvalidOperationException ($"{nameof (TaskHandle)} does not belong to this {nameof (TaskHandleManager)}.");
        }
    }
}
