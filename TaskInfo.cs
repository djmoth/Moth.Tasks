namespace Moth.Tasks
{
    using System;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    internal class TaskInfo
    {
        private RunOperation run;
        private DisposeOperation dispose;

        private TaskInfo (RunOperation run, DisposeOperation dispose, int id, Type type, int dataSize)
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

        private delegate void RunOperation (ref TaskQueue.TaskDataAccess access, TaskInfo task);

        private delegate void DisposeOperation (in TaskQueue.TaskDataAccess access, TaskInfo task);

        public int ID { get; }

        public string Name { get; }

        public Type Type { get; }

        public int DataSize { get; }

        public int DataIndices { get; }

        public bool Disposable { get; }

        public static unsafe TaskInfo Create<T> (int id) where T : struct, ITask
        {
            Type type = typeof (T);

            RunOperation run = null;
            DisposeOperation dispose = null;

            if (default (T) is IDisposable) // If T implements IDisposable
            {
                BindingFlags methodBindingFlags = BindingFlags.Static | BindingFlags.NonPublic;

                run = (RunOperation)typeof (TaskInfo).GetMethod (nameof (RunAndDispose), methodBindingFlags).MakeGenericMethod (type).CreateDelegate (typeof (RunOperation));

                dispose = (DisposeOperation)typeof (TaskInfo).GetMethod (nameof (Dispose), methodBindingFlags).MakeGenericMethod (type).CreateDelegate (typeof (DisposeOperation));
            } else
            {
                run = Run;
            }

            return new TaskInfo (Run, dispose, id, type, Unsafe.SizeOf<T> ());

            static void Run (ref TaskQueue.TaskDataAccess access, TaskInfo task)
            {
                T data = access.GetTaskData<T> (task);
                access.Dispose ();

                data.Run ();
            }

            static void RunAndDispose<U> (ref TaskQueue.TaskDataAccess access, TaskInfo task) where U : struct, ITask, IDisposable
            {
                U data = access.GetTaskData<U> (task);
                access.Dispose ();

                try
                {
                    data.Run ();
                } finally
                {
                   data.Dispose ();
                }
            }

            static void Dispose<U> (in TaskQueue.TaskDataAccess access, TaskInfo task) where U : struct, ITask, IDisposable
            {
                U data = access.GetTaskData<U> (task);

                data.Dispose ();
            }
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void Run (ref TaskQueue.TaskDataAccess access) => run (ref access, this);

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void Dispose (in TaskQueue.TaskDataAccess access) => dispose (in access, this);
    }
}
