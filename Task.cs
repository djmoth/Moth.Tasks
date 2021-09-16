namespace Moth.Tasks
{
    using System;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    internal class Task
    {
        private RunTask run;
        private DisposeTask dispose;
        private IDisposable disposableObject;

        public Task (RunTask run, DisposeTask dispose, int id, string name, int dataSize, bool unmanaged)
        {
            this.run = run;
            this.dispose = dispose;
            ID = id;
            Name = name;
            DataSize = dataSize;
            Unmanaged = unmanaged;
        }

        public int ID { get; }

        public string Name { get; }

        public int DataSize { get; }

        public bool Unmanaged { get; }

        public static unsafe Task Create<T> (int id) where T : struct, ITask
        {
            RunTask run = (ref byte data) => Unsafe.As<byte, T> (ref data).Run ();

            DisposeTask dispose;

            if (default (T) is IDisposable) // If T implements IDisposable
            {
                IDisposable asDisposable = (IDisposable)default (T);

                dispose = (ref byte data) =>
                {
                    Unsafe.Unbox<T> (asDisposable) = Unsafe.As<byte, T> (ref data); // Write task data to asDisposable object
                    asDisposable.Dispose (); // Call Dispose with task data
                };
            } else
            {
                dispose = null;
            }

            Type type = typeof (T);

            return new Task (run, dispose, id, type.FullName, Unsafe.SizeOf<T> (), IsUnmanaged (type));
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void Run (ref byte data) => run (ref data);

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void Dispose (ref byte data) => dispose?.Invoke (ref data);

        private static DisposeTask GetDisposeIfDisposable<T> () where T : struct, ITask
        {
            if (typeof (T).IsAssignableFrom (typeof (IDisposable)))
            {
                T source = default;

                object boxed = source;

                ref T boxedRef = ref Unsafe.Unbox<T> (boxed);


            }
        }

        private static bool IsUnmanaged (Type type)
        {
            return CheckFields (type);

            bool CheckFields (Type t)
            {
                foreach (FieldInfo fieldInfo in t.GetFields (BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    Type fieldType = fieldInfo.FieldType;

                    if (fieldType.IsPrimitive || fieldType.IsPointer || fieldType.IsEnum)
                    {
                        continue;
                    }

                    if (!fieldType.IsValueType || (!fieldType.IsPrimitive && !CheckFields (fieldType)))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }

    internal unsafe delegate void RunTask (ref byte data);

    internal delegate void DisposeTask (ref byte task);
}
