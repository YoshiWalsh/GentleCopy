using GentleCopy;
using GentleCopy.TaskProcessors;
using System;
using System.CommandLine;

namespace GentleCopy;

class Program
{
    /// <summary>
    /// Utility to recursively copy the contents of one directory to another. Designed to support instant-resume with no additional filesystem operations through the use of a persisted task queue.
    /// Intended for use with unreliable source filesystems.
    /// </summary>
    /// <param name="source">The path to copy files from.</param>
    /// <param name="destination">The path to copy files to.</param>
    /// <param name="queueFile">Where the persistent queue of operations should be stored, in order to allow resuming the operation. Optional, defaults to "transfer.log" in the current directory.</param>
    static void Main(string[] args)
    {
        string source = Path.Combine(Directory.GetCurrentDirectory(), args[0]);
        string destination = Path.Combine(Directory.GetCurrentDirectory(), args[1]);
        string queueFile = args.Length > 2 ? args[2] : "./transfer.tfq";

        NewQueueEntry initialTask = new NewQueueEntry()
        {
            Status = QueueEntryStatus.Queued,
            TaskType = Scan.TaskType,
            Path = source,
        };
        TaskQueue queue = new TaskQueue(initialTask, queueFile);
        queue.RegisterProcessor(new Scan(source, destination));
        queue.RegisterProcessor(new CopyFile(source, destination));

        Console.WriteLine("Queue loaded. Press enter when ready to start.");
        Console.ReadKey();

        queue.RunTasks();
    }
}