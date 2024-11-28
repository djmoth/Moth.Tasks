namespace Moth.Tasks
{
    /// <summary>
    /// Represents a method that provides an <see cref="IWorkerThread"/> for an <see cref="IWorker"/>.
    /// </summary>
    /// <param name="worker">The <see cref="IWorker"/> to provide for.</param>
    /// <returns>A new <see cref="IWorkerThread"/>.</returns>
    /// <remarks>This method should create a new unique <see cref="IWorkerThread"/> for each call, which must not have been started beforehand.</remarks>
    public delegate IWorkerThread WorkerThreadProvider (IWorker worker);
}
