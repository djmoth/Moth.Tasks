namespace Moth.Tasks
{
    public interface IDisposableTaskInfo
    {
        void Dispose (TaskQueue.TaskDataAccess access);
    }
}
