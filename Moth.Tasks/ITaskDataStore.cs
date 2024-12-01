namespace Moth.Tasks
{
    public interface ITaskDataStore
    {
        int FirstTask { get; }

        int LastTaskEnd { get; }

        int Size { get; }

        void Clear ();

        T Dequeue<T> (ITaskInfo<T> taskInfo) where T : struct, ITaskType;

        void Enqueue<T> (in T task, ITaskInfo<T> taskInfo) where T : struct, ITaskType;

        void Insert<T> (int dataIndex, int refIndex, T task, ITaskInfo<T> taskInfo) where T : struct, ITaskType;

        void Skip (ITaskInfo taskInfo);
    }
}