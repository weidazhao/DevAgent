using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DevAgent
{
    public class MessageHub : IDisposable
    {
        private Socket _listeningSocket;
        private Stream _stream;
        private BinaryReader _reader;
        private BinaryWriter _writer;
        private Task _intializationTask;
        private CancellationToken _cancellationToken;

        public event EventHandler<MessageEventArgs> MessageReceived;

        public MessageHub(int localPort, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;

            _listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listeningSocket.Bind(new IPEndPoint(IPAddress.Any, localPort));
            _listeningSocket.Listen(1);

            _intializationTask = Task.Run(async () =>
            {
                var socket = await _listeningSocket.AcceptAsync();
                Initialize(socket);
            });
        }

        public MessageHub(Uri remoteUri, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Any, 0));

            _intializationTask = Task.Run(async () =>
            {
                var remoteIPAddress = (await Dns.GetHostAddressesAsync(remoteUri.Host)).FirstOrDefault();
                await socket.ConnectAsync(remoteIPAddress, remoteUri.Port);

                Initialize(socket);
            });
        }

        public void Dispose()
        {
            _writer.Dispose();
            _reader.Dispose();
            _stream.Dispose();

            if (_listeningSocket != null)
            {
                _listeningSocket.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        public Task WaitUntilConnectedAsync()
        {
            return _intializationTask;
        }

        public void SendMessage(Message message)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            string payload = JsonConvert.SerializeObject(message);

            _writer.Write(payload);
        }

        private void Initialize(Socket socket)
        {
            _stream = new NetworkStream(socket, ownsSocket: true);
            _reader = new BinaryReader(_stream, Encoding.UTF8, leaveOpen: true);
            _writer = new BinaryWriter(_stream, Encoding.UTF8, leaveOpen: true);

            Task.Run(() => OnMessageReceived());

            Console.WriteLine("Message hub initialized.");
        }

        private void OnMessageReceived()
        {
            while (true)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                string payload = _reader.ReadString();

                var message = JsonConvert.DeserializeObject<Message>(payload);

                MessageReceived?.Invoke(this, new MessageEventArgs(message));
            }
        }
    }
}
