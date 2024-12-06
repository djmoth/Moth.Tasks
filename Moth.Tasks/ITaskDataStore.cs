namespace Moth.Tasks
{
    /// <summary>
    /// Interface for a task data store.
    /// </summary>
    public interface ITaskDataStore
    {
        /// <summary>
        /// Gets the index of the first byte of first task data in the store.
        /// </summary>
        int FirstTask { get; }

        /// <summary>
        /// Gets the index of the last byte of the last task data in the store.
        /// </summary>
        int LastTaskEnd { get; }

        /// <summary>
        /// Gets the total size of the data in the store in bytes.
        /// </summary>
        int Size { get; }

        /// <summary>
        /// Gets the current capacity of the store in bytes.
        /// </summary>
        int Capacity { get; }

        /// <summary>
        /// Clears the task data store.
        /// </summary>
        void Clear ();

        /// <summary>
        /// Dequeues a task from the store.
        /// </summary>
        /// <typeparam name="T">The type of the task data.</typeparam>
        /// <param name="taskInfo"><see cref="ITaskMetadata{T}"/> instance containing task type information.</param>
        /// <returns>The task data.</returns>
        T Dequeue<T> (ITaskMetadata<T> taskInfo) where T : struct, ITaskType;

        /// <summary>
        /// Enqueues a task into the store.
        /// </summary>
        /// <typeparam name="T">The type of the task data.</typeparam>
        /// <param name="task">The task data to enqueue.</param>
        /// <param name="taskInfo"><see cref="ITaskMetadata{T}"/> instance containing task type information.</param>
        void Enqueue<T> (in T task, ITaskMetadata<T> taskInfo) where T : struct, ITaskType;

        /// <summary>
        /// Inserts a task into the store at a specific index.
        /// </summary>
        /// <typeparam name="T">The type of the task data.</typeparam>
        /// <param name="dataIndex">Insertion index of unmanaged data.</param>
        /// <param name="refIndex">Inseration index of managed references.</param>
        /// <param name="task">Task data to insert.</param>
        /// <param name="taskInfo"><see cref="ITaskMetadata{T}"/> instance containing task type information.</param>
        void Insert<T> (ref int dataIndex, ref int refIndex, in T task, ITaskMetadata<T> taskInfo) where T : struct, ITaskType;

        /// <summary>
        /// Skips a task in the store.
        /// </summary>
        /// <param name="taskInfo"><see cref="ITaskMetadata"/> instance containing task type information.</param>
        void Skip (ITaskMetadata taskInfo);
    }
}