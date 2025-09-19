using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.Tasks.Tests.UnitTests
{
    [TestFixture]
    public class TaskWithHandleTests
    {
        [Test]
        public void Run_WithArgument_CallsRunWithArgumentAndDoesNotDispose ()
        {
            List<object> suppliedArgs = new List<object> ();
            object valueToReturn = new object ();
            Counter disposeCallCount = new Counter ();

            var task = new TestTask<object, object> (suppliedArgs, valueToReturn, disposeCallCount);

            TaskHandle handle = default;

            var taskWithHandle = new TaskWithHandle<TestTask<object, object>, object, object> (task, handle);

            object arg = new object ();
            object returnedValue = taskWithHandle.Run (arg);

            Assert.Multiple (() =>
            {
                Assert.That (suppliedArgs, Is.EqualTo (new object[] { arg }));
                Assert.That (returnedValue, Is.EqualTo (valueToReturn));
                Assert.That (disposeCallCount.Count, Is.EqualTo (0));
            });
        }

        [Test]
        public void Dispose_WhenCalled_DisposesTaskAndCallsNotifyTaskCompletion ()
        {
            Mock<ITaskHandleManager> mockTaskHandleManager = new Mock<ITaskHandleManager> ();
            TaskHandle handle = new TaskHandle (mockTaskHandleManager.Object, 0);
            Counter disposeCallCount = new Counter ();

            var taskWithHandle = new TaskWithHandle<TestTask<object, object>, object, object> (new TestTask<object, object> (null, null, disposeCallCount), handle);

            taskWithHandle.Dispose ();

            mockTaskHandleManager.Verify (m => m.NotifyTaskCompletion (handle), Times.Once);

            Assert.That (disposeCallCount.Count, Is.EqualTo (1));
        }

        public class Counter
        {
            public int Count;
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
