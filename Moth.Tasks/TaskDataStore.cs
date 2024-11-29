namespace Moth.Tasks
{
    using Moth.IO.Serialization;
    using System;
    using System.Runtime.CompilerServices;

    internal class TaskDataStore
    {
        private readonly TaskReferenceStore taskReferenceStore = new TaskReferenceStore ();
        private byte[] taskData;

        public TaskDataStore (int dataCapacity)
        {
            taskData = new byte[dataCapacity];
        }

        public int FirstTask { get; private set; }

        public int LastTaskEnd { get; private set; }

        public int Size => LastTaskEnd - FirstTask;

        public void Enqueue<T> (T task, ITaskInfo<T> taskInfo)
            where T : struct, ITaskType
        {
            // If new task data will overflow the taskData array
            CheckCapacity (taskInfo.UnmanagedSize);

            taskInfo.Serialize (task, taskData.AsSpan (LastTaskEnd), taskReferenceStore.Write);

            LastTaskEnd += taskInfo.UnmanagedSize;
        }

        public T Dequeue<T> (ITaskInfo<T> taskInfo)
            where T : struct, ITaskType
        {
            taskInfo.Deserialize (out T task, taskData.AsSpan (FirstTask), taskReferenceStore.Read);

            FirstTask += taskInfo.UnmanagedSize;

            if (FirstTask == LastTaskEnd) // If firstTask is now equal to lastTaskEnd, then this was the last task in the queue
            {
                FirstTask = 0;
                LastTaskEnd = 0;
            }

            return task;
        }

        public void Skip (ITaskInfo taskInfo)
        {
            FirstTask += taskInfo.UnmanagedSize;

            if (FirstTask == LastTaskEnd)
            {
                FirstTask = 0;
                LastTaskEnd = 0;
            }

            if (taskInfo.IsManaged)
                taskReferenceStore.Skip (taskInfo.ReferenceCount);
        }

        public void Insert<T> (int dataIndex, int refIndex, T task, ITaskInfo<T> taskInfo)
            where T : struct, ITaskType
        {
            CheckCapacity (taskInfo.UnmanagedSize);

            int copyDestination = dataIndex + taskInfo.UnmanagedSize;
            int copySource = dataIndex;
            int byteCount = LastTaskEnd - copyDestination;

            Unsafe.CopyBlockUnaligned (ref taskData[copyDestination], ref taskData[copySource], (uint)byteCount);

            using (var insertContext = taskReferenceStore.EnterInsertContext (refIndex, taskInfo.ReferenceCount, out ObjectWriter insertWriter))
            {
                taskInfo.Serialize (task, taskData.AsSpan (dataIndex), insertWriter);
            }

            LastTaskEnd += taskInfo.UnmanagedSize;
        }

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
                    // If taskInfo.DataIndices is abnormally large, doubling the size might not always be enough
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
