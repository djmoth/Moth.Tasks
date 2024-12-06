namespace Moth.Tasks
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Cache of <see cref="ITaskMetadata"/>.
    /// </summary>
    public class TaskMetadataCache : ITaskMetadataCache
    {
        private readonly object syncRoot = new object ();
        private readonly ITaskMetadataProvider taskInfoProvider;
        private readonly Dictionary<Type, int> idCache = new Dictionary<Type, int> (16);
        private ITaskMetadata[] taskCache = new ITaskMetadata[16];
        private int nextID;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskMetadataCache"/> class, using <see cref="TaskMetadata.Provider"/> as <see cref="ITaskMetadataProvider"/>.
        /// </summary>
        public TaskMetadataCache ()
            : this (TaskMetadata.Provider) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskMetadataCache"/> class, using <paramref name="taskInfoProvider"/> as <see cref="ITaskMetadataProvider"/>.
        /// </summary>
        /// <param name="taskInfoProvider">The <see cref="ITaskMetadataProvider"/> to use for creating task information.</param>
        /// <exception cref="ArgumentNullException"><paramref name="taskInfoProvider"/> is null.</exception>
        public TaskMetadataCache (ITaskMetadataProvider taskInfoProvider)
        {
            this.taskInfoProvider = taskInfoProvider ?? throw new ArgumentNullException (nameof (taskInfoProvider));
        }

        /// <summary>
        /// Get a task by type.
        /// </summary>
        /// <typeparam name="T">Type of task.</typeparam>
        /// <returns><see cref="ITaskMetadata"/> for <typeparamref name="T"/>.</returns>
        public ITaskMetadata<T> GetTask<T> ()
            where T : struct, ITaskType
        {
            lock (syncRoot)
            {
                if (!idCache.TryGetValue (typeof (T), out int id))
                {
                    ITaskMetadata<T> task = AddTask<T> ();
                    return task;
                }

                return (ITaskMetadata<T>)taskCache[id];
            }
        }

        /// <summary>
        /// Get task by id.
        /// </summary>
        /// <param name="id">Assigned id of task.</param>
        /// <returns><see cref="ITaskMetadata"/> for <paramref name="id"/>.</returns>
        public ITaskMetadata GetTask (int id)
        {
            lock (syncRoot)
            {
                return taskCache[id];
            }
        }

        private ITaskMetadata<T> AddTask<T> () where T : struct, ITaskType
        {
            int id = nextID;
            nextID++;

            idCache.Add (typeof (T), id);

            ITaskMetadata<T> task = taskInfoProvider.Create<T> (id);

            if (id >= taskCache.Length)
            {
                Array.Resize (ref taskCache, taskCache.Length * 2);
            }

            taskCache[id] = task;

            return task;
        }
    }
}
