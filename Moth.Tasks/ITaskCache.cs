namespace Moth.Tasks
{
    public interface ITaskCache
    {
        ITaskInfo GetTask (int id);

        ITaskInfo<T> GetTask<T> () where T : struct, ITaskType;
    }
}