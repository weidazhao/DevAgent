using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

            if (args.Length != 0)
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
            }
        }

        private static Socket OpenListeningSocket(int localPort)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Any, localPort));
            socket.Listen(1);

            return socket;
        }

        private static async Task<Stream> AcceptSocketAsync(Socket listeningSocket, CancellationToken cancellationToken)
        {
            var socket = await listeningSocket.AcceptAsync();

            return new NetworkStream(socket, ownsSocket: true);
        }

        private static async Task<Stream> OpenSocketAsync(Uri remoteUri, CancellationToken cancellationToken)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Any, 0));

            var remoteIPAddress = (await Dns.GetHostAddressesAsync(remoteUri.Host)).FirstOrDefault();

            if (remoteIPAddress == null)
            {
                throw new InvalidOperationException();
            }

            await socket.ConnectAsync(remoteIPAddress, remoteUri.Port);

            return new NetworkStream(socket, ownsSocket: true);
        }
    }
}
