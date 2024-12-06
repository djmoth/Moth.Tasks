namespace Moth.Tasks
{
    using System;
    using Moth.IO.Serialization;

    /// <summary>
    /// Static class for generating <see cref="ITaskInfo"/> instances.
    /// </summary>
    public static class TaskInfo
    {
        /// <summary>
        /// <see cref="TaskInfoProvider"/> for creating <see cref="ITaskInfo"/> instances using <see cref="Format.Provider"/> for serialization.
        /// </summary>
        public static readonly TaskInfoProvider Provider = new TaskInfoProvider (Format.Provider);

        /// <summary>
        /// Creates a new <see cref="ITaskInfo{TTask}"/> instance from a type and ID.
        /// </summary>
        /// <typeparam name="TTask">Type of task.</typeparam>
        /// <param name="id">ID of task.</param>
        /// <returns>A new <see cref="ITaskInfo{TTask}"/> instance representing the task <typeparamref name="TTask"/>.</returns>
        internal static unsafe ITaskInfo<TTask> Create<TTask> (int id)
             where TTask : struct, ITaskType
            => Provider.Create<TTask> (id);
    }
}
