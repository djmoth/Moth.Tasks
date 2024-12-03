namespace Moth.Tasks.Tests.UnitTests
{
    using NUnit.Framework;

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
            store.Write (new object ());
            Assert.That (store.Capacity, Is.EqualTo (startingCapacity));
            store.Write (new object ());
            Assert.That (store.Capacity, Is.GreaterThan (startingCapacity));
        }
    }
}
