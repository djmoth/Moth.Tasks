namespace Moth.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public sealed unsafe class TaskQueue : IDisposable
    {
        private readonly object taskLock = new object ();
        private readonly TaskCache taskCache = new TaskCache ();
        private readonly Queue<int> tasks = new Queue<int> ();
        private object[] taskData;
        private int firstTask;
        private int lastTaskEnd;
        private bool hasDisposableTasks;

        public TaskQueue (int startCapacity)
        {
            if (startCapacity < 0)
            {
                throw new ArgumentOutOfRangeException (nameof (startCapacity), $"{nameof (startCapacity)} must be larger than or equal to zero.");
            }

            taskData = new object[startCapacity];
        }

        public void Clear ()
        {
            lock (taskLock)
            {
                tasks.Clear ();

                firstTask = 0;
                lastTaskEnd = 0;
            }
        }

        public void Enqueue (Action action) => Enqueue (new DelegateTask (action));

        public void Enqueue<T> (Action<T> action, T arg) => Enqueue (new DelegateTask<T> (action, arg));

        public void Enqueue<T1, T2> (Action<T1, T2> action, T1 arg1, T2 arg2) => Enqueue (new DelegateTask<T1, T2> (action, arg1, arg2));

        public void Enqueue<T1, T2, T3> (Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3) => Enqueue (new DelegateTask<T1, T2, T3> (action, arg1, arg2, arg3));

        public void Enqueue<T> (T task) where T : struct, ITask
        {
            lock (taskLock)
            {
                TaskInfo taskInfo = taskCache.GetTask<T> ();

                int totalDataSize = taskInfo.DataSize + sizeof (ushort);

                if (lastTaskEnd + totalDataSize > taskData.Length * IntPtr.Size)
                {
                    int taskDataLength = lastTaskEnd - firstTask;

                    if (taskDataLength + totalDataSize > taskData.Length * IntPtr.Size)
                    {
                        Array.Resize (ref taskData, taskData.Length * 2);
                    }

                    if (firstTask != 0)
                    {
                        Buffer.BlockCopy (taskData, firstTask, taskData, 0, taskDataLength); // Move tasks to front

                        lastTaskEnd -= firstTask;
                        firstTask = 0;
                    }
                }

                ref byte rawTasks = ref Unsafe.As<object, byte> (ref taskData[0]);
                ref byte newTask = ref Unsafe.Add (ref rawTasks, lastTaskEnd);

                if (!taskInfo.Unmanaged)
                {
                    long newTaskAddress = (long)Unsafe.AsPointer (ref newTask);

                    if (newTaskAddress % IntPtr.Size != 0) // If address is not aligned correctly
                    {

                    }
                }

                Unsafe.WriteUnaligned (ref Unsafe.Add (ref newTask, sizeof (ushort)), task); // Write Task Data

                Unsafe.WriteUnaligned (ref newTask, (ushort)taskID); // Write Task ID



                lastTaskEnd += totalDataSize;

                if (task is IDisposable)
                {
                    hasDisposableTasks = true;
                }
            }
        }

        public bool TryRunNextTask () => TryRunNextTask (null);

        public bool TryRunNextTask (IProfiler profiler)
        {
            byte* taskData;
            TaskInfo task;

            lock (taskLock)
            {
                if (firstTask == lastTaskEnd)
                {
                    return false;
                }

                ref byte taskRef = ref Unsafe.As<object, byte> (ref this.taskData[0]);
                taskRef = ref Unsafe.Add (ref taskRef, firstTask);

                ref byte idRef = ref Unsafe.Add (taskRef, )

                int id = Unsafe.ReadUnaligned<ushort> (ref this.taskData[firstTask]);

                task = taskCache.GetTask (id);

                byte* data = stackalloc byte[task.DataSize];
                taskData = data;

                this.taskData.AsSpan (firstTask + sizeof (ushort), task.DataSize).CopyTo (new Span<byte> (data, task.DataSize)); // Copy Task data to stack

                firstTask += task.DataSize + sizeof (ushort);

                if (firstTask == lastTaskEnd)
                {
                    firstTask = 0;
                    lastTaskEnd = 0;
                }
            }

            profiler?.BeginTask (task.Name);

            task.Run (taskData);

            profiler?.EndTask ();

            return true;
        }

        public void Dispose ()
        {
            Dispose (true);
            GC.SuppressFinalize (this);
        }

        private void Dispose (bool fromDispose)
        {
            
        }

        private void Reset ()
        {
            firstTask = 0;
            lastTaskEnd = 0;
            hasDisposableTasks = false;
        }
    }
}
