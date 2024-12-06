namespace Moth.Tasks
{
    using System;

    /// <summary>
    /// Represents a context for inserting references into an <see cref="ITaskReferenceStore"/>.
    /// </summary>
    public readonly struct TaskReferenceInsertContext : IDisposable
    {
        private readonly Action onDisposeAction;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskReferenceInsertContext"/> struct.
        /// </summary>
        /// <param name="onDisposeAction"><see cref="Action"/> to call on <see cref="Dispose"/>.</param>
        internal TaskReferenceInsertContext (Action onDisposeAction)
        {
            this.onDisposeAction = onDisposeAction;
        }

        /// <summary>
        /// Disposes the insert context.
        /// </summary>
        public void Dispose () => onDisposeAction?.Invoke ();
    }
}
