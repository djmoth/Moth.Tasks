namespace Moth.Tasks
{
    using Moth.IO.Serialization;
    using System;
    using System.Runtime.CompilerServices;
    using Validation;

    /// <summary>
    /// Stores task data.
    /// </summary>
    internal class TaskDataStore : ITaskDataStore
    {
        private readonly ITaskReferenceStore taskReferenceStore;
        private byte[] taskData;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskDataStore"/> class with a specified data starting capacity.
        /// </summary>
        /// <param name="dataCapacity">Starting capacity of unmanaged task data.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="dataCapacity"/> is less than zero.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="taskReferenceStore"/> is <see langword="null"/>.</exception>"
        public TaskDataStore (int dataCapacity, ITaskReferenceStore taskReferenceStore)
        {
            Requires.Range (dataCapacity >= 0, nameof (dataCapacity));
            Requires.NotNull (taskReferenceStore, nameof (taskReferenceStore));

            taskData = new byte[dataCapacity];
            this.taskReferenceStore = taskReferenceStore;
        }

        /// <inheritdoc/>
        public int FirstTask { get; private set; }

        /// <inheritdoc/>
        public int LastTaskEnd { get; private set; }

        /// <inheritdoc/>
        public int Size => LastTaskEnd - FirstTask;

        /// <inheritdoc/>
        public int Capacity => taskData.Length;

        /// <inheritdoc/>
        public void Enqueue<T> (in T task, ITaskInfo<T> taskInfo)
            where T : struct, ITaskType
        {
            // If new task data will overflow the taskData array
            CheckCapacity (taskInfo.UnmanagedSize);

            taskInfo.Serialize (task, taskData.AsSpan (LastTaskEnd), taskReferenceStore.Write);

            LastTaskEnd += taskInfo.UnmanagedSize;
        }

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException"><see cref="Size"/> is zero.</exception>
        public T Dequeue<T> (ITaskInfo<T> taskInfo)
            where T : struct, ITaskType
        {
            if (Size == 0)
                throw new InvalidOperationException ("Cannot Dequeue as TaskDataStore.Size is zero.");

            taskInfo.Deserialize (out T task, taskData.AsSpan (FirstTask), taskReferenceStore.Read);

            FirstTask += taskInfo.UnmanagedSize;

            if (FirstTask == LastTaskEnd) // If firstTask is now equal to lastTaskEnd, then this was the last task in the queue
            {
                FirstTask = 0;
                LastTaskEnd = 0;
            }

            return task;
        }

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException"><see cref="Size"/> is zero.</exception>
        public void Skip (ITaskInfo taskInfo)
        {
            if (Size == 0)
                throw new InvalidOperationException ("Cannot Skip as TaskDataStore.Size is zero.");

            FirstTask += taskInfo.UnmanagedSize;

            if (FirstTask == LastTaskEnd)
            {
                FirstTask = 0;
                LastTaskEnd = 0;
            }

            if (taskInfo.IsManaged)
                taskReferenceStore.Skip (taskInfo.ReferenceCount);
        }

        /// <inheritdoc/>
        public void Insert<T> (ref int dataIndex, ref int refIndex, in T task, ITaskInfo<T> taskInfo)
            where T : struct, ITaskType
        {
            if (dataIndex < 0 || dataIndex > LastTaskEnd)
                throw new ArgumentOutOfRangeException (nameof (dataIndex));

            if (refIndex < 0 || refIndex > taskReferenceStore.Count)
                throw new ArgumentOutOfRangeException (nameof (refIndex));

            if (dataIndex == LastTaskEnd)
            {
                Enqueue (task, taskInfo);
                return;
            }

            // If inserting at the beginning of the store and there is enough space before the first task
            if (dataIndex == FirstTask && dataIndex - taskInfo.UnmanagedSize > 0)
            {
                // Insert new task data before the first task without moving any data
                FirstTask -= taskInfo.UnmanagedSize;
                dataIndex = FirstTask;
            } else
            {
                CheckCapacity (taskInfo.UnmanagedSize);

                int copyDestination = dataIndex + taskInfo.UnmanagedSize;
                int copySource = dataIndex;
                int byteCount = LastTaskEnd - copyDestination;

                Unsafe.CopyBlockUnaligned (ref taskData[copyDestination], ref taskData[copySource], (uint)byteCount);

                LastTaskEnd += taskInfo.UnmanagedSize;
            }

            using (var insertContext = taskReferenceStore.EnterInsertContext (refIndex, taskInfo.ReferenceCount, out ObjectWriter insertWriter))
            {
                taskInfo.Serialize (task, taskData.AsSpan (dataIndex), insertWriter);
            }
        }

        /// <inheritdoc/>
        public void Clear ()
        {
            FirstTask = 0;
            LastTaskEnd = 0;

            taskReferenceStore.Clear ();
        }

        private void CheckCapacity (int unmanagedSize)
        {
            if (LastTaskEnd + unmanagedSize > taskData.Length)
            {
                int totalTaskDataLength = LastTaskEnd - FirstTask;

                // If there is not enough total space in taskData array to hold new task, then resize taskData
                if (totalTaskDataLength + unmanagedSize > taskData.Length)
                {
                    // If unmanagedSize is abnormally large, doubling the size might not always be enough
                    int newSize = Math.Max (taskData.Length * 2, totalTaskDataLength + unmanagedSize);
                    Array.Resize (ref taskData, newSize);
                }

                if (FirstTask != 0)
                {
                    Array.Copy (taskData, FirstTask, taskData, 0, totalTaskDataLength); // Move tasks to the beginning of taskData, to eliminate wasted space

                    LastTaskEnd = totalTaskDataLength;
                    FirstTask = 0;
                }
            }
        }
    }
}
