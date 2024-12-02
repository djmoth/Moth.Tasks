namespace Moth.Tasks.Tests.UnitTests
{
    using Moq;
    using Moth.IO.Serialization;
    using NUnit.Framework;
    using System;

    using IFormatProvider = Moth.IO.Serialization.IFormatProvider;

    [TestFixture]
    public class TaskInfoProviderTests
    {
        private Mock<IFormatProvider> mockFormatProvider;

        [SetUp]
        public void SetUp ()
        {
            mockFormatProvider = new Mock<IFormatProvider> ();
        }

        [Test]
        public void Create_TestTask_ReturnsCorrectType () => TestCreatesCorrectTaskInfoType<TestTask> (typeof (TaskInfo<TestTask>));

        [Test]
        public void Create_TestTaskArg_ReturnsCorrectType () => TestCreatesCorrectTaskInfoType<TestTaskArg> (typeof (TaskInfo<TestTaskArg, int>));

        [Test]
        public void Create_TestTaskArgResult_ReturnsCorrectType () => TestCreatesCorrectTaskInfoType<TestTaskArgResult> (typeof (TaskInfo<TestTaskArgResult, int, int>));

        [Test]
        public void Create_DisposableTestTask_ReturnsCorrectType () => TestCreatesCorrectTaskInfoType<DisposableTestTask> (typeof (DisposableTaskInfo<DisposableTestTask>));

        [Test]
        public void Create_DisposableTestTaskArg_ReturnsCorrectType () => TestCreatesCorrectTaskInfoType<DisposableTestTaskArg> (typeof (DisposableTaskInfo<DisposableTestTaskArg, int>));

        [Test]
        public void Create_DisposableTestTaskArgResult_ReturnsCorrectType () => TestCreatesCorrectTaskInfoType<DisposableTestTaskArgResult> (typeof (DisposableTaskInfo<DisposableTestTaskArgResult, int, int>));
        public unsafe void TestCreatesCorrectTaskInfoType<T> (Type expectedTaskInfoType)
            where T : struct, ITaskType
        {
            int taskID = 1;

            mockFormatProvider.Setup (x => x.Get<T> ()).Returns (Mock.Of<IFormat<T>> ());

            TaskInfoProvider taskInfoProvider = new TaskInfoProvider (mockFormatProvider.Object);

            ITaskInfo<T> taskInfo = taskInfoProvider.Create<T> (taskID);

            Assert.That (taskInfo.GetType (), Is.EqualTo (expectedTaskInfoType));
        }

        public struct TestTask : ITask
        {
            public void Run () { }
        }

        public struct TestTaskArg : ITask<int>
        {
            public void Run (int i) { }
        }

        public struct TestTaskArgResult : ITask<int, int>
        {
            public int Run (int i) => i;
        }

        public struct DisposableTestTask : ITask, IDisposable
        {
            public void Dispose () { }

            public void Run () { }
        }

        public struct DisposableTestTaskArg : ITask<int>, IDisposable
        {
            public void Dispose () { }

            public void Run (int i) { }
        }

        public struct DisposableTestTaskArgResult : ITask<int, int>, IDisposable
        {
            public void Dispose () { }

            public int Run (int i) => i;
        }
    }
}
