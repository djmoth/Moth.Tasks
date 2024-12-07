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
        public void Run_WithArgument_CallsRunWithArgument ()
        {
            List<object> suppliedArgs = new List<object> ();
            object valueToReturn = new object ();

            var task = new TestTask<object, object> (suppliedArgs, valueToReturn);

            TaskHandle handle = default;

            var taskWithHandle = new TaskWithHandle<TestTask<object, object>, object, object> (task, handle);

            object arg = new object ();
            object returnedValue = taskWithHandle.Run (arg);

            Assert.Multiple (() =>
            {
                Assert.That (suppliedArgs, Is.EqualTo (new object[] { arg }));
                Assert.That (returnedValue, Is.EqualTo (valueToReturn));
            });
        }

        [Test]
        public void Dispose_WhenCalled_CallsNotifyTaskCompletion ()
        {
            Mock<ITaskHandleManager> mockTaskHandleManager = new Mock<ITaskHandleManager> ();
            TaskHandle handle = new TaskHandle (mockTaskHandleManager.Object, 0);

            var taskWithHandle = new TaskWithHandle<TestTask<object, object>, object, object> (new TestTask<object, object> (null, null), handle);

            taskWithHandle.Dispose ();

            mockTaskHandleManager.Verify (m => m.NotifyTaskCompletion (handle), Times.Once);
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
