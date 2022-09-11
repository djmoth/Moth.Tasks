namespace Moth.Tasks
{
    using Moth.IO.Serialization;
    using System;
    using System.Collections.Generic;

    internal class TaskReferenceStore
    {
        private object[] references;
        private int head;
        private int tail

        public TaskReferenceStore ()
        {
            Write = WriteImpl;
            Read = ReadImpl;
        }

        public ObjectWriter Write { get; }

        public ObjectReader Read { get; }

        private int WriteImpl (in object obj, Span<byte> destination)
        {
            references.Enqueue (obj);
            return 0;
        }

        private int ReadImpl (out object obj, Type type, ReadOnlySpan<byte> source)
        {
            obj = references.Dequeue ();
            return 0;
        }

        public void Clear () => references.Clear ();
    }
}
