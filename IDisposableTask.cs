namespace Moth.Tasks
{
    public interface IDisposableTask : ITask
    {
        void Dispose (bool cancelled);
    }
}
