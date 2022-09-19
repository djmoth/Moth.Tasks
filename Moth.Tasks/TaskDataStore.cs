using System;
using System.Runtime.CompilerServices;

namespace Moth.Tasks
{
    internal class TaskDataStore
    {
        private readonly TaskReferenceStore taskReferenceStore = new TaskReferenceStore ();
        private byte[] taskData;
        private int firstTask;
        private int lastTaskEnd;

        public void Enqueue<T> (T task, TaskInfo<T> taskInfo) where T : struct, ITask
        {
            // If new task data will overflow the taskData array
            CheckCapacity (taskInfo.UnmanagedSize);

            taskInfo.Serialize (task, taskData.AsSpan (lastTaskEnd), taskReferenceStore);

            lastTaskEnd += taskInfo.UnmanagedSize;
        }

        public T Dequeue<T> (TaskInfo<T> taskInfo) where T : struct, ITask
        {
            taskInfo.Deserialize (out T task, taskData.AsSpan (firstTask), taskReferenceStore);

            firstTask += taskInfo.UnmanagedSize;

            if (firstTask == lastTaskEnd) // If firstTask is now equal to lastTaskEnd, then this was the last task in the queue
            {
                firstTask = 0;
                lastTaskEnd = 0;
            }

            return task;
        }

        public void Insert<T> (int dataIndex, int refIndex, T task, TaskInfo<T> taskInfo) where T : struct, ITask
        {
            CheckCapacity (taskInfo.UnmanagedSize);

            int copyDestination = dataIndex + taskInfo.UnmanagedSize;
            int copySource = dataIndex;
            int byteCount = lastTaskEnd - copyDestination;

            Unsafe.CopyBlockUnaligned (ref taskData[copyDestination], ref taskData[copySource], (uint)byteCount);

            using (var insertContext = taskReferenceStore.EnterInsertContext (refIndex, taskInfo.ReferenceCount))
            {
                taskInfo.Serialize (task, taskData.AsSpan (dataIndex), taskReferenceStore);
            }

            lastTaskEnd += taskInfo.UnmanagedSize;
        }

        private void CheckCapacity (int unmanagedSize)
        {
            if (lastTaskEnd + unmanagedSize > taskData.Length)
            {
                int totalTaskDataLength = lastTaskEnd - firstTask;

                // If there is not enough total space in taskData array to hold new task, then resize taskData
                if (totalTaskDataLength + unmanagedSize > taskData.Length)
                {
                    // If taskInfo.DataIndices is abnormally large, doubling the size might not always be enough
                    int newSize = Math.Max (taskData.Length * 2, totalTaskDataLength + unmanagedSize);
                    Array.Resize (ref taskData, newSize);
                }

                if (firstTask != 0)
                {
                    Array.Copy (taskData, firstTask, taskData, 0, totalTaskDataLength); // Move tasks to the beginning of taskData, to eliminate wasted space

                    lastTaskEnd = totalTaskDataLength;
                    firstTask = 0;
                }
            }
        }
    }
}
