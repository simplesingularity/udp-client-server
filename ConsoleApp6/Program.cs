using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp6
{
    internal class Program
    {
        DateTime lastResponse = DateTime.Now;
        static void Main(string[] args)
        {
            YClient client = new YClient("localhost", 44444);
            bool replied = false;

            try
            {
                replied = client.Ping().Result;
            }
            catch (AggregateException agx)
            {
                Console.WriteLine("Failed to connect to server: {0}", agx.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed: {0}", ex.Message);
            }

            Console.WriteLine(replied);
            Console.ReadLine();
        }

        private static void TestEncode()
        {

            Frame test_frame = new Frame();
            test_frame.type = 4;
            test_frame.data = Encoding.UTF8.GetBytes("Pong");
            test_frame.size = test_frame.data.Length;
            MemoryStream ms = new MemoryStream();
            test_frame.Serialize(ms);
            byte[] raw_test = ms.ToArray();
            for (int i = 0; i < raw_test.Length; i++)
            {
                Console.Write("\\x" + raw_test[i].ToString("x2"));
            }
            Console.Read();

        }
    }



    class YClient
    {
        private UdpClient client;
        private string _host;
        private int _port;

        public YClient(string host, int port)
        {
            _host = host;
            _port = port;
            client = new UdpClient();
        }

        public async Task<bool> Ping()
        {
            int sent = await SendString("Ping");
            string returned = await ReceiveString();
            return ("Pong" == returned);
        }

        private async Task<string> ReceiveString()
        {
            Frame frame = await ReceiveData();

            if (frame.type != 4)
                throw new FormatException("Failed to parse incoming string (wrong type)");

            return Encoding.UTF8.GetString(frame.data, 0, frame.size);
        }

        private async Task<Frame> ReceiveData()
        {
            UdpReceiveResult result = await client.ReceiveAsync();
            byte[] rawData = result.Buffer;
            MemoryStream ms = new MemoryStream(rawData);
            Frame frame = Frame.Deserialize(ms);
            return frame;
        }

        private async Task<int> SendString(string value)
        {
            int type = 4; // string
            byte[] data = Encoding.UTF8.GetBytes(value);
            return await SendData(type, data);
        }

        private async Task<int> SendData(int type, byte[] data)
        {
            Frame frame = new Frame();
            frame.type = type;
            frame.data = data;

            MemoryStream ms = new MemoryStream();
            frame.Serialize(ms);
            byte[] raw = ms.ToArray();
            ms.Close();

            int sent = await client.SendAsync(raw, raw.Length, _host, _port);
            return sent;
        }
    }

    class Frame
    {
        public int type;
        public int size;
        public byte[] data;

        public void Serialize(Stream stream)
        {
            if (data != null && data.Length > 0)
            {
                size = data.Length;
            }

            for (int i = 0; i < sizeof(int); i++)
            {
                stream.WriteByte((byte)(type >> (i * 8)));
            }
            for (int i = 0; i < sizeof(int); i++)
            {
                stream.WriteByte((byte)(size >> (i * 8)));
            }

            if (size > 0)
            {
                stream.Write(data, 0, size);
            }

            stream.Flush();
        }

        public static Frame Deserialize(Stream stream)
        {
            int _type = 0;
            for (int i = 0; i < sizeof(int); i++)
            {
                _type |= stream.ReadByte() << (8 * i);
            }
            int _size = 0;
            for (int i = 0; i < sizeof(int); i++)
            {
                _size |= stream.ReadByte() << (8 * i);
            }

            Frame frame = new Frame();
            frame.type = _type;
            frame.size = _size;

            byte[] _data;
            if (_size > 0)
            {
                _data = new byte[_size];
                int r = stream.Read(_data, 0, _size);
                if (r < _size)
                    throw new Exception("Faulty frame");
                frame.data = _data;
            }

            return frame;

        }
    }
}
