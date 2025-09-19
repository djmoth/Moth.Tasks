namespace Moth.Tasks
{
    /// <summary>
    /// Represents a callback method to be called when a task completes with a result.
    /// </summary>
    /// <typeparam name="TResult">The type of the result produced by the task.</typeparam>
    /// <param name="result">The result produced by the task.</param>
    public delegate void TaskResultCallback<TResult> (TResult result);
}
