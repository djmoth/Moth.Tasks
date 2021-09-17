namespace Moth.Tasks
{
    using System;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    internal class TaskInfo
    {
        private TaskOperation run;
        private TaskOperation dispose;

        private TaskInfo (TaskOperation run, TaskOperation dispose, int id, string name, int size)
        {
            this.run = run;
            this.dispose = dispose;
            ID = id;
            Name = name;

            // Round size up to the highest multiple of IntPtr.Size
            DataSize = ((size + IntPtr.Size - 1) / IntPtr.Size) * IntPtr.Size;
        }

        public int ID { get; }

        public string Name { get; }

        public int DataSize { get; }

        public static unsafe TaskInfo Create<T> (int id) where T : struct, ITask
        {
            TaskOperation run = (ref byte data) => Unsafe.As<byte, T> (ref data).Run ();

            TaskOperation dispose = null;

            if (default (T) is IDisposable) // If T implements IDisposable
            {
                IDisposable disposableBox = (IDisposable)default (T);

                dispose = (ref byte data) =>
                {
                    Unsafe.Unbox<T> (disposableBox) = Unsafe.As<byte, T> (ref data); // Write task data to disposableBox object
                    disposableBox.Dispose (); // Invoke Dispose method on task data
                };
            }

            Type type = typeof (T);

            return new TaskInfo (run, dispose, id, type.FullName, Unsafe.SizeOf<T> ());
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void Run (ref byte data) => run (ref data);

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void Dispose (ref byte data) => dispose (ref data);

        private delegate void TaskOperation (ref byte data);
    }
}
