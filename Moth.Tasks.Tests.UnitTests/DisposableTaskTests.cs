namespace Moth.Tasks.Tests.UnitTests
{
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class DisposableTaskTests
    {
        [Test]
        public void Dispose_WithDisposableTask_DisposesTaskOnce ()
        {
            var task = new DisposableTestTask ();

            DisposableTask<DisposableTestTask>.Dispose (ref task);

            Assert.That (task.DisposeCallCount, Is.EqualTo (1));
        }

        public struct DisposableTestTask : ITask, IDisposable
        {
            public int DisposeCallCount { get; private set; }

            public void Run () { }

            public void Dispose () => DisposeCallCount++;
        }
    }
}
