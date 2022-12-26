namespace Moth.Tasks
{
    public interface ITaskQueue
    {
        public void Enqueue<T> (T task) where T : ITask;
    }
}
