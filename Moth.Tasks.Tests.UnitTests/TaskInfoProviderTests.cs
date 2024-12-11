namespace Moth.Tasks.Tests.UnitTests
{
    using Moq;
    using Moth.IO.Serialization;
    using NUnit.Framework;
    using System;

    using IFormatProvider = Moth.IO.Serialization.IFormatProvider;

    [TestFixture]
    public class TaskMetadataProviderTests
    {
        private Mock<IFormatProvider> mockFormatProvider;

        [SetUp]
        public void SetUp ()
        {
            mockFormatProvider = new Mock<IFormatProvider> ();
        }

        [Test]
        public void Create_TestTask_ReturnsCorrectType () => TestCreatesCorrectTaskMetadataType<TestTask> (typeof (TaskMetadata<TestTask>));

        [Test]
        public void Create_TestTaskArg_ReturnsCorrectType () => TestCreatesCorrectTaskMetadataType<TestTaskArg> (typeof (TaskMetadata<TestTaskArg, int>));

        [Test]
        public void Create_TestTaskArgResult_ReturnsCorrectType () => TestCreatesCorrectTaskMetadataType<TestTaskArgResult> (typeof (TaskMetadata<TestTaskArgResult, int, int>));

        [Test]
        public void Create_DisposableTestTask_ReturnsCorrectType () => TestCreatesCorrectTaskMetadataType<DisposableTestTask> (typeof (TaskMetadata<DisposableTestTask>));

        [Test]
        public void Create_DisposableTestTaskArg_ReturnsCorrectType () => TestCreatesCorrectTaskMetadataType<DisposableTestTaskArg> (typeof (TaskMetadata<DisposableTestTaskArg, int>));

        [Test]
        public void Create_DisposableTestTaskArgResult_ReturnsCorrectType () => TestCreatesCorrectTaskMetadataType<DisposableTestTaskArgResult> (typeof (TaskMetadata<DisposableTestTaskArgResult, int, int>));

        public unsafe void TestCreatesCorrectTaskMetadataType<T> (Type expectedTaskMetadataType)
            where T : struct, ITask
        {
            int taskID = 1;

            mockFormatProvider.Setup (x => x.Get<T> ()).Returns (Mock.Of<IFormat<T>> ());

            TaskMetadataProvider taskInfoProvider = new TaskMetadataProvider (mockFormatProvider.Object);

            ITaskMetadata<T> taskInfo = taskInfoProvider.Create<T> (taskID);

            Assert.That (taskInfo.GetType (), Is.EqualTo (expectedTaskMetadataType));
        }

        public struct TestTask : ITask<Unit, Unit>
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

        public struct DisposableTestTask : ITask<Unit, Unit>, IDisposable
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
