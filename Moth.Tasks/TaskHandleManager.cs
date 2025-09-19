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

        /// <summary>
        /// Gets the number of active task handles that are not yet completed.
        /// </summary>
        public int ActiveHandles
        {
            get
            {
                lock (taskHandles)
                {
                    return taskHandles.Count;
                }
            }
        }

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
        /// <exception cref="ArgumentException"><paramref name="handle"/> is an invalid <see cref="TaskHandle"/>.</exception>
        public bool IsTaskComplete (TaskHandle handle)
        {
            ThrowIfInvalidHandle (handle);

            lock (taskHandles)
            {
                return !taskHandles.ContainsKey (handle.ID);
            }
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentException"><paramref name="handle"/> is an invalid <see cref="TaskHandle"/>.</exception>
        public bool WaitForCompletion (TaskHandle handle, int millisecondsTimeout, CancellationToken token = default)
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
                complete = waitEvent.Wait (millisecondsTimeout, token);
            }

            return complete;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentException"><paramref name="handle"/> is an invalid <see cref="TaskHandle"/>.</exception>
        /// <exception cref="InvalidOperationException">Task handle has already been completed.</exception>
        public void NotifyTaskCompletion (TaskHandle handle)
        {
            ThrowIfInvalidHandle (handle);

            lock (taskHandles)
            {
                if (!taskHandles.TryGetValue (handle.ID, out ManualResetEventSlim waitEvent))
                    throw new InvalidOperationException ("Task handle has already been completed.");

                if (waitEvent != null)
                {
                    waitEvent.Set ();
                    waitEvent.Dispose ();
                }

                taskHandles.Remove (handle.ID);
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

        private void ThrowIfInvalidHandle (TaskHandle handle)
        {
            if (!handle.IsValid || handle.ID >= nextTaskHandle)
                throw new ArgumentException ($"{nameof (TaskHandle)} is invalid.");

            if (handle.Manager != this)
                throw new ArgumentException ($"{nameof (TaskHandle)} does not belong to this {nameof (TaskHandleManager)}.");
        }
    }
}
