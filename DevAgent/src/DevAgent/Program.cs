using System;
using System.Threading;

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

            using (var cts = new CancellationTokenSource())
            {
                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    Console.WriteLine("Application is shutting down...");
                    cts.Cancel();

                    // Don't terminate the process immediately, wait for the Main thread to exit gracefully.
                    eventArgs.Cancel = true;
                };

                if (remoteUri != null)
                {
                    using (var messageHub = new MessageHub(remoteUri, cts.Token))
                    {
                        messageHub.WaitUntilConnectedAsync().Wait();

                        messageHub.MessageReceived += (sender, message) =>
                        {
                            Console.WriteLine($"Received message with method '{message.Method}' and id '{message.Id}'");
                        };

                        while (!cts.Token.IsCancellationRequested)
                        {
                            try
                            {
                                messageHub.SendMessage(new Message() { Id = Guid.NewGuid().ToString(), Method = "SendFile" });

                                Thread.Sleep(1000);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                                return;
                            }
                        }
                    }
                }

                if (localPort != 0)
                {
                    using (var messageHub = new MessageHub(localPort, cts.Token))
                    {
                        messageHub.WaitUntilConnectedAsync().Wait();

                        messageHub.MessageReceived += (sender, message) =>
                        {
                            Console.WriteLine($"Received message with method '{message.Method}' and id '{message.Id}'");
                        };

                        while (!cts.Token.IsCancellationRequested)
                        {
                            try
                            {
                                messageHub.SendMessage(new Message() { Id = Guid.NewGuid().ToString(), Method = "SendFile" });

                                Thread.Sleep(1000);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                                return;
                            }
                        }
                    }
                }
            }
        }
    }
}
