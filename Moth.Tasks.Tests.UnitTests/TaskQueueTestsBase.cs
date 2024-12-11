namespace Moth.Tasks.Tests.UnitTests
{
    using Moq;
    using NUnit.Framework;

    public class TaskQueueTestsBase
    {
        private Mock<ITaskMetadataCache> mockTaskCache;
        private Mock<ITaskDataStore> mockTaskDataStore;
        private Mock<ITaskHandleManager> mockTaskHandleManager;
        private MockTaskMetadata<TestTask> mockTestTaskMetadata;
        private MockTaskMetadata<TaskWithHandle<TaskWrapper<TestTask>, Unit, Unit>, Unit, Unit> mockTestTaskWithHandle;
        private MockTaskMetadata<DisposableTestTask> mockDisposableTestTaskMetadata;
        private MockTaskMetadata<TaskWithHandle<TaskWrapper<DisposableTestTask>, Unit, Unit>, Unit, Unit> mockDisposableTestTaskWithHandle;
        private Mock<IProfiler> mockProfiler;
    }
}
