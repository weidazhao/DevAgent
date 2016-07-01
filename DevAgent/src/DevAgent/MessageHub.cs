using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;

namespace DevAgent
{
    public class MessageHub : IDisposable
    {
        private readonly Stream _stream;
        private readonly BinaryReader _reader;
        private readonly BinaryWriter _writer;

        public event EventHandler<Message> MessageReceived;

        public MessageHub(Stream stream)
        {
            _stream = stream;
            _reader = new BinaryReader(_stream, Encoding.UTF8, leaveOpen: true);
            _writer = new BinaryWriter(_stream, Encoding.UTF8, leaveOpen: true);
        }

        public void Dispose()
        {
            _writer.Dispose();
            _reader.Dispose();
            _stream.Dispose();

            GC.SuppressFinalize(this);
        }

        public void SendMessage(Message message)
        {
            string payload = JsonConvert.SerializeObject(message);

            _writer.Write(payload);
        }

        private void OnMessageReceived()
        {
            try
            {
                while (true)
                {
                    string payload = _reader.ReadString();

                    var message = JsonConvert.DeserializeObject<Message>(payload);

                    MessageReceived?.Invoke(this, message);
                }
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }
}
