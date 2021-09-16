namespace Moth.Tasks
{
    using System;
    using System.Collections.Generic;

    internal unsafe class TaskCache
    {
        private readonly Dictionary<Type, int> idCache = new Dictionary<Type, int> (16);
        private Task[] taskCache = new Task[16];
        private int nextID;

        public Task GetTask<T> () where T : struct, ITask
        {
            if (!idCache.TryGetValue (typeof (T), out int id))
            {
                Task task = AddTask<T> ();

                idCache.Add (typeof (T), task.ID);

                return task;
            }

            return taskCache[id];
        }

        public Task GetTask (int id)
        {
            return taskCache[id];
        }

        private Task AddTask<T> () where T : struct, ITask
        {
            int id = nextID;
            nextID++;

            idCache.Add (typeof (T), id);

            Task task = Task.Create<T> (id);

            if (id >= taskCache.Length)
            {
                Array.Resize (ref taskCache, taskCache.Length * 2);
            }

            taskCache[id] = task;

            return task;
        }
    }
}
