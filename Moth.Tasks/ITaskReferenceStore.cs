using Moth.IO.Serialization;

namespace Moth.Tasks
{
    /// <summary>
    /// Stores reference fields from tasks.
    /// </summary>
    public interface ITaskReferenceStore
    {
        /// <summary>
        /// Gets the index of the first reference.
        /// </summary>
        int Start { get; }

        /// <summary>
        /// Gets the index after the last reference.
        /// </summary>
        int End { get; }

        /// <summary>
        /// Gets the total number of references stored.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the current capacity of the store.
        /// </summary>
        int Capacity { get; }

        /// <summary>
        /// Gets the <see cref="ObjectReader"/> for reading stored references.
        /// </summary>
        ObjectReader Read { get; }

        /// <summary>
        /// Gets the <see cref="ObjectWriter"/> for writing references.
        /// </summary>
        ObjectWriter Write { get; }

        /// <summary>
        /// Clears all stored references.
        /// </summary>
        void Clear ();

        /// <summary>
        /// Enters an insert context to insert references.
        /// </summary>
        /// <param name="insertIndex">The index to start inserting at</param>
        /// <param name="refCount">The total number of references that will be inserted.</param>
        /// <param name="insertWriter">The <see cref="ObjectWriter"/> to write references to.</param>
        /// <returns>A <see cref="TaskReferenceInsertContext"/> on which <see cref="TaskReferenceInsertContext.Dispose"/> must be called when done.</returns>
        TaskReferenceInsertContext EnterInsertContext (ref int insertIndex, int refCount, out ObjectWriter insertWriter);

        /// <summary>
        /// Skips a number of references.
        /// </summary>
        /// <param name="refCount">The number of references to skip.</param>
        void Skip (int refCount);
    }
}