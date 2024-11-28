namespace Moth.Tasks
{
    using Moth.IO.Serialization;
    using System;
    using System.Collections.Generic;

    public class TaskReferenceStore
    {
        private object[] references = new object[32];
        private int start;
        private int end;
        private int insertIndex = -1;

        private ObjectWriter write;
        private ObjectWriter insert;

        public TaskReferenceStore ()
        {
            write = WriteImpl;
            insert = InsertImpl;

            Read = ReadImpl;
        }

        public ObjectWriter Write => insertIndex == -1 ? write : insert;

        public ObjectReader Read { get; }

        public InsertContext EnterInsertContext (int insertIndex, int refCount)
        {
            CheckCapacity (refCount);

            int countAboveInsertIndex = end - insertIndex;

            Array.Copy (references, insertIndex, references, insertIndex + refCount, countAboveInsertIndex); // Move elements above insert area

            this.insertIndex = insertIndex;

            return new InsertContext (this);
        }

        public void Clear ()
        {
            for (int i = start; i < end; i++)
            {
                references[i] = null;
            }

            start = 0;
            end = 0;
        }

        public void Skip (int refCount)
        {
            // Clear references
            for (int i = 0; i < refCount; i++)
            {
                references[start + i] = null;
            }

            start += refCount;

            if (start == end)
            {
                start = 0;
                end = 0;
            }
        }

        private int WriteImpl (in object obj, Span<byte> destination)
        {
            CheckCapacity (1);

            references[end] = obj;
            end++;

            return 0;
        }

        private int InsertImpl (in object obj, Span<byte> destination)
        {
            references[insertIndex] = obj;
            insertIndex++;

            return 0;
        }

        private int ReadImpl (out object obj, Type type, ReadOnlySpan<byte> source)
        {
            obj = references[start];
            references[start] = null; // Clear stored reference

            start++;

            if (start == end)
            {
                start = 0;
                end = 0;
            }

            return 0;
        }

        private void CheckCapacity (int refCount)
        {
            if (end + refCount > references.Length)
            {
                int count = end - start;

                if (count + refCount > references.Length)
                {
                    int newSize = Math.Max (count * 2, count + refCount);
                    Array.Resize (ref references, newSize);
                }

                if (start != 0)
                {
                    Array.Copy (references, start, references, 0, count);

                    end = count;
                    start = 0;
                }
            }
        }

        public ref struct InsertContext
        {
            private TaskReferenceStore store;

            internal InsertContext (TaskReferenceStore store)
            {
                this.store = store;
            }

            public void Dispose ()
            {
                store.insertIndex = -1;
            }
        }
    }
}
