namespace Moth.Tasks
{
    using System;
    using Moth.IO.Serialization;

    /// <summary>
    /// Represents a task that takes no argument and returns no result.
    /// </summary>
    /// <typeparam name="TTask">Type of task.</typeparam>
    public interface ITaskInfo<TTask> : ITaskInfo
    {
        /// <summary>
        /// Serializes the task data.
        /// </summary>
        /// <param name="task">Task data to serialize.</param>
        /// <param name="destination">Destination to serialize to.</param>
        /// <param name="refWriter"><see cref="ObjectWriter"/> for handling any reference fields.</param>
        void Serialize (in TTask task, Span<byte> destination, ObjectWriter refWriter);

        /// <summary>
        /// Deserializes the task data.
        /// </summary>
        /// <param name="task">Deserialized task data.</param>
        /// <param name="source">Source to deserialize from.</param>
        /// <param name="refReader"><see cref="ObjectReader"/> for handling any reference fields.</param>
        void Deserialize (out TTask task, ReadOnlySpan<byte> source, ObjectReader refReader);
    }
}
