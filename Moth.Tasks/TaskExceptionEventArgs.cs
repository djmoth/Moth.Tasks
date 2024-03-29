﻿namespace Moth.Tasks
{
    using System;

    /// <summary>
    /// Contains information about an exception that was thrown in a task.
    /// </summary>
    public class TaskExceptionEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TaskExceptionEventArgs"/> class.
        /// </summary>
        /// <param name="exception"><see cref="System.Exception"/> thrown.</param>
        public TaskExceptionEventArgs (Exception exception)
        {
            Exception = exception;
        }

        /// <summary>
        /// <see cref="System.Exception"/> thrown.
        /// </summary>
        public Exception Exception { get; }
    }
}
