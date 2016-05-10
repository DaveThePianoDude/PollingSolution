using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using RestSharp;

namespace PollingMIDB
{
    class Poller
    {
        CancellationTokenSource wtoken;
        ActionBlock<DateTimeOffset> task;

        ITargetBlock<DateTimeOffset> CreateNeverEndingTask(
        Action<DateTimeOffset> action, CancellationToken cancellationToken)
        {
            // Validate parameters.
            if (action == null) throw new ArgumentNullException("action");

            // Declare the block variable, it needs to be captured.
            ActionBlock<DateTimeOffset> block = null;

            // Create the block, it will call itself, so
            // you need to separate the declaration and
            // the assignment.
            // Async so you can wait easily when the
            // delay comes.
            block = new ActionBlock<DateTimeOffset>(async now => {
                // Perform the action.
                action(now);

                // Wait.
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken).
                    // Doing this here because synchronization context more than
                    // likely *doesn't* need to be captured for the continuation
                    // here.  As a matter of fact, that would be downright
                    // dangerous.
                    ConfigureAwait(false);

                // Post the action back to the block.
                block.Post(DateTimeOffset.Now);
            }, new ExecutionDataflowBlockOptions
            {
                CancellationToken = cancellationToken
            });

            // Return the block.
            return block;
        }
        void DoWork(DateTimeOffset now)
        {
            // Some work that takes up to 30 seconds but isn't returning anything.
            Console.WriteLine("Did some work");
        }

        public void StartWork()
        {
            // Create the token source.
            wtoken = new CancellationTokenSource();

            // Set the task.
            task = (ActionBlock<DateTimeOffset>)CreateNeverEndingTask(now => DoWork(now), wtoken.Token);

            // Start the task.  Post the time.
            task.Post(DateTimeOffset.Now);
        }
        void StopWork()
        {
            // CancellationTokenSource implements IDisposable.
            using (wtoken)
            {
                // Cancel.  This will cancel the task.
                wtoken.Cancel();
            }

            // Set everything to null, since the references
            // are on the class level and keeping them around
            // is holding onto invalid state.
            wtoken = null;
            task = null;
        }

    }

    class Program
    {
        static CancellationTokenSource wtoken;
        static ActionBlock<DateTimeOffset> task;

        static ITargetBlock<DateTimeOffset> CreateNeverEndingTask(
        Action<DateTimeOffset> action, CancellationToken cancellationToken)
        {
            // Validate parameters.
            if (action == null) throw new ArgumentNullException("action");

            // Declare the block variable, it needs to be captured.
            ActionBlock<DateTimeOffset> block = null;

            // Create the block, it will call itself, so
            // you need to separate the declaration and
            // the assignment.
            // Async so you can wait easily when the
            // delay comes.
            block = new ActionBlock<DateTimeOffset>(async now => {
                // Perform the action.
                action(now);

                // Wait.
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken).
                    // Doing this here because synchronization context more than
                    // likely *doesn't* need to be captured for the continuation
                    // here.  As a matter of fact, that would be downright
                    // dangerous.
                    ConfigureAwait(false);

                // Post the action back to the block.
                block.Post(DateTimeOffset.Now);
            }, new ExecutionDataflowBlockOptions
            {
                CancellationToken = cancellationToken
            });

            // Return the block.
            return block;
        }
        static void DoWork(DateTimeOffset now)
        {
            // Some work that takes up to 30 seconds but isn't returning anything.

            var baseUrl = "http://192.168.1.9:9200/";
            var client = new RestClient(baseUrl);
            var request = new RestRequest("fishnet_midb/object/_count", Method.GET);

            // execute the request
            IRestResponse response = client.Execute(request);
            var content = response.Content; // raw content as string

            Console.WriteLine(content);
        }

        static public void StartWork()
        {
            // Create the token source.
            wtoken = new CancellationTokenSource();

            // Set the task.
            task = (ActionBlock<DateTimeOffset>)CreateNeverEndingTask(now => DoWork(now), wtoken.Token);

            // Start the task.  Post the time.
            task.Post(DateTimeOffset.Now);
        }
        static void StopWork()
        {
            // CancellationTokenSource implements IDisposable.
            using (wtoken)
            {
                // Cancel.  This will cancel the task.
                wtoken.Cancel();
            }

            // Set everything to null, since the references
            // are on the class level and keeping them around
            // is holding onto invalid state.
            wtoken = null;
            task = null;
        }

        class MyCustomData
        {
            public long CreationTime;
            public int Name;
            public int ThreadNum;
        }

        static void TaskDemo2()
        {
            // Create the task object by using an Action(Of Object) to pass in custom data
            // in the Task constructor. This is useful when you need to capture outer variables
            // from within a loop. As an experiement, try modifying this code to 
            // capture i directly in the lambda, and compare results.
            Task[] taskArray = new Task[10];

            for (int i = 0; i < taskArray.Length; i++)
            {
                taskArray[i] = new Task((obj) =>
                {
                    MyCustomData mydata = (MyCustomData)obj;
                    mydata.ThreadNum = Thread.CurrentThread.ManagedThreadId;
                    Console.WriteLine("Hello from Task #{0} created at {1} running on thread #{2}.",
                                      mydata.Name, mydata.CreationTime, mydata.ThreadNum);
                },
                new MyCustomData() { Name = i, CreationTime = DateTime.Now.Ticks }
                );
                taskArray[i].Start();
            }
        }



        static void Main(string[] args)
        {
            //TaskDemo2();
            Task.Factory.StartNew(() => StartWork());

            Console.ReadLine();
        }
    }
}
