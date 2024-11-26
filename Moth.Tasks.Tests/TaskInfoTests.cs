namespace Moth.Tasks.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Moth.Tasks;
    using NUnit.Framework;
    using NUnit.Framework.Legacy;

    public class TaskInfoTests
    {
        [Test]
        public void CreateTask ()
        {
            AssertTaskInfo<Task> (false, false);
        }

        [Test]
        public void CreateTaskWithRef ()
        {
            AssertTaskInfo<TaskWithRef> (false, true);
        }

        [Test]
        public void CreateDisposableTask ()
        {
            AssertTaskInfo<DisposableTask> (true, false);
        }

        void AssertTaskInfo<T> (bool disposable, bool isManaged) where T : struct, ITask
        {
            int taskID = 1; // Mock ID, set by a TaskCache in reality
            ITaskInfo<T> taskInfo = TaskInfo.Create<T> (1);

            ClassicAssert.AreEqual (taskID, taskInfo.ID);

            ClassicAssert.AreEqual (typeof (T), taskInfo.Type);

            ClassicAssert.AreEqual (IntPtr.Size, taskInfo.UnmanagedSize); // Task contains one field of IntPtr, size should match IntPtr.Size then

            ClassicAssert.AreEqual (isManaged, taskInfo.IsManaged);

            ClassicAssert.AreEqual (disposable, taskInfo.IsDisposable); // Task does not implement IDisposable
        }

        struct Task : ITask
        {
            public IntPtr MockData;

            public void Run ()
            {

            }
        }

        struct TaskWithRef : ITask
        {
            public IntPtr MockData;
            public object Ref;

            public void Run ()
            {

            }
        }

        struct DisposableTask : ITask, IDisposable
        {
            public IntPtr MockData;

            public void Run ()
            {

            }

            public void Dispose ()
            {

            }
        }
    }
}
