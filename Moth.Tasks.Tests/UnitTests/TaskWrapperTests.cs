namespace Moth.Tasks.Tests.UnitTests
{
    using NUnit.Framework;
    using System.Collections.Generic;

    [TestFixture]
    public unsafe class TaskWrapperTests
    {
        [Test]
        public void TaskWrapperOfTTaskTArgTResult_Run_CallsRunWithDefaultArgument ()
        {
            List<object> suppliedArgs = new List<object> ();
            var task = new TestTask<object, object> (suppliedArgs, null);

            var wrapper = new TaskWrapper<TestTask<object, object>, object, object> (task);

            wrapper.Run ();

            Assert.That (suppliedArgs, Is.EqualTo (new object[] { default }));
        }

        [Test]
        public void TaskWrapperOfTTaskTArg_Run_CallsRunWithSuppliedArgumentAndReturnsUnit ()
        {
            List<object> suppliedArgs = new List<object> ();
            var task = new TestTask<object> (suppliedArgs);
            var wrapper = new TaskWrapper<TestTask<object>, object> (task);

            object arg = new object ();
            Unit result = wrapper.Run (arg);

            Assert.That (suppliedArgs, Is.EqualTo (new object[] { arg }));
            Assert.That (result, Is.EqualTo (default (Unit)));
        }

        [Test]
        public void TaskWrapperOfTTask_Run_CallsRunAndReturnsUnit ()
        {
            Counter runCallCount = new Counter ();

            var task = new TestTask (runCallCount);

            var wrapper = new TaskWrapper<TestTask> (task);

            Unit result = wrapper.Run (default);

            Assert.That (runCallCount.Count, Is.EqualTo (1));
            Assert.That (result, Is.EqualTo (default (Unit)));
        }

        public class Counter
        {
            public int Count;
        }

        public struct TestTask : ITask
        {
            public Counter RunCallCount;

            public TestTask (Counter runCallCount)
            {
                RunCallCount = runCallCount;
            }

            public void Run () => RunCallCount.Count++;
        }

        public struct TestTask<TArg> : ITask<TArg>
        {
            public List<TArg> SuppliedArgs;

            public TestTask (List<TArg> suppliedArgs)
            {
                SuppliedArgs = suppliedArgs;
            }

            public void Run (TArg arg) => SuppliedArgs.Add (arg);
        }

        public struct TestTask<TArg, TResult> : ITask<TArg, TResult>
        {
            public List<TArg> SuppliedArgs;
            public TResult ResultToReturn;

            public TestTask (List<TArg> suppliedArgs, TResult resultToReturn)
            {
                SuppliedArgs = suppliedArgs;
                ResultToReturn = resultToReturn;
            }

            public TResult Run (TArg arg)
            {
                SuppliedArgs.Add (arg);
                return ResultToReturn;
            }
        }
    }
}
