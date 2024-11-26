
namespace Moth.Tasks.Tests
{
    using NUnit.Framework;
    using NUnit.Framework.Legacy;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;


    internal class TaskDataStoreTests
    {
        [Test]
        public unsafe void EnqueueTask_ExpandTaskData ()
        {
            TaskDataStore taskData = new TaskDataStore (1);
            ITaskInfo<Task> taskInfoStub = new FakeTaskInfo ();

            taskData.Enqueue (new Task (), taskInfoStub);

            ClassicAssert.AreEqual (sizeof (Task), taskData.Size);
        }

        [Test]
        public void DequeueTask_ReturnsCorrectTask ()
        {
            TaskDataStore taskData = new TaskDataStore (100);
            ITaskInfo<Task> taskInfoStub = new FakeTaskInfo ();
            Task task = new Task (42);

            taskData.Enqueue (task, taskInfoStub);
            Task dequeuedTask = taskData.Dequeue (taskInfoStub);

            ClassicAssert.AreEqual (task.Data, dequeuedTask.Data);
        }

        [Test]
        public void SkipTask_UpdatesFirstTaskCorrectly ()
        {
            TaskDataStore taskData = new TaskDataStore (100);
            ITaskInfo<Task> taskInfoStub = new FakeTaskInfo ();
            Task task = new Task (42);

            taskData.Enqueue (task, taskInfoStub);
            taskData.Skip (taskInfoStub);

            ClassicAssert.AreEqual (0, taskData.FirstTask);
            ClassicAssert.AreEqual (0, taskData.LastTaskEnd);
        }

        [Test]
        public void InsertTask_InsertsCorrectly ()
        {
            TaskDataStore taskData = new TaskDataStore (100);
            ITaskInfo<Task> taskInfoStub = new FakeTaskInfo ();
            Task task1 = new Task (42);
            Task task2 = new Task (123);

            taskData.Enqueue (task1, taskInfoStub);
            taskData.Insert (0, 0, task2, taskInfoStub);

            Task dequeuedTask = taskData.Dequeue (taskInfoStub);
            ClassicAssert.AreEqual (task2.Data, dequeuedTask.Data);
        }

        [Test]
        public void ClearTaskDataStore_ResetsCorrectly ()
        {
            TaskDataStore taskData = new TaskDataStore (100);
            ITaskInfo<Task> taskInfoStub = new FakeTaskInfo ();
            Task task = new Task { Data = 42 };

            taskData.Enqueue (task, taskInfoStub);
            taskData.Clear ();

            ClassicAssert.AreEqual (0, taskData.FirstTask);
            ClassicAssert.AreEqual (0, taskData.LastTaskEnd);
        }

        [Test]
        public unsafe void CheckCapacity_ResizesCorrectly ()
        {
            TaskDataStore taskData = new TaskDataStore (1);
            ITaskInfo<Task> taskInfoStub = new FakeTaskInfo ();
            Task task = new Task (42);

            taskData.Enqueue (task, taskInfoStub);

            ClassicAssert.GreaterOrEqual (taskData.Size, sizeof (Task));
        }

        /// <summary>
        /// Test task
        /// </summary>
        struct Task : ITask
        {
            public int Data;

            public Task (int data)
            {
                Data = data;
            }

            public void Run ()
            {

            }
        }

        unsafe class FakeTaskInfo : ITaskInfo<Task>
        {
            public int ID => 0;

            public Type Type => typeof (Task);

            public int UnmanagedSize => sizeof (Task);

            public int ReferenceCount => 0;

            public bool IsManaged => false;

            public bool IsDisposable => false;

            public bool HasArgs => false;

            public void Deserialize (out Task task, ReadOnlySpan<byte> source, TaskReferenceStore references)
            {
                task = new Task (MemoryMarshal.Read<int> (source));
            }

            public void Serialize (in Task task, Span<byte> destination, TaskReferenceStore references)
            {
                MemoryMarshal.Write (destination, ref Unsafe.AsRef (task.Data));
            }
        }
    }
}
