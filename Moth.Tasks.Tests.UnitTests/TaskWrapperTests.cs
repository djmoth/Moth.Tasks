namespace Moth.Tasks.Tests.UnitTests
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    [TestFixture]
    public unsafe class TaskWrapperTests
    {
        [Test]
        public void TaskWrapperOfTTaskTArgTResult_Run_CallsRunWithDefaultArgument ()
        {
            List<object> suppliedArgs = new List<object> ();
            var task = new TestTask<object, object> (suppliedArgs, null, null);

            var wrapper = new TaskWrapper<TestTask<object, object>, object, object> (task);

            wrapper.Run ();

            Assert.That (suppliedArgs, Is.EqualTo (new object[] { default }));
        }

        [Test]
        public void TaskWrapperOfTTaskTArgTResult_Dispose_DisposesTask ()
        {
            Counter disposeCallCount = new Counter ();
            var task = new TestTask<object, object> (null, null, disposeCallCount);
            var wrapper = new TaskWrapper<TestTask<object, object>, object, object> (task);

            wrapper.Dispose ();

            Assert.That (disposeCallCount.Count, Is.EqualTo (1));
        }

        [Test]
        public void TaskWrapperOfTTaskTArg_Run_CallsRunWithSuppliedArgumentAndReturnsUnit ()
        {
            List<object> suppliedArgs = new List<object> ();
            var task = new TestTask<object> (suppliedArgs, null);
            var wrapper = new TaskWrapper<TestTask<object>, object> (task);

            object arg = new object ();
            Unit result = wrapper.Run (arg);

            Assert.That (suppliedArgs, Is.EqualTo (new object[] { arg }));
            Assert.That (result, Is.EqualTo (default (Unit)));
        }

        [Test]
        public void TaskWrapperOfTTaskTArg_Dispose_DisposesTask ()
        {
            Counter disposeCallCount = new Counter ();
            var task = new TestTask<object> (null, disposeCallCount);
            var wrapper = new TaskWrapper<TestTask<object>, object> (task);

            wrapper.Dispose ();

            Assert.That (disposeCallCount.Count, Is.EqualTo (1));
        }

        [Test]
        public void TaskWrapperOfTTask_Run_CallsRunAndReturnsUnit ()
        {
            Counter runCallCount = new Counter ();
            var task = new TestTask (runCallCount, null);
            var wrapper = new TaskWrapper<TestTask> (task);

            Unit result = wrapper.Run (default);

            Assert.That (runCallCount.Count, Is.EqualTo (1));
            Assert.That (result, Is.EqualTo (default (Unit)));
        }

        [Test]
        public void TaskWrapperOfTTask_Dispose_DisposesTask ()
        {
            Counter disposeCallCount = new Counter ();
            var task = new TestTask (null, disposeCallCount);

            var wrapper = new TaskWrapper<TestTask> (task);

            wrapper.Dispose ();

            Assert.That (disposeCallCount.Count, Is.EqualTo (1));
        }

        public class Counter
        {
            public int Count;
        }

        public struct TestTask : ITask<Unit, Unit>, IDisposable
        {
            public Counter RunCallCount;
            public Counter DisposeCallCount;

            public TestTask (Counter runCallCount, Counter disposeCallCount)
            {
                RunCallCount = runCallCount;
                DisposeCallCount = disposeCallCount;
            }

            public void Run () => RunCallCount.Count++;

            public void Dispose () => DisposeCallCount.Count++;
        }

        public struct TestTask<TArg> : ITask<TArg>, IDisposable
        {
            public List<TArg> SuppliedArgs;
            public Counter DisposeCallCount;

            public TestTask (List<TArg> suppliedArgs, Counter disposeCallCount)
            {
                SuppliedArgs = suppliedArgs;
                DisposeCallCount = disposeCallCount;
            }

            public void Run (TArg arg) => SuppliedArgs.Add (arg);

            public void Dispose () => DisposeCallCount.Count++;
        }

        public struct TestTask<TArg, TResult> : ITask<TArg, TResult>, IDisposable
        {
            public List<TArg> SuppliedArgs;
            public TResult ResultToReturn;
            public Counter DisposeCallCount;

            public TestTask (List<TArg> suppliedArgs, TResult resultToReturn, Counter disposeCallCount)
            {
                SuppliedArgs = suppliedArgs;
                ResultToReturn = resultToReturn;
                DisposeCallCount = disposeCallCount;
            }

            public TResult Run (TArg arg)
            {
                SuppliedArgs.Add (arg);
                return ResultToReturn;
            }

            public void Dispose () => DisposeCallCount.Count++;
        }
    }
}
