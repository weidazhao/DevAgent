﻿using System;
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
                    using (var messageHub = new MessageHub(remoteUri))
                    {
                        messageHub.WaitUntilConnectedAsync().Wait();

                        messageHub.MessageReceived += (s, e) =>
                        {
                            Console.WriteLine($"Received message with method '{e.Method}' and id '{e.Id}'");
                        };

                        while (!cts.Token.IsCancellationRequested)
                        {
                            messageHub.SendMessage(new Message() { Id = Guid.NewGuid().ToString(), Method = "SendFile" });

                            Thread.Sleep(1000);
                        }
                    }
                }

                if (localPort != 0)
                {
                    using (var messageHub = new MessageHub(localPort))
                    {
                        messageHub.WaitUntilConnectedAsync().Wait();

                        messageHub.MessageReceived += (s, e) =>
                        {
                            Console.WriteLine($"Received message with method '{e.Method}' and id '{e.Id}'");
                        };

                        while (!cts.Token.IsCancellationRequested)
                        {
                            messageHub.SendMessage(new Message() { Id = Guid.NewGuid().ToString(), Method = "SendFile" });

                            Thread.Sleep(1000);
                        }
                    }
                }
            }
        }
    }
}
