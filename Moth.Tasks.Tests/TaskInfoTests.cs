using System;
using System.Collections.Generic;
using System.Text;
using Moth.Tasks;
using NUnit.Framework;

namespace Moth.Tasks.Tests
{
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
            TaskInfo<T> taskInfo = TaskInfo.Create<T> (1);

            Assert.AreEqual (taskID, taskInfo.ID);

            Assert.AreEqual (typeof (T), taskInfo.Type);

            Assert.AreEqual (IntPtr.Size, taskInfo.UnmanagedSize); // Task contains one field of IntPtr, size should match IntPtr.Size then

            Assert.AreEqual (isManaged, taskInfo.IsManaged);

            Assert.AreEqual (disposable, taskInfo.Disposable); // Task does not implement IDisposable
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
