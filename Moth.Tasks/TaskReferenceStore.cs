namespace Moth.Tasks
{
    using Moth.IO.Serialization;
    using System;

    /// <inheritdoc/>
    public class TaskReferenceStore : ITaskReferenceStore
    {
        private readonly ObjectWriter insert;
        private readonly Action insertContextOnDispose;
        private object[] references;
        private int insertIndex = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskReferenceStore"/> class.
        /// </summary>
        /// <param name="startCapacity">The starting capacity of the store.</param>
        public TaskReferenceStore (int startCapacity)
        {
            references = new object[startCapacity];

            Write = WriteImpl;
            insert = InsertImpl;
            insertContextOnDispose = () => insertIndex = -1;

            Read = ReadImpl;
        }

        /// <inheritdoc/>
        public int Start { get; private set; }

        /// <inheritdoc/>
        public int End { get; private set; }

        /// <inheritdoc/>
        public int Count => End - Start;

        /// <inheritdoc/>
        public int Capacity => references.Length;

        /// <inheritdoc/>
        public ObjectWriter Write { get; }

        /// <inheritdoc/>
        public ObjectReader Read { get; }

        /// <inheritdoc/>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="insertIndex"/> is not within the range of stored references.</exception>"
        public TaskReferenceInsertContext EnterInsertContext (ref int insertIndex, int refCount, out ObjectWriter insertWriter)
        {
            if (this.insertIndex != -1)
            {
                throw new InvalidOperationException ("Cannot enter insert context while already in insert context.");
            }

            if (insertIndex < Start || insertIndex > End)
            {
                throw new ArgumentOutOfRangeException (nameof (insertIndex), "Insert index must be within the range of stored references.");
            }

            // If inserting at the beginning of the store and there is enough space before the first task
            if (insertIndex == Start && Start >= refCount)
            {
                Start -= refCount;
                insertIndex = Start;
            } else
            {
                CheckCapacity (refCount);

                int countAboveInsertIndex = End - insertIndex;
                End += refCount;

                Array.Copy (references, insertIndex, references, insertIndex + refCount, countAboveInsertIndex); // Move elements above insert area
            }

            this.insertIndex = insertIndex;
            insertWriter = insert;

            return new TaskReferenceInsertContext (insertContextOnDispose);
        }

        /// <inheritdoc/>
        public void Clear ()
        {
            for (int i = Start; i < End; i++)
            {
                references[i] = null;
            }

            Start = 0;
            End = 0;
        }

        /// <inheritdoc/>
        public void Skip (int refCount)
        {
            if (Start + refCount > End)
            {
                throw new ArgumentOutOfRangeException (nameof (refCount), "Cannot skip more references than stored.");
            }

            // Clear references
            for (int i = 0; i < refCount; i++)
            {
                references[Start + i] = null;
            }

            Start += refCount;

            if (Start == End)
            {
                Start = 0;
                End = 0;
            }
        }

        private int WriteImpl (in object obj, Span<byte> destination)
        {
            CheckCapacity (1);

            references[End] = obj;
            End++;

            return 0;
        }

        private int InsertImpl (in object obj, Span<byte> destination)
        {
            if (insertIndex == -1)
            {
                throw new InvalidOperationException ("Cannot insert object outside of insert context.");
            }

            references[insertIndex] = obj;
            insertIndex++;

            return 0;
        }

        private int ReadImpl (out object obj, Type type, ReadOnlySpan<byte> source)
        {
            if (Count == 0)
                throw new InvalidOperationException ("Cannot read from empty store.");

            obj = references[Start];
            references[Start] = null; // Clear stored reference

            Start++;

            if (Start == End)
            {
                Start = 0;
                End = 0;
            }

            return 0;
        }

        private void CheckCapacity (int refCount)
        {
            if (End + refCount > references.Length)
            {
                int count = End - Start;

                if (count + refCount > references.Length)
                {
                    int newSize = Math.Max (count * 2, count + refCount);
                    Array.Resize (ref references, newSize);
                }

                if (Start != 0)
                {
                    Array.Copy (references, Start, references, 0, count);

                    End = count;
                    Start = 0;
                }
            }
        }
    }
}
