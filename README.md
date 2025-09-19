# Moth.Tasks
**Low-allocation async/synchronous task library**

Provided as an alternative to the built-in `System.Threading.Tasks` system, for high-performance applications where excessive garbage collection is undesired.

## Motive
When using the `System.Threading.Tasks.Task` type, a new object will be created to store data for each task enqueued. 
This is not a problem when a small number of tasks are enqueued, yet in high-performance applications,
this extra pressure on the garbage collector can result in adverse performance. 

The `Moth.Tasks` package can mitigate this problem, by using an internal buffer for task data, so as the only allocation is the task buffer itself.

## How to Use
The [`TaskQueue`](https://djmoth.github.io/Moth.Tasks/api/Moth.Tasks.TaskQueue.html) type provides methods for enqueueing and executing tasks.

Tasks can be enqueued as a [`System.Action`](https://docs.microsoft.com/en-us/dotnet/api/system.action)/[`System.Action<T1, ...>`](https://docs.microsoft.com/en-us/dotnet/api/system.action-1) delegate:
```C#
// Create a new TaskQueue
TaskQueue tasks = new TaskQueue ();

// Enqueue a task taking two integer parameters: a & b, as an Action<int, int>
tasks.Enqueue ((int a, int b) =>
{
    int result = a + b; // Add a & b together
    Console.WriteLine ($"The result of {a} + {b} is {result}"); // Print the result to the console
}, 2, 2); // 2 & 2 is supplied as arguments for both parameters down here
```
or as a struct implementing the [`ITask`](https://djmoth.github.io/Moth.Tasks/api/Moth.Tasks.ITask.html) interface:
```C#
struct AdditionTask : ITask
{
    public int A, B; // Two numbers which will be added together
    
    // ITask.Run is invoked when the task is executed
    public void Run ()
    {
        int result = A + B; // Add a & b together
        Console.WriteLine ($"The result of {a} + {b} is {result}"); // Print the result to the console
    }
}

...

// Enqueue an AdditionTask with 2 as arguments for A & B
tasks.Enqueue (new AdditionTask { A = 2, B = 2 });
```
To execute the enqueued tasks, invoke either the [`TaskQueue.RunNextTask`](https://djmoth.github.io/Moth.Tasks/api/Moth.Tasks.TaskQueue.html#Moth_Tasks_TaskQueue_RunNextTask_Moth_Tasks_IProfiler_System_Threading_CancellationToken_)
or [`TaskQueue.TryRunNextTask`](https://djmoth.github.io/Moth.Tasks/api/Moth.Tasks.TaskQueue.html#Moth_Tasks_TaskQueue_TryRunNextTask_Moth_Tasks_IProfiler_) method:
```C#
if (tasks.TryRunNextTask ()) // Runs the next task in the TaskQueue and returns true, or returns false if the queue was empty
{
    ...
}

tasks.RunNextTask (); // Blocks until a task is enqueued, then runs it
```
To execute tasks in a background thread, the [`Worker`](https://djmoth.github.io/Moth.Tasks/api/Moth.Tasks.Worker.html) & [`WorkerGroup`](https://djmoth.github.io/Moth.Tasks/api/Moth.Tasks.WorkerGroup.html)
can be used:
```C#
// A Worker represents a Thread which will continuously execute tasks in the background 
Worker worker = new Worker (new TaskQueue (), true, true);

// A WorkerGroup consists of multiple workers, all executing tasks from the same TaskQueue
WorkerGroup workerGroup = new WorkerGroup (4, new TaskQueue (), true, true);
```
