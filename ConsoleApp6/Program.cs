using System;
using System.IO;
using System.Net.Sockets;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

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
            ms.Close();
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

            Util.SerializeInt(stream, type);
            Util.SerializeInt(stream, size);

            if (size > 0)
            {
                stream.Write(data, 0, size);
            }

            stream.Flush();
        }

        public static Frame Deserialize(Stream stream)
        {
            int _type = Util.DeserializeInt(stream);
            int _size = Util.DeserializeInt(stream);

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

    static class Util
    {
        private static Encoding encoding = new UTF8Encoding(false, false);
        public static byte[] SerializeInt(int value)
        {
            byte[] raw = new byte[sizeof(int)];
            for (int j = 0; j < raw.Length; j++)
                raw[j] = (byte)(value >> (8 * j));
            return raw;
        }

        public static int DeserializeInt(byte[] data)
        {
            int value = 0;
            for (int j = 0; j < data.Length; j++)
                value |= data[j] << (8 * j);
            return value;
        }

        public static void SerializeInt(Stream stream, int value)
        {
            for (int j = 0; j < sizeof(int); j++)
            {
                stream.WriteByte((byte)(value >> (8 * j)));
            }
        }

        public static int DeserializeInt(Stream stream)
        {
            int value = 0;
            for (int j = 0; j < sizeof(int); j++)
            {
                value |= stream.ReadByte() << (8 * j);
            }
            return value;
        }

        public static void SerializeString(Stream stream, string value )
        {
            byte[] raw = encoding.GetBytes(value);
            stream.Write(raw, 0, raw.Length);
        }
        
        public static string DeserializeString(Stream stream, int length )
        {
            byte[] raw = new byte[length];
            int r;
            r = stream.Read(raw, 0, raw.Length );
            if (r < length)
                throw new Exception("Wrong string length, or failed to read string");
            return encoding.GetString(raw);

        }
    }
}
