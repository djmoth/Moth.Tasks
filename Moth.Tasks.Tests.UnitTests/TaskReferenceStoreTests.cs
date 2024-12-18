namespace Moth.Tasks.Tests.UnitTests
{
    using Moth.IO.Serialization;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    [TestFixture]
    public class TaskReferenceStoreTests
    {
        [Test]
        public void Constructor_WithStartingCapacity_InitializesCorrectly()
        {
            int startingCapacity = 10;
            TaskReferenceStore store = new TaskReferenceStore (startingCapacity);

            Assert.That (store.Capacity, Is.EqualTo (startingCapacity));

            Assert.That (store.Start, Is.EqualTo (0));
            Assert.That (store.End, Is.EqualTo (0));
            Assert.That (store.Count, Is.EqualTo (0));

            Assert.That (store.Write, Is.Not.Null);
            Assert.That (store.Read, Is.Not.Null);
        }

        [Test]
        public void Write_WhenOutOfCapacity_IncreasesCapacity ()
        {
            TaskReferenceStore store = new TaskReferenceStore (0);

            // Initial capacity is 0, so this should increase it
            store.Write (null, Span<byte>.Empty);

            Assert.That (store.Capacity, Is.GreaterThan (0));
        }

        [Test]
        public void Write_WhenCalled_StartUnchangedAndEndAndCountIncremented ()
        {
            TaskReferenceStore store = new TaskReferenceStore (0);

            int start = store.Start;
            int end = store.End;
            int count = store.Count;

            // Initial capacity is 0, so this should increase it
            store.Write (null, Span<byte>.Empty);

            Assert.That (store.Start, Is.EqualTo (start));
            Assert.That (store.End, Is.EqualTo (end + 1));
            Assert.That (store.Count, Is.EqualTo (count + 1));
        }

        [Test]
        public void Read_WhenNotEmptyBeforeAndNotEmptyAfter_StartIncrementedEndUnchangedAndCountDecremented ()
        {
            TaskReferenceStore store = new TaskReferenceStore (0);

            store.Write (null, Span<byte>.Empty);
            store.Write (null, Span<byte>.Empty);

            int start = store.Start;
            int end = store.End;
            int count = store.Count;

            store.Read (out object _, typeof (object), Span<byte>.Empty);

            Assert.That (store.Start, Is.EqualTo (start + 1));
            Assert.That (store.End, Is.EqualTo (end));
            Assert.That (store.Count, Is.EqualTo (count - 1));
        }

        [Test]
        public void Read_WhenNotEmptyBeforeButEmptyAfter_ResetsCorrectly ()
        {
            TaskReferenceStore store = new TaskReferenceStore (0);

            store.Write (null, Span<byte>.Empty);

            store.Read (out object _, typeof (object), Span<byte>.Empty);

            Assert.That (store.Start, Is.EqualTo (0));
            Assert.That (store.End, Is.EqualTo (0));
            Assert.That (store.Count, Is.EqualTo (0));
        }

        [Test]
        public void Read_WhenEmpty_ThrowsInvalidOperationException ()
        {
            TaskReferenceStore store = new TaskReferenceStore (0);
            Assert.That (() => store.Read (out object _, typeof (object), Span<byte>.Empty), Throws.InvalidOperationException);
        }

        [Test]
        public void Read_WhenNotEmpty_ClearsInternalReference ()
        {
            TaskReferenceStore store = new TaskReferenceStore (0);

            WeakReference objectRef = new WeakReference (null);

            // Allocate and write object in a separate method to ensure that no references in this stack frame are held
            var writeObject = () =>
            {
                object obj = new object ();
                objectRef.Target = obj;
                store.Write (obj, Span<byte>.Empty);
            };

            writeObject ();

            // Read object and clear the returned reference
            store.Read (out object readReference, typeof (object), Span<byte>.Empty);
            readReference = null;

            // If Read correctly cleared its internal reference, there should now be no live references to the object, and as such it should be collected by GC.Collect
            GC.Collect (GC.MaxGeneration, GCCollectionMode.Forced, true, true);

            Assert.That (objectRef.IsAlive, Is.False);

            GC.KeepAlive (store); // Ensure store is not collected before the end of the test
        }

        [TestCase (1)]
        [TestCase (2)]
        [TestCase (3)]
        public void WriteThenRead_WhenOutOfCapacity_PreservesReferencesWhenResizingWritesAndReadesSameReference (int timesToResize)
        {
            TaskReferenceStore store = new TaskReferenceStore (1);

            List<object> writtenReferences = new List<object> ();

            int timesResized = 0;

            int maxWrittenReferences = 100;
            for (int i = 0; i < maxWrittenReferences; i++)
            {
                writtenReferences.Add (new object ());

                int previousCapacity = store.Capacity;
                store.Write (writtenReferences[i], Span<byte>.Empty);

                if (store.Capacity > previousCapacity)
                {
                    timesResized++;

                    if (timesResized == timesToResize)
                    {
                        break;
                    }
                }
            }

            Assume.That (timesResized, Is.EqualTo (timesToResize));

            object[] readReferences = new object[writtenReferences.Count];

            for (int i = 0; i < readReferences.Length; i++)
            {
                store.Read (out readReferences[i], typeof (object), Span<byte>.Empty);
            }

            Assert.That (readReferences, Is.EqualTo (writtenReferences));
        }

        [Test]
        public void WriteThenRead_WhenCalled_WritesAndReadesSameReference ()
        {
            TaskReferenceStore store = new TaskReferenceStore (1);

            object writtenReference = new object ();

            store.Write (writtenReference, Span<byte>.Empty);
            store.Read (out object readReference, typeof (object), Span<byte>.Empty);

            Assert.That (readReference, Is.SameAs (writtenReference));
        }

        [Test]
        public void EnterInsertContext_WhenCalled_ReturnsInsertObjectWriter ()
        {
            TaskReferenceStore store = new TaskReferenceStore (0);

            int insertIndex = 0;
            store.EnterInsertContext (ref insertIndex, 1, out ObjectWriter insertWriter);

            Assert.That (insertWriter, Is.Not.Null);
        }

        [Test]
        public void EnterInsertContext_WhenInsertObjectWriterUsedAfterDispose_ThrowsInvalidOperationException ()
        {
            TaskReferenceStore store = new TaskReferenceStore (0);
            int insertIndex = 0;

            var insertContext = store.EnterInsertContext (ref insertIndex, 1, out ObjectWriter insertWriter);

            insertContext.Dispose ();

            Assert.That (() => insertWriter (null, Span<byte>.Empty), Throws.TypeOf (typeof (InvalidOperationException)));
        }

        [Test]
        public void EnterInsertContext_InsertIndexLessThanStart_ThrowsArgumentOutOfRangeException ()
        {
            TaskReferenceStore store = new TaskReferenceStore (0);

            int insertIndex = store.Start - 1;

            Assert.That (() => store.EnterInsertContext (ref insertIndex, 1, out _), Throws.TypeOf (typeof (ArgumentOutOfRangeException)));
        }

        [Test]
        public void EnterInsertContext_InsertIndexGreaterThanEnd_ThrowsArgumentOutOfRangeException ()
        {
            TaskReferenceStore store = new TaskReferenceStore (0);
            int insertIndex = store.End + 1;
            Assert.That (() => store.EnterInsertContext (ref insertIndex, 1, out _), Throws.TypeOf (typeof (ArgumentOutOfRangeException)));
        }

        [Test]
        public void EnterInsertContext_InsertAtStartWithSpaceEnoughBeforeStart_InsertsWithoutMovingReferences ()
        {
            TaskReferenceStore store = new TaskReferenceStore (0);

            store.Write (null, Span<byte>.Empty);
            store.Write (null, Span<byte>.Empty);
            store.Read (out _, typeof (object), Span<byte>.Empty);

            Assume.That (store.Start, Is.EqualTo (1));
            Assume.That (store.End, Is.EqualTo (2));

            int insertIndex = store.Start;
            store.EnterInsertContext (ref insertIndex, 1, out _);

            Assert.That (insertIndex, Is.EqualTo (0));
            Assert.That (store.Start, Is.EqualTo (0));
            Assert.That (store.End, Is.EqualTo (2));
        }

        [Test]
        public void EnterInsertContext_InsertAtStartWithoutEnoughSpaceBeforeStartWithoutIncreasingCapacity_InsertIndexIsUnchanged ()
        {
            int initialCapacity = 4;
            TaskReferenceStore store = new TaskReferenceStore (initialCapacity);

            store.Write (null, Span<byte>.Empty);
            store.Write (null, Span<byte>.Empty);
            store.Read (out _, typeof (object), Span<byte>.Empty);

            Assume.That (store.Start, Is.EqualTo (1));
            Assume.That (store.End, Is.EqualTo (2));

            int insertIndex = store.Start;
            int originalInsertIndex = insertIndex;
            store.EnterInsertContext (ref insertIndex, 2, out _);

            Assume.That (store.Capacity, Is.EqualTo (initialCapacity));

            Assert.That (insertIndex, Is.EqualTo (originalInsertIndex));
            Assert.That (store.Start, Is.EqualTo (1));
            Assert.That (store.End, Is.EqualTo (4));
        }

        [Test]
        public void EnterInsertContext_WhenCalledTwiceWithoutDisposeFirst_ThrowsInvalidOperationException ()
        {
            TaskReferenceStore store = new TaskReferenceStore (0);

            int insertIndex = 0;

            store.EnterInsertContext (ref insertIndex, 1, out _);

            Assert.That (() => store.EnterInsertContext (ref insertIndex, 1, out _), Throws.TypeOf (typeof (InvalidOperationException)));
        }

        [Test]
        public void EnterInsertContext_WhenCalledTwiceButDisposedFirst_EntersCorrectly ()
        {
            TaskReferenceStore store = new TaskReferenceStore (0);
            int insertIndex = 0;
            var insertContext = store.EnterInsertContext (ref insertIndex, 1, out _);
            insertContext.Dispose ();

            Assert.That (() => store.EnterInsertContext (ref insertIndex, 1, out _), Throws.Nothing);
        }

        [TestCase (1)]
        [TestCase (2)]
        [TestCase (5)]
        public void EnterInsertContextThenRead_WhenEmpty_InsertsAndReadsSameReference (int refCount)
        {
            TaskReferenceStore store = new TaskReferenceStore (0);

            object[] writtenReferences = new object[refCount];

            for (int i = 0; i < writtenReferences.Length; i++)
            {
                writtenReferences[i] = new object ();
            }

            int insertIndex = 0;

            using (var insertContext = store.EnterInsertContext (ref insertIndex, writtenReferences.Length, out ObjectWriter insertWriter))
            {
                foreach (object reference in writtenReferences)
                {
                    insertWriter (reference, Span<byte>.Empty);
                }
            }

            object[] readReferences = new object[writtenReferences.Length];

            for (int i = 0; i < writtenReferences.Length; i++)
            {
                store.Read (out readReferences[i], typeof (object), Span<byte>.Empty);
            }

            Assert.That (readReferences, Is.EqualTo (writtenReferences));
        }

        [TestCase (0, 0, 1)] // Insert 1 reference at Start when empty
        [TestCase (1, 1, 1)] // Insert 1 reference at End when Count is one
        [TestCase (2, 1, 1)] // Insert 1 reference in middle when Count is two
        public void EnterInsertContextThenRead_WhenCalled_InsertsAndReadsSameReference (int initialCount, int insertIndex, int insertCount)
        {
            TaskReferenceStore store = new TaskReferenceStore (0);

            List<object> writtenReferences = new List<object> (initialCount);

            for (int i = 0; i < initialCount; i++)
            {
                object reference = new object ();
                writtenReferences.Add (reference);
                store.Write (reference, Span<byte>.Empty);
            }

            Assume.That (store.Count, Is.EqualTo (initialCount));

            int originalInsertIndex = insertIndex;

            using (var insertContext = store.EnterInsertContext (ref insertIndex, insertCount, out ObjectWriter insertWriter))
            {
                for (int i = 0; i < insertCount; i++)
                {
                    object reference = new object ();
                    writtenReferences.Insert (insertIndex, reference);

                    insertWriter (reference, Span<byte>.Empty);
                }
            }

            // insertIndex shouldn't have changed, as there should not be space to insert before the original insertIndex
            Assume.That (insertIndex, Is.EqualTo (originalInsertIndex));
            Assume.That (store.Count, Is.EqualTo (initialCount + insertCount));

            object[] readReferences = new object[writtenReferences.Count];

            for (int i = 0; i < writtenReferences.Count; i++)
            {
                store.Read (out readReferences[i], typeof (object), Span<byte>.Empty);
            }

            Assert.That (readReferences, Is.EqualTo (writtenReferences));
        }

        [Test]
        public void Skip_WhenNotEmptyBeforeAndNotEmptyAfter_StartIncrementedAndEndUnchangedAndCountDecremented ()
        {
            TaskReferenceStore store = new TaskReferenceStore (0);

            store.Write (null, Span<byte>.Empty);
            store.Write (null, Span<byte>.Empty);

            int start = store.Start;
            int end = store.End;
            int count = store.Count;

            store.Skip (1);

            Assert.That (store.Start, Is.EqualTo (start + 1));
            Assert.That (store.End, Is.EqualTo (end));
            Assert.That (store.Count, Is.EqualTo (count - 1));
        }

        [Test]
        public void Skip_WhenNotEmptyBeforeButEmptyAfter_ResetsCorrectly ()
        {
            TaskReferenceStore store = new TaskReferenceStore (0);

            store.Write (null, Span<byte>.Empty);

            store.Skip (1);

            Assert.That (store.Start, Is.EqualTo (0));
            Assert.That (store.End, Is.EqualTo (0));
            Assert.That (store.Count, Is.EqualTo (0));
        }

        [Test]
        public void Skip_WhenRefCountLargerThanCount_ThrowsArgumentOutOfRangeException ()
        {
            TaskReferenceStore store = new TaskReferenceStore (0);

            Assert.That (() => store.Skip (1), Throws.TypeOf (typeof (ArgumentOutOfRangeException)));
        }

        [Test]
        public void Clear_WhenNotEmpty_ResetsCorrectly ()
        {
            TaskReferenceStore store = new TaskReferenceStore (0);

            store.Write (null, Span<byte>.Empty);

            store.Clear ();

            Assert.That (store.Start, Is.EqualTo (0));
            Assert.That (store.End, Is.EqualTo (0));
            Assert.That (store.Count, Is.EqualTo (0));
        }

        [Test]
        public void Clear_WhenNotEmpty_ClearsReferences ()
        {
            TaskReferenceStore store = new TaskReferenceStore (0);

            WeakReference objectRef = new WeakReference (null);

            // Allocate and write object in a separate method to ensure that no references in this stack frame are held
            var writeObject = () =>
            {
                object obj = new object ();
                objectRef.Target = obj;
                store.Write (obj, Span<byte>.Empty);
            };

            writeObject ();

            store.Clear ();

            // If Clear correctly cleared its internal reference, there should now be no live references to the object, and as such it should be collected by GC.Collect
            GC.Collect (GC.MaxGeneration, GCCollectionMode.Forced, true, true);

            Assert.That (objectRef.IsAlive, Is.False);

            GC.KeepAlive (store); // Ensure store is not collected before the end of the test
        }

        [Test]
        public void Clear_WhenEmpty_ResetsCorrectly ()
        {
            TaskReferenceStore store = new TaskReferenceStore (0);

            store.Clear ();

            Assert.That (store.Start, Is.EqualTo (0));
            Assert.That (store.End, Is.EqualTo (0));
            Assert.That (store.Count, Is.EqualTo (0));
        }
    }
}
