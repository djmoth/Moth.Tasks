namespace Moth.Tasks
{
    using System;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    internal class TaskInfo
    {
        private TaskOperation run;
        private TaskOperation dispose;

        private TaskInfo (TaskOperation run, TaskOperation dispose, int id, Type type, int dataSize)
        {
            this.run = run;
            this.dispose = dispose;
            ID = id;
            Name = type.FullName;
            Type = type;

            DataSize = dataSize;

            // Round dataSize to number of IntPtr.Size data indices required to hold task data in a TaskQueue.
            DataIndices = (dataSize + IntPtr.Size - 1) / IntPtr.Size;

            Disposable = dispose != null;
        }

        private delegate void TaskOperation (TaskQueue.TaskDataAccess data, TaskInfo task);

        public int ID { get; }

        public string Name { get; }

        public Type Type { get; }

        public int DataSize { get; }

        public int DataIndices { get; }

        public bool Disposable { get; }

        public static unsafe TaskInfo Create<T> (int id) where T : struct, ITask
        {
            TaskOperation run = (access, task) =>
            {
                T data;

                using (access)
                {
                    data = access.GetTaskData<T> (task);
                }

                data.Run ();
            };

            TaskOperation dispose = null;

            if (default (T) is IDisposable disposableBox) // If T implements IDisposable
            {
                dispose = (data, task) =>
                {
                    Unsafe.Unbox<T> (disposableBox) = data.GetTaskData<T> (task); // Write task data to disposableBox object
                    disposableBox.Dispose (); // Invoke Dispose method on task data
                };
            }

            Type type = typeof (T);

            return new TaskInfo (run, dispose, id, type, Unsafe.SizeOf<T> ());
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void Run (TaskQueue.TaskDataAccess data) => run (data, this);

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void Dispose (TaskQueue.TaskDataAccess data) => dispose (data, this);
    }
}
