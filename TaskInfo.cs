namespace Moth.Tasks
{
    using System;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    internal class TaskInfo
    {
        private RunOperation run;
        private DisposeOperation dispose;

        private TaskInfo (RunOperation run, DisposeOperation dispose, int id, string name, int size)
        {
            this.run = run;
            this.dispose = dispose;
            ID = id;
            Name = name;

            // Round to number of IntPtr.Size data indices required to hold task data in a TaskQueue.
            DataIndices = (size + IntPtr.Size - 1) / IntPtr.Size;
        }

        private delegate void RunOperation (ref byte data);

        private delegate void DisposeOperation (ref byte data, bool cancelled);

        public int ID { get; }

        public string Name { get; }

        public int DataIndices { get; }

        public static unsafe TaskInfo Create<T> (int id) where T : struct, ITask
        {
            RunOperation run;
            DisposeOperation dispose;

            if (default (T) is IDisposableTask disposableBox) // If T implements IDisposable
            {
                dispose = (ref byte data, bool cancelled) =>
                {
                    Unsafe.Unbox<T> (disposableBox) = Unsafe.As<byte, T> (ref data); // Write task data to disposableBox object
                    disposableBox.Dispose (cancelled); // Invoke Dispose method on task data
                };

                run = (ref byte data) =>
                {
                    try
                    {
                        Unsafe.As<byte, T> (ref data).Run ();
                    } finally
                    {
                        dispose (ref data, false);
                    }
                };
            } else
            {
                run = (ref byte data) => Unsafe.As<byte, T> (ref data).Run ();
                dispose = null;
            }

            Type type = typeof (T);

            return new TaskInfo (run, dispose, id, type.FullName, Unsafe.SizeOf<T> ());
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void Run (ref byte data) => run (ref data);

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void Cancel (ref byte data) => dispose?.Invoke (ref data, true);
    }
}
