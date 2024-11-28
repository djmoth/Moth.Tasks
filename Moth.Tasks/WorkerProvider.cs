namespace Moth.Tasks
{
    /// <summary>
    /// Represents a method that provides a new <see cref="IWorker"/> for a <see cref="WorkerGroup"/>, given an index of the worker in the group.
    /// </summary>
    /// <param name="workerGroup">The <see cref="WorkerGroup"/> the <see cref="IWorker"/> will belong to.</param>
    /// <param name="workerIndex">The index of the <see cref="IWorker"/> in the <see cref="WorkerGroup"/>.</param>
    /// <returns>A new <see cref="IWorker"/>.</returns>
    /// <remarks>This method should create a new unique <see cref="IWorker"/> for each call. The method may be called multiple times with the same parameters if the <see cref="WorkerGroup"/> was resized.</remarks>
    public delegate IWorker WorkerProvider (WorkerGroup workerGroup, int workerIndex);
}
