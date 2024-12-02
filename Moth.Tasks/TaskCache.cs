namespace Moth.Tasks
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Cache of <see cref="ITaskInfo"/>.
    /// </summary>
    public class TaskCache : ITaskCache
    {
        private readonly object syncRoot = new object ();
        private readonly ITaskInfoProvider taskInfoProvider;
        private readonly Dictionary<Type, int> idCache = new Dictionary<Type, int> (16);
        private ITaskInfo[] taskCache = new ITaskInfo[16];
        private int nextID;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskCache"/> class, using <see cref="TaskInfo.Provider"/> as <see cref="ITaskInfoProvider"/>.
        /// </summary>
        public TaskCache ()
            : this (TaskInfo.Provider) { }


        /// <summary>
        /// Initializes a new instance of the <see cref="TaskCache"/> class, using <paramref name="taskInfoProvider"/> as <see cref="ITaskInfoProvider"/>.
        /// </summary>
        /// <param name="taskInfoProvider">The <see cref="ITaskInfoProvider"/> to use for creating task information.</param>
        /// <exception cref="ArgumentNullException"><paramref name="taskInfoProvider"/> is null.</exception>
        public TaskCache (ITaskInfoProvider taskInfoProvider)
        {
            this.taskInfoProvider = taskInfoProvider ?? throw new ArgumentNullException (nameof (taskInfoProvider));
        }

        /// <summary>
        /// Get a task by type.
        /// </summary>
        /// <typeparam name="T">Type of task.</typeparam>
        /// <returns><see cref="ITaskInfo"/> for <typeparamref name="T"/>.</returns>
        public ITaskInfo<T> GetTask<T> ()
            where T : struct, ITaskType
        {
            lock (syncRoot)
            {
                if (!idCache.TryGetValue (typeof (T), out int id))
                {
                    ITaskInfo<T> task = AddTask<T> ();
                    return task;
                }

                return (ITaskInfo<T>)taskCache[id];
            }
        }

        /// <summary>
        /// Get task by id.
        /// </summary>
        /// <param name="id">Assigned id of task.</param>
        /// <returns><see cref="ITaskInfo"/> for <paramref name="id"/>.</returns>
        public ITaskInfo GetTask (int id)
        {
            lock (syncRoot)
            {
                return taskCache[id];
            }
        }

        private ITaskInfo<T> AddTask<T> () where T : struct, ITaskType
        {
            int id = nextID;
            nextID++;

            idCache.Add (typeof (T), id);

            ITaskInfo<T> task = taskInfoProvider.Create<T> (id);

            if (id >= taskCache.Length)
            {
                Array.Resize (ref taskCache, taskCache.Length * 2);
            }

            taskCache[id] = task;

            return task;
        }
    }
}
