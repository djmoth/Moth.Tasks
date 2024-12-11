namespace Moth.Tasks.Tests.UnitTests
{
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class TaskOfTTaskTests
    {
        [Test]
        public void TryDispose_WithNonDisposableTask_DoesNotDisposeTask ()
        {
            var task = new TestTask ();

            bool disposed = Task<TestTask>.TryDispose (ref task);

            Assert.Multiple (() =>
            {
                Assert.That (task.Disposed, Is.False);
                Assert.That (disposed, Is.False);
            });
        }

        [Test]
        public void TryDispose_WithDisposableTask_DisposesTask ()
        {
            var task = new DisposableTestTask ();

            bool disposed = Task<DisposableTestTask>.TryDispose (ref task);

            Assert.Multiple (() =>
            {
                Assert.That (task.Disposed, Is.True);
                Assert.That (disposed, Is.True);
            });
        }

        public struct TestTask : ITask<Unit, Unit>
        {
            public bool Disposed { get; private set; }

            public void Run () { }

            public void Dispose () => Disposed = true;
        }

        public struct DisposableTestTask : ITask<Unit, Unit>, IDisposable
        {
            public bool Disposed { get; private set; }

            public void Run () { }

            public void Dispose () => Disposed = true;
        }
    }
}
