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
            AssertTaskInfo<Task> (false);
        }

        [Test]
        public void CreateDisposableTask ()
        {
            AssertTaskInfo<DisposableTask> (true);
        }

        void AssertTaskInfo<T> (bool disposable) where T : struct, ITask
        {
            int taskID = 1; // Mock ID, set by a TaskCache in reality
            TaskInfo taskInfo = TaskInfo.Create<T> (1);

            Assert.AreEqual (taskID, taskInfo.ID);

            Assert.AreEqual (typeof (T), taskInfo.Type);

            Assert.AreEqual (IntPtr.Size, taskInfo.DataSize); // Task contains one field of IntPtr, size should match IntPtr.Size then

            Assert.AreEqual (1, taskInfo.DataIndices); // One "Data Index" refers to an index in the internal TaskQueue.taskData object array

            Assert.AreEqual (disposable, taskInfo.Disposable); // Task does not implement IDisposable
        }

        struct Task : ITask
        {
            IntPtr mockData;

            public void Run ()
            {

            }
        }

        struct DisposableTask : ITask, IDisposable
        {
            IntPtr mockData;

            public void Run ()
            {

            }

            public void Dispose ()
            {

            }
        }
    }
}
