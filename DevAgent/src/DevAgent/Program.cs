using System;
using System.Threading;
using System.Threading.Tasks;

namespace DevAgent
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (args.Length == 0)
            {
                throw new ArgumentException(null, nameof(args));
            }

            Uri remoteUri;
            Uri.TryCreate(args[0], UriKind.Absolute, out remoteUri);

            int localPort;
            int.TryParse(args[0], out localPort);

            string localRootDirectory = args[1];

            using (var cts = new CancellationTokenSource())
            {
                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    Console.WriteLine("Application is shutting down...");
                    cts.Cancel();

                    // Don't terminate the process immediately, wait for the Main thread to exit gracefully.
                    eventArgs.Cancel = true;
                };

                FileSynchronizer fileSync = null;

                if (remoteUri != null)
                {
                    Task.Run(() => fileSync = new FileSynchronizer(localRootDirectory, new MessageHub(remoteUri, cts.Token)));

                    while (!cts.Token.IsCancellationRequested)
                    {
                        Thread.Sleep(100);
                    }
                }

                if (localPort != 0)
                {
                    Task.Run(() => fileSync = new FileSynchronizer(localRootDirectory, new MessageHub(localPort, cts.Token)));

                    while (!cts.Token.IsCancellationRequested)
                    {
                        Thread.Sleep(100);
                    }
                }
            }
        }
    }
}
