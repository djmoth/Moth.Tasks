namespace Moth.Tasks
{
    /// <summary>
    /// Represents a method that provides a new <see cref="IWorker"/> for a worker group, given an index of the worker in the group.
    /// </summary>
    /// <param name="workerIndex">The index of the <see cref="IWorker"/> in the group.</param>
    /// <returns>A new <see cref="IWorker"/>.</returns>
    /// <remarks>This method should create a new unique <see cref="IWorker"/> for each call. The method may be called multiple times with the same index if the worker group was resized.</remarks>
    public delegate IWorker WorkerProvider (int workerIndex);
}
