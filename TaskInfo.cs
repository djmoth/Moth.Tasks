namespace Moth.Tasks
{
    using System;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    internal class TaskInfo
    {
        private RunTask run;
        private DisposeTask dispose;

        private TaskInfo (RunTask run, DisposeTask dispose, int id, string name, int dataSize)
        {
            this.run = run;
            this.dispose = dispose;
            ID = id;
            Name = name;
            DataSize = dataSize;
        }

        public int ID { get; }

        public string Name { get; }

        public int DataSize { get; }

        public static unsafe TaskInfo Create<T> (int id) where T : struct, ITask
        {
            RunTask run = (ref byte data) => Unsafe.As<byte, T> (ref data).Run ();

            DisposeTask dispose;

            if (default (T) is IDisposable) // If T implements IDisposable
            {
                dispose = ((IDisposable)default (T)).Dispose;
            } else
            {
                dispose = null;
            }

            Type type = typeof (T);

            return new TaskInfo (run, dispose, id, type.FullName, Unsafe.SizeOf<T> (), IsUnmanaged (type));
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void Run (ref byte data) => run (ref data);

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void Dispose (ref byte data)
        {
            Unsafe.Unbox<T> (dispose.Target) = Unsafe.As<byte, T> (ref data); // Write task data to asDisposable object
            asDisposable.Dispose (); // Call Dispose with task data
        }

        private class DisposableBox<T> : IDisposable where T : struct, IDisposable
        {
            private T data;

            private 
        }
    }

    internal unsafe delegate void RunTask (ref byte data);

    internal delegate void DisposeTask ();
}
