namespace Moth.Tasks
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public sealed unsafe class TaskQueue : IDisposable
    {
        private readonly object taskLock = new object ();
        private readonly TaskCache taskCache = new TaskCache ();
        private object[] tasks;
        private int firstTask;
        private int lastTaskEnd;
        private bool hasDisposableTasks;

        public TaskQueue (int startCapacity)
        {
            if (startCapacity < 0)
            {
                throw new ArgumentOutOfRangeException (nameof (startCapacity), $"{nameof (startCapacity)} must be larger than or equal to zero.");
            }

            tasks = new object[startCapacity];
        }

        public void Clear ()
        {
            lock (taskLock)
            {
                firstTask = 0;
                lastTaskEnd = 0;
            }
        }

        public void Enqueue (ITask task) => Enqueue (new ManagedTask (task));

        public void Enqueue<T> (T task) where T : struct, ITask
        {
            lock (taskLock)
            {
                Task taskInfo = taskCache.GetTask<T> ();

                int totalDataSize = taskInfo.DataSize + sizeof (ushort);

                if (lastTaskEnd + totalDataSize > tasks.Length * IntPtr.Size)
                {
                    int taskDataLength = lastTaskEnd - firstTask;

                    if (taskDataLength + totalDataSize > tasks.Length * IntPtr.Size)
                    {
                        Array.Resize (ref tasks, tasks.Length * 2);
                    }

                    if (firstTask != 0)
                    {
                        Buffer.BlockCopy (tasks, firstTask, tasks, 0, taskDataLength); // Move tasks to front

                        lastTaskEnd -= firstTask;
                        firstTask = 0;
                    }
                }

                ref byte rawTasks = ref Unsafe.As<object, byte> (ref tasks[0]);
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
            Task task;

            lock (taskLock)
            {
                if (firstTask == lastTaskEnd)
                {
                    return false;
                }

                ref byte taskRef = ref Unsafe.As<object, byte> (ref tasks[0]);
                taskRef = ref Unsafe.Add (ref taskRef, firstTask);

                ref byte idRef = ref Unsafe.Add (taskRef, )

                int id = Unsafe.ReadUnaligned<ushort> (ref tasks[firstTask]);

                task = taskCache.GetTask (id);

                byte* data = stackalloc byte[task.DataSize];
                taskData = data;

                tasks.AsSpan (firstTask + sizeof (ushort), task.DataSize).CopyTo (new Span<byte> (data, task.DataSize)); // Copy Task data to stack

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
            Unmanaged.Free (ref tasks);

            if (!fromDispose)
                Debug.LogWarning ("TaskQueue disposed from Finalizer!");
        }

        private void Reset ()
        {
            firstTask = 0;
            lastTaskEnd = 0;
            hasDisposableTasks = false;
        }
    }
}
