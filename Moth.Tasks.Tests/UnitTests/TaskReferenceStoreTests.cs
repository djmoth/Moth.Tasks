namespace Moth.Tasks.Tests.UnitTests
{
    using NUnit.Framework;
    using System;
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
            GC.WaitForPendingFinalizers ();

            Assert.That (objectRef.IsAlive, Is.False);

            GC.KeepAlive (store); // Ensure store is not collected before the end of the test
        }

        [Test]
        public void WriteThenRead_WhenOutOfCapacity_PreservesReferencesWhenResizingWritesAndReadesSameReference ()
        {
            TaskReferenceStore store = new TaskReferenceStore (1);

            object[] writtenReferences = { new object (), new object () };

            for (int i = 0; i < writtenReferences.Length; i++)
            {
                store.Write (writtenReferences[i], Span<byte>.Empty);
            }

            object[] readReferences = new object[writtenReferences.Length];

            for (int i = 0; i < writtenReferences.Length; i++)
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
        public void EnterInsertContext

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
            GC.WaitForPendingFinalizers ();

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
