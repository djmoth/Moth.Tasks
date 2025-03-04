namespace Moth.Tasks;

public interface IWorker<TArg, TResult> : IWorker
{
    ITaskQueue<TArg, TResult> Tasks { get; }
}
