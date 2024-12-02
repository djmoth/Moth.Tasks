namespace Moth.Tasks
{
    using Moth.IO.Serialization;
    using System;
    using System.Linq;

    using IFormatProvider = Moth.IO.Serialization.IFormatProvider;

    public class TaskInfoProvider : ITaskInfoProvider
    {
        private readonly IFormatProvider formatProvider;

        public TaskInfoProvider (IFormatProvider formatProvider)
        {
            this.formatProvider = formatProvider;
        }

        public ITaskInfo<TTask> Create<TTask> (int id)
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
                    taskInfoType = typeof (DisposableTaskInfo<>).MakeGenericType (type);
                } else if (interfaceType.GetGenericTypeDefinition () == typeof (ITask<>))
                {
                    taskInfoType = typeof (DisposableTaskInfo<,>).MakeGenericType (interfaceType.GetGenericArguments ().Prepend (type).ToArray ());
                } else if (interfaceType.GetGenericTypeDefinition () == typeof (ITask<,>))
                {
                    taskInfoType = typeof (DisposableTaskInfo<,,>).MakeGenericType (interfaceType.GetGenericArguments ().Prepend (type).ToArray ());
                } else
                {
                    throw new NotImplementedException ();
                }
            } else
            {
                if (interfaceType == typeof (ITask))
                {
                    taskInfoType = typeof (TaskInfo<>).MakeGenericType (type);
                } else if (interfaceType.GetGenericTypeDefinition () == typeof (ITask<>))
                {
                    taskInfoType = typeof (TaskInfo<,>).MakeGenericType (interfaceType.GetGenericArguments ().Prepend (type).ToArray ());
                } else if (interfaceType.GetGenericTypeDefinition () == typeof (ITask<,>))
                {
                    taskInfoType = typeof (TaskInfo<,,>).MakeGenericType (interfaceType.GetGenericArguments ().Prepend (type).ToArray ());
                } else
                {
                    throw new NotImplementedException ();
                }
            }

            return (ITaskInfo<TTask>)Activator.CreateInstance (taskInfoType, id, (IFormat<TTask>)formatProvider.Get<TTask> ());
        }
    }
}
