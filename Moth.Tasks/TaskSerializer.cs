using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Moth.Tasks
{
    internal static class TaskSerializer
    {
        private static readonly Module Module = typeof (TaskSerializer).Module;

        private static readonly Dictionary<Type, FieldOffsets> fieldOffsets;
        

        public static void Generate<T> (out TaskInfo<T>.Write write, out TaskInfo<T>.Read read, out int unmanagedSize, out int referenceFields) where T : struct, ITask
        {
            Type type = typeof (T);

            if (!RuntimeHelpers.IsReferenceOrContainsReferences<T> ())
            {
                write = (in T task, out byte destination, Queue<object> references) =>
                {
                    Unsafe.SkipInit (out destination);
                    Unsafe.WriteUnaligned (ref destination, task);
                };

                read = (out T task, in byte source, Queue<object> references) =>
                {
                    task = Unsafe.ReadUnaligned<T> (ref Unsafe.AsRef (source));
                };

                unmanagedSize = Unsafe.SizeOf<T> ();
                referenceFields = 0;

                return;
            }

            Type[] parameters = { typeof (T).MakeByRefType (), typeof (byte).MakeByRefType (), typeof (Queue<object>) };

            DynamicMethod writeSource = new DynamicMethod (type.FullName + "_Write", typeof (void), parameters, Module, true);
            DynamicMethod readSource = new DynamicMethod (type.FullName + "_Read", typeof (void), parameters, Module, true);

            List<Field> fields = new List<Field> ();

            void GetFields ()
            {
                foreach (FieldInfo field in type.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {

                }
            }

           
        }

        private struct Field
        {
            public int Offset { get; }

            public int Size { get; }

            public bool IsReference { get; }
        }

        private class FieldOffsets
        {

        }
    }
}
