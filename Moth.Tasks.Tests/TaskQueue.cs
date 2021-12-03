using NUnit.Framework;
using System;

namespace Moth.Tasks.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup ()
        {

        }

        [Test]
        public void EnqueueITask ()
        {

        }

        [Test]
        public void EnqueueAndTryRunTask ()
        {
            TaskQueue queue = new TaskQueue ();

            TaskResult result = new TaskResult ();
            int value = 42;

            queue.Enqueue (new PutValueTask (value, result));

            queue.TryRunNextTask (out Exception ex);

            Assert.IsNull (ex, ex?.Message);

            Assert.AreEqual (value, result.Value, $"Incorrect value of result: {result.Value}");
            

        }

        class TaskResult
        {
            public int Value { get; set; }
        }

        readonly struct PutValueTask : ITask
        {
            private readonly int value;
            private readonly TaskResult result;

            public PutValueTask (int value, TaskResult result)
            {
                this.value = value;
                this.result = result;
            }

            public void Run ()
            {
                result.Value = value;
            }
        }
    }
}