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

        private delegate void TaskOperation (ref TaskQueue.TaskDataAccess data, TaskInfo task);

        public int ID { get; }

        public string Name { get; }

        public Type Type { get; }

        public int DataSize { get; }

        public int DataIndices { get; }

        public bool Disposable { get; }

        public static unsafe TaskInfo Create<T> (int id) where T : struct, ITask
        {
            Type type = typeof (T);

            TaskOperation run = null, dispose = null;

            if (default (T) is IDisposable) // If T implements IDisposable
            {
                BindingFlags methodBindingFlags = BindingFlags.Static | BindingFlags.NonPublic;

                run = (TaskOperation)typeof (TaskInfo).GetMethod (nameof (RunAndDispose), methodBindingFlags).MakeGenericMethod (type).CreateDelegate (typeof (TaskOperation));

                dispose = (TaskOperation)typeof (TaskInfo).GetMethod (nameof (DisposeImpl), methodBindingFlags).MakeGenericMethod (type).CreateDelegate (typeof (TaskOperation));
            } else
            {
                run = Run;
            }

            return new TaskInfo (Run, dispose, id, type, Unsafe.SizeOf<T> ());

            void Run (ref TaskQueue.TaskDataAccess access, TaskInfo task) => access.GetTaskData<T> (task).Run ();
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void Run (ref TaskQueue.TaskDataAccess access) => run (ref access, this);

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void Dispose (TaskQueue.TaskDataAccess access) => dispose (ref access, this);

        private static void RunAndDispose<T> (ref TaskQueue.TaskDataAccess access, TaskInfo task) where T : struct, ITask, IDisposable
        {
            T data = access.GetTaskData<T> (task);

            try
            {
                data.Run ();
            } finally
            {
                data.Dispose ();
            }
        }

        private static void DisposeImpl<T> (ref TaskQueue.TaskDataAccess access, TaskInfo task) where T : struct, ITask, IDisposable => access.GetTaskData<T> (task).Dispose ();
    }
}
