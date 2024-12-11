namespace Moth.Tasks.Tests.UnitTests
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    [TestFixture]
    public class TaskExtensionsTests
    {
        [Test]
        public void TryRunAndDispose_WithNonDisposableTask_RunsOnce ()
        {
            var task = new TestTask ();

            Assume.That (task.RunCallCount, Is.EqualTo (0));

            task.TryRunAndDispose ();

            Assert.That (task.RunCallCount, Is.EqualTo (1));
        }

        [Test]
        public void TryRunAndDispose_WithDisposableTask_RunsOnceDisposesOnce ()
        {
            var task = new DisposableTestTask ();

            Assume.That (task.RunCallCount, Is.EqualTo (0));
            Assume.That (task.DisposeCallCount, Is.EqualTo (0));

            task.TryRunAndDispose ();

            Assert.Multiple (() =>
            {
                Assert.That (task.RunCallCount, Is.EqualTo (1));
                Assert.That (task.DisposeCallCount, Is.EqualTo (1));
            });
        }

        [Test]
        public void TryRunAndDispose_WithDisposableTaskThrowingException_RunsOnceAndDisposesOnce ()
        {
            var task = new DisposableTestTaskThrowingException ();

            Assume.That (task.RunCallCount, Is.EqualTo (0));
            Assume.That (task.DisposeCallCount, Is.EqualTo (0));
            Assume.That (() => task.TryRunAndDispose (), Throws.Exception);

            Assert.That (task.RunCallCount, Is.EqualTo (1));
            Assert.That (task.DisposeCallCount, Is.EqualTo (1));
        }

        [Test]
        public void TryRunAndDispose_WithNonDisposableTaskWithArg_RunsOnceWithArg ()
        {
            var task = new TestTaskWithArg { SuppliedArgs = new List<int> () };

            Assume.That (task.SuppliedArgs.Count, Is.EqualTo (0));

            int arg = 42;
            task.TryRunAndDispose (arg);

            Assert.That (task.SuppliedArgs, Is.EqualTo (new int[] { arg }));
        }

        [Test]
        public void TryRunAndDispose_WithDisposableTaskWithArg_RunsOnceWithArgAndDisposesOnce ()
        {
            var task = new DisposableTestTaskWithArg { SuppliedArgs = new List<int> () };

            Assume.That (task.SuppliedArgs.Count, Is.EqualTo (0));
            Assume.That (task.DisposeCallCount, Is.EqualTo (0));

            int arg = 42;
            task.TryRunAndDispose (arg);

            Assert.That (task.SuppliedArgs, Is.EqualTo (new int[] { arg }));
            Assert.That (task.DisposeCallCount, Is.EqualTo (1));
        }

        [Test]
        public void TryRunAndDispose_WithDisposableTaskWithArgThrowingException_RunsOnceWithArgAndDisposesOnce ()
        {
            var task = new DisposableTestTaskWithArgThrowingException { SuppliedArgs = new List<int> () };

            Assume.That (task.SuppliedArgs.Count, Is.EqualTo (0));
            Assume.That (task.DisposeCallCount, Is.EqualTo (0));

            int arg = 42;
            Assume.That (() => task.TryRunAndDispose (arg), Throws.Exception);

            Assert.That (task.SuppliedArgs, Is.EqualTo (new int[] { arg }));
            Assert.That (task.DisposeCallCount, Is.EqualTo (1));
        }

        [Test]
        public void TryRunAndDispose_WithNonDisposableTaskWithArgAndResult_RunsOnceWithArgAndReturnsResult ()
        {
            var task = new TestTaskWithArgAndResult { SuppliedArgs = new List<int> (), ExpectedResult = 123 };

            Assume.That (task.SuppliedArgs.Count, Is.EqualTo (0));

            int arg = 42;
            task.TryRunAndDispose (arg, out int result);

            Assert.Multiple (() =>
            {
                Assert.That (task.SuppliedArgs, Is.EqualTo (new int[] { arg }));
                Assert.That (result, Is.EqualTo (task.ExpectedResult));
            });
        }

        [Test]
        public void TryRunAndDispose_WithDisposableTaskWithArgAndResult_RunsOnceWithArgAndReturnsResultAndDisposesOnce ()
        {
            var task = new DisposableTestTaskWithArgAndResult { SuppliedArgs = new List<int> (), ExpectedResult = 123 };

            Assume.That (task.SuppliedArgs.Count, Is.EqualTo (0));
            Assume.That (task.DisposeCallCount, Is.EqualTo (0));

            int arg = 42;
            task.TryRunAndDispose (arg, out int result);

            Assert.Multiple (() =>
            {
                Assert.That (task.SuppliedArgs, Is.EqualTo (new int[] { arg }));
                Assert.That (result, Is.EqualTo (task.ExpectedResult));
                Assert.That (task.DisposeCallCount, Is.EqualTo (1));
            });
        }

        [Test]
        public void TryRunAndDispose_WithDisposableTaskWithArgAndResultThrowingException_RunsOnceWithArgAndDisposesOnce ()
        {
            var task = new DisposableTestTaskWithArgAndResultThrowingException { SuppliedArgs = new List<int> (), ExpectedResult = 123 };

            Assume.That (task.SuppliedArgs.Count, Is.EqualTo (0));
            Assume.That (task.DisposeCallCount, Is.EqualTo (0));

            int arg = 42;
            Assume.That (() => task.TryRunAndDispose (arg, out int _), Throws.Exception);

            Assert.Multiple (() =>
            {
                Assert.That (task.SuppliedArgs, Is.EqualTo (new int[] { arg }));
                Assert.That (task.DisposeCallCount, Is.EqualTo (1));
            });
        }

        [Test]
        public void TryDispose_WithNonDisposableTask_DoesNothing ()
        {
            var task = new TestTask ();

            Assert.That (() => task.TryDispose (), Throws.Nothing);
        }

        [Test]
        public void TryDispose_WithDisposableTask_DisposesTask ()
        {
            var task = new DisposableTestTask ();

            task.TryDispose ();

            Assert.That (task.DisposeCallCount, Is.EqualTo (1));
        }

        public struct TestTask : ITask<Unit, Unit>
        {
            public int RunCallCount;

            public void Run () => RunCallCount++;
        }

        public struct DisposableTestTask : ITask<Unit, Unit>, IDisposable
        {
            public int RunCallCount;
            public int DisposeCallCount;

            public void Run () => RunCallCount++;

            public void Dispose () => DisposeCallCount++;
        }

        public struct DisposableTestTaskThrowingException : ITask<Unit, Unit>, IDisposable
        {
            public int RunCallCount;
            public int DisposeCallCount;

            public void Run ()
            {
                RunCallCount++;
                throw new Exception ();
            }

            public void Dispose () => DisposeCallCount++;
        }

        public struct TestTaskWithArg : ITask<int>
        {
            public List<int> SuppliedArgs;

            public void Run (int arg) => SuppliedArgs.Add (arg);
        }

        public struct DisposableTestTaskWithArg : ITask<int>, IDisposable
        {
            public List<int> SuppliedArgs;
            public int DisposeCallCount;

            public void Run (int arg) => SuppliedArgs.Add (arg);

            public void Dispose () => DisposeCallCount++;
        }

        public struct DisposableTestTaskWithArgThrowingException : ITask<int>, IDisposable
        {
            public List<int> SuppliedArgs;
            public int DisposeCallCount;

            public void Run (int arg)
            {
                SuppliedArgs.Add (arg);
                throw new Exception ();
            }

            public void Dispose () => DisposeCallCount++;
        }

        public struct TestTaskWithArgAndResult : ITask<int, int>
        {
            public List<int> SuppliedArgs;
            public int ExpectedResult;

            public int Run (int arg)
            {
                SuppliedArgs.Add (arg);
                return ExpectedResult;
            }
        }

        public struct DisposableTestTaskWithArgAndResult : ITask<int, int>, IDisposable
        {
            public List<int> SuppliedArgs;
            public int DisposeCallCount;
            public int ExpectedResult;

            public int Run (int arg)
            {
                SuppliedArgs.Add (arg);
                return ExpectedResult;
            }

            public void Dispose () => DisposeCallCount++;
        }

        public struct DisposableTestTaskWithArgAndResultThrowingException : ITask<int, int>, IDisposable
        {
            public List<int> SuppliedArgs;
            public int DisposeCallCount;
            public int ExpectedResult;

            public int Run (int arg)
            {
                SuppliedArgs.Add (arg);
                throw new Exception ();
            }

            public void Dispose () => DisposeCallCount++;
        }
    }
}
