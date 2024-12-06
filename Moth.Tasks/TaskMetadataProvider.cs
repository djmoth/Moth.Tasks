namespace Moth.Tasks
{
    using System;
    using System.Linq;
    using Moth.IO.Serialization;
    using IFormatProvider = Moth.IO.Serialization.IFormatProvider;

    /// <summary>
    /// Provides <see cref="ITaskMetadata{TTask}"/> instances.
    /// </summary>
    public class TaskMetadataProvider : ITaskMetadataProvider
    {
        private readonly IFormatProvider formatProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskMetadataProvider"/> class.
        /// </summary>
        /// <param name="formatProvider"><see cref="IFormatProvider"/> for serializing task data.</param>
        public TaskMetadataProvider (IFormatProvider formatProvider)
        {
            this.formatProvider = formatProvider;
        }

        /// <inheritdoc />
        public ITaskMetadata<TTask> Create<TTask> (int id)
            where TTask : struct, ITaskType
        {
            Type type = typeof (TTask);

            bool isDisposable = false;
            Type interfaceType = null;

            foreach (Type i in type.GetInterfaces ())
            {
                if (i == typeof (IDisposable))
                {
                    isDisposable = true;
                } else if (i == typeof (ITask))
                {
                    interfaceType = i;
                } else if (i.IsGenericType && (i.GetGenericTypeDefinition () == typeof (ITask<>) || i.GetGenericTypeDefinition () == typeof (ITask<,>)))
                {
                    if (interfaceType != null)
                        throw new InvalidOperationException ("Task type is ambiguous.");

                    interfaceType = i;
                }
            }

            if (interfaceType == null)
                throw new InvalidOperationException ("Task type does not implement ITask or its generic variants.");

            Type taskInfoType;

            if (isDisposable)
            {
                if (interfaceType == typeof (ITask))
                {
                    taskInfoType = typeof (DisposableTaskMetadata<>).MakeGenericType (type);
                } else if (interfaceType.GetGenericTypeDefinition () == typeof (ITask<>))
                {
                    taskInfoType = typeof (DisposableTaskMetadata<,>).MakeGenericType (interfaceType.GetGenericArguments ().Prepend (type).ToArray ());
                } else if (interfaceType.GetGenericTypeDefinition () == typeof (ITask<,>))
                {
                    taskInfoType = typeof (DisposableTaskMetadata<,,>).MakeGenericType (interfaceType.GetGenericArguments ().Prepend (type).ToArray ());
                } else
                {
                    throw new NotImplementedException ();
                }
            } else
            {
                if (interfaceType == typeof (ITask))
                {
                    taskInfoType = typeof (TaskMetadata<>).MakeGenericType (type);
                } else if (interfaceType.GetGenericTypeDefinition () == typeof (ITask<>))
                {
                    taskInfoType = typeof (TaskMetadata<,>).MakeGenericType (interfaceType.GetGenericArguments ().Prepend (type).ToArray ());
                } else if (interfaceType.GetGenericTypeDefinition () == typeof (ITask<,>))
                {
                    taskInfoType = typeof (TaskMetadata<,,>).MakeGenericType (interfaceType.GetGenericArguments ().Prepend (type).ToArray ());
                } else
                {
                    throw new NotImplementedException ();
                }
            }

            return (ITaskMetadata<TTask>)Activator.CreateInstance (taskInfoType, id, (IFormat<TTask>)formatProvider.Get<TTask> ());
        }
    }
}
