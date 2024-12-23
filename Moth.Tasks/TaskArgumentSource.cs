namespace Moth.Tasks
{
    /// <summary>
    /// Represents a method that can supply an argument for a task.
    /// </summary>
    /// <typeparam name="TArg">Type of task argument.</typeparam>
    /// <returns>An instance of <typeparamref name="TArg"/>.</returns>
    public delegate TArg TaskArgumentSource<TArg> ();
}
