namespace Moth.Tasks
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Cache of <see cref="ITaskInfo"/>.
    /// </summary>
    public class TaskCache
    {
        private readonly object syncRoot = new object ();
        private readonly Dictionary<Type, int> idCache = new Dictionary<Type, int> (16);
        private ITaskInfo[] taskCache = new ITaskInfo[16];
        private int nextID;

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

            ITaskInfo<T> task = TaskInfo.Create<T> (id);

            if (id >= taskCache.Length)
            {
                Array.Resize (ref taskCache, taskCache.Length * 2);
            }

            taskCache[id] = task;

            return task;
        }
    }
}
