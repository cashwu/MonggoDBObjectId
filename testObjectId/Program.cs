using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace testObjectId
{
    class Program
    {
        static void Main(string[] args)
        {
            // 5f3b7ab9e1ff015ae3000001
            // 5f3b7ad7e1ff015af1000001
            var objectIds = Enumerable.Range(1, 10).Select(a => ObjectId.NewId()).ToList();

            Console.WriteLine(JsonSerializer.Serialize(objectIds));
            
            Console.WriteLine("------");
            
            Console.WriteLine(JsonSerializer.Serialize(objectIds.Select(a => a.ToString())));
            
            Console.WriteLine("------");
            
            var sourceId = ObjectId.NewId();

            string stringId = sourceId;
            Console.WriteLine(" string id " + stringId);
            
            string userId= ObjectId.NewId();
            Console.WriteLine(" user id " + userId);
            
            Console.WriteLine("------");

            var objectId = new ObjectId(sourceId);
            Console.WriteLine("source id " + JsonSerializer.Serialize(sourceId));
            Console.WriteLine("object id " + JsonSerializer.Serialize(objectId));
            
            Console.WriteLine("------");

            Console.ReadKey();
        }
    }

    public class ObjectIdFactory
    {
        private readonly byte[] pidHex;
        private readonly byte[] machineHash;
        private readonly UTF8Encoding utf8 = new UTF8Encoding(false);
        private readonly DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private int increment;

        public ObjectIdFactory()
        {
            MD5 md5 = MD5.Create();
            machineHash = md5.ComputeHash(utf8.GetBytes(Dns.GetHostName()));
            pidHex = BitConverter.GetBytes(Process.GetCurrentProcess().Id);
            Array.Reverse(pidHex);
        }

        /// <summary>
        ///  产生一个新的 24 位唯一编号
        /// </summary>
        /// <returns></returns>
        public ObjectId NewId()
        {
            int copyIdx = 0;
            byte[] hex = new byte[12];
            byte[] time = BitConverter.GetBytes(GetTimestamp());
            Array.Reverse(time);
            Array.Copy(time, 0, hex, copyIdx, 4);
            copyIdx += 4;

            Array.Copy(machineHash, 0, hex, copyIdx, 3);
            copyIdx += 3;

            Array.Copy(pidHex, 2, hex, copyIdx, 2);
            copyIdx += 2;

            byte[] inc = BitConverter.GetBytes(GetIncrement());
            Array.Reverse(inc);
            Array.Copy(inc, 1, hex, copyIdx, 3);

            return new ObjectId(hex);
        }

        private int GetIncrement() =>
            System.Threading.Interlocked.Increment(ref increment);

        private int GetTimestamp() =>
            Convert.ToInt32(Math.Floor((DateTime.UtcNow - unixEpoch).TotalSeconds));
    }

    public class ObjectId
    {
        private readonly static ObjectIdFactory factory = new ObjectIdFactory();

        public ObjectId(byte[] hexData)
        {
            this.Hex = hexData;
            ReverseHex();
        }

        public ObjectId(string value)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException("value");
            if (value.Length != 24) throw new ArgumentOutOfRangeException("value should be 24 characters");

            Hex = new byte[12];

            for (int i = 0; i < value.Length; i += 2)
            {
                try
                {
                    Hex[i / 2] = Convert.ToByte(value.Substring(i, 2), 16);
                }
                catch
                {
                    Hex[i / 2] = 0;
                }
            }

            ReverseHex();
        }

        public static ObjectId Empty
        {
            get
            {
                return new ObjectId("000000000000000000000000");
            }
        }

        public byte[] Hex { get; private set; }

        public int Timestamp { get; private set; }

        public int Machine { get; private set; }

        public int ProcessId { get; private set; }

        public int Increment { get; private set; }

        public static ObjectId NewId() =>
            factory.NewId();

        public static bool operator <(ObjectId a, ObjectId b) =>
            a.CompareTo(b) < 0;

        public static bool operator <=(ObjectId a, ObjectId b) =>
            a.CompareTo(b) <= 0;

        public static bool operator ==(ObjectId a, ObjectId b) =>
            a.Equals(b);

        public static bool operator !=(ObjectId a, ObjectId b) =>
            !(a == b);

        public static bool operator >=(ObjectId a, ObjectId b) =>
            a.CompareTo(b) >= 0;

        public static bool operator >(ObjectId a, ObjectId b) =>
            a.CompareTo(b) > 0;

        public static implicit operator string(ObjectId objectId) =>
            objectId.ToString();

        public static implicit operator ObjectId(string objectId) =>
            new ObjectId(objectId);

        public override string ToString()
        {
            if (Hex == null)
                Hex = new byte[12];

            StringBuilder hexText = new StringBuilder();

            for (int i = 0; i < this.Hex.Length; i++)
            {
                hexText.Append(this.Hex[i].ToString("x2"));
            }

            return hexText.ToString();
        }

        public override int GetHashCode() =>
            ToString().GetHashCode();

        public int CompareTo(ObjectId other)
        {
            if (other is null)
                return 1;

            for (int i = 0; i < Hex.Length; i++)
            {
                if (Hex[i] < other.Hex[i])
                    return -1;
                else if (Hex[i] > other.Hex[i])
                    return 1;
            }

            return 0;
        }

        public bool Equals(ObjectId other) =>
            CompareTo(other) == 0;

        public override bool Equals(object obj) =>
            base.Equals(obj);

        private void ReverseHex()
        {
            int copyIdx = 0;
            byte[] time = new byte[4];
            Array.Copy(Hex, copyIdx, time, 0, 4);
            Array.Reverse(time);
            this.Timestamp = BitConverter.ToInt32(time, 0);
            copyIdx += 4;
            byte[] mid = new byte[4];
            Array.Copy(Hex, copyIdx, mid, 0, 3);
            this.Machine = BitConverter.ToInt32(mid, 0);
            copyIdx += 3;
            byte[] pids = new byte[4];
            Array.Copy(Hex, copyIdx, pids, 0, 2);
            Array.Reverse(pids);
            this.ProcessId = BitConverter.ToInt32(pids, 0);
            copyIdx += 2;
            byte[] inc = new byte[4];
            Array.Copy(Hex, copyIdx, inc, 0, 3);
            Array.Reverse(inc);
            this.Increment = BitConverter.ToInt32(inc, 0);
        }
    }
}