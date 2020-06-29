using Common;
using Common.DAL.Transaction;
using Common.RPC;
using Common.RPC.BufferSerializer;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestConsole
{
    internal class A
    {
    }

    internal class B
    {
    }

    struct RegisterRequest : IRPCData
    {
        public string UserName { get; set; }
        public string Password { get; set; }

        public byte MessageID => 0xFF;

        public byte[] ToBuffer()
        {
            byte[] messageIDBuffer = BitConverter.GetBytes((int)MessageID);
            byte[] userNameBuffer = System.Text.Encoding.UTF8.GetBytes(UserName);
            byte[] passwordBuffer = System.Text.Encoding.UTF8.GetBytes(Password);

            return messageIDBuffer.Concat(BitConverter.GetBytes(userNameBuffer.Length + passwordBuffer.Length)).Concat(BitConverter.GetBytes(userNameBuffer.Length)).Concat(userNameBuffer).Concat(BitConverter.GetBytes(passwordBuffer.Length)).Concat(passwordBuffer).ToArray();
        }
    }

    struct RegisterResponse : IRPCData
    {
        public bool Success { get; set; }
        public string ResponseMessage { get; set; }

        public byte MessageID => 0xFE;

        public void SetData(byte[] buffer)
        {
            int offset = 0;

            byte messageID = (byte)BitConverter.ToInt32(buffer, offset);
            offset += 4;

            int length = BitConverter.ToInt32(buffer, offset);
            offset += 4;

            Success = BitConverter.ToBoolean(buffer, offset);
            offset += 1;

            int userNameLength = BitConverter.ToInt32(buffer, offset);
            offset += 4;

            ResponseMessage = System.Text.Encoding.UTF8.GetString(buffer, offset, userNameLength);
            offset += userNameLength;
        }
    }


    internal class Program
    {
        private static byte[] CRC16(byte[] data, int length)
        {
            if (length > 0)
            {
                ushort crc = 0xFFFF;

                for (int i = 0; i < length; i++)
                {
                    crc = (ushort)(crc ^ (data[i]));

                    for (int j = 0; j < 8; j++)
                        crc = (crc & 1) != 0 ? (ushort)((crc >> 1) ^ 0xA001) : (ushort)(crc >> 1);
                }

                byte hi = (byte)((crc & 0xFF00) >> 8); //高位置
                byte lo = (byte)(crc & 0x00FF); //低位置

                return new byte[] { hi, lo };
            }

            return new byte[] { 0, 0 };
        }

        private static bool CheckCRC(byte[] bufferCRC, byte[] valueCRC)
        {
            for (int i = 0; i < 2; i++)
                if (bufferCRC[i] != valueCRC[i])
                    return false;

            return true;
        }

        private unsafe static void Main(string[] args)
        {
            ConfigManager.Init("Development");

<<<<<<< Updated upstream
=======





            //Task.Factory.StartNew(() =>
            //{
            //    byte[] buffer = new byte[1024 * 1024 * 10];
            //    int time = Environment.TickCount;
            //    int count = 0;

            //    //IBufferSerializer bufferSerializer = new BinaryBufferSerializer(Encoding.UTF8);
            //    IBufferSerializer bufferSerializer = new JsonBufferSerializer(Encoding.UTF8);

            //    while (true)
            //    {
            //        RegisterRequest registerRequest = new RegisterRequest();
            //        registerRequest.UserName = $"zxy{Environment.TickCount}";
            //        registerRequest.Password = $"ps{Environment.TickCount}";

            //        int length = bufferSerializer.Serialize(registerRequest, buffer);

            //        byte[] data = new byte[length];

            //        fixed (byte* bufferPtr = buffer)
            //        fixed (byte* dataPtr = data)
            //            Buffer.MemoryCopy(bufferPtr, dataPtr, length, length);

            //        RegisterRequest registerRequest1 = (RegisterRequest)bufferSerializer.Deserialize(data);

            //        if (registerRequest.Password != registerRequest1.Password || registerRequest.UserName != registerRequest1.UserName)
            //        {
            //            throw new Exception();
            //        }

            //        count++;

            //        if (Environment.TickCount - time > 1000)
            //        {
            //            Console.WriteLine($"每秒序列化+反序列化处理次数：{count}, {DateTime.Now:hh:MM:ss}");
            //            time = Environment.TickCount;
            //            count = 0;
            //        }
            //    }
            //});


            //Console.Read();

























            //Task.Factory.StartNew(() =>
            //{
            //    long index = 1;

            //    while (true)
            //    {
            //        RegisterRequest registerRequest = new RegisterRequest();
            //        registerRequest.UserName = $"zxy{Environment.TickCount}";
            //        registerRequest.Password = $"ps{Environment.TickCount}";

            //        byte[] data = registerRequest.ToBuffer();

            //        long dataID = index;
            //        long sessionID = index++;


            //        byte[] dataIDBuffer = BitConverter.GetBytes(dataID);
            //        byte[] sessionIDBuffer = BitConverter.GetBytes(sessionID);

            //        byte[] crc = CRC16(dataIDBuffer.Concat(sessionIDBuffer).Concat(data).ToArray(), data.Length + 16);

            //        byte[] sendBuffer = dataIDBuffer.Concat(sessionIDBuffer).Concat(data).Concat(crc).ToArray();


            //        UdpClient udpClient = new UdpClient(0);
            //        IPEndPoint remotePoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9988);

            //        udpClient.Send(sendBuffer, sendBuffer.Length, remotePoint);
            //        byte[] recv = udpClient.Receive(ref remotePoint);

            //        if (recv.Length == 16)
            //        {
            //            Console.WriteLine($"server recieved request, dataID: {BitConverter.ToInt64(recv, 0)}, sessionID: {BitConverter.ToInt64(recv, 8)}");
            //        }

            //        recv = udpClient.Receive(ref remotePoint);

            //        if (recv.Length > 16)
            //        {
            //            byte[] crcResponse = recv.Skip(recv.Length - 2).Take(2).ToArray();
            //            byte[] crcV = CRC16(recv, recv.Length - 2);

            //            if (CheckCRC(crcResponse, crcV))
            //            {
            //                Console.WriteLine($"client recieved response, dataID: {BitConverter.ToInt64(recv, 0)}, sessionID: {BitConverter.ToInt64(recv, 8)}");
            //            }

            //            udpClient.Send(sendBuffer, sendBuffer.Length, remotePoint);

            //            RegisterResponse registerResponse = new RegisterResponse();
            //            registerResponse.SetData(recv.Skip(16).Take(recv.Length - 8 - 2).ToArray());

            //            Console.WriteLine($"success: {registerResponse.Success}, responseMessage: {registerResponse.ResponseMessage}");
            //        }

            //        System.Threading.Thread.Sleep(100);
            //    }
            //});

            //Console.Read();















            //HashSet<long> ids = new HashSet<long>(1024 * 1024 * 1024);

            //for (int asd = 0; asd < 4; asd++)
            //{
            //    Task.Factory.StartNew(() =>
            //    {
            //        while (true)
            //        {
            //            long id = Common.IDGenerator.NextID();
            //            if (ids.Contains(id))
            //            {
            //                var ld = ids.Last();
            //            }
            //            else
            //            {
            //                ids.Add(id);
            //            }
            //        }
            //    });
            //}

            //Console.Read();
            //return;

>>>>>>> Stashed changes
            for (int cindex = 0; cindex < 4; cindex++)
            {
                int index = cindex;

                Task.Factory.StartNew(() =>
                {
                    int count = 0;
                    int wcount = 0;
                    int time = Environment.TickCount;

                    if (index % 2 == 0)
                    {
                        while (true)
                        {
                            for (int i = 0; i < 5; i++)
                            {
                                try
                                {
                                    if (!TransactionResourceHelper.ApplayResource(typeof(A), index, 5, 10000))
                                    {
                                        wcount++;
                                    }
                                }
                                catch
                                {
                                    Console.WriteLine($"index: {index} Apply Error");
                                }

                                Thread.Sleep(1);

                                try
                                {
                                    if (!TransactionResourceHelper.ApplayResource(typeof(B), index, 5, 10000))
                                    {
                                        wcount++;
                                    }
                                }
                                catch
                                {
                                    Console.WriteLine($"index: {index} Apply Error");
                                }

                                Thread.Sleep(1);

                                try
                                {
                                    TransactionResourceHelper.ReleaseResource(index);
                                }
                                catch
                                {
                                    Console.WriteLine($"index: {index} Release Error");
                                }

                                count++;

                                if (Environment.TickCount - time > 1000)
                                {
                                    Console.WriteLine($"index: {index}, count: {count}");
                                    Console.WriteLine($"index: {index}, wcount: {wcount}");
                                    time = Environment.TickCount;
                                    count = 0;
                                }
                            }

                            Thread.Sleep(1);
                        }
                    }
                    else
                    {
                        while (true)
                        {
                            for (int i = 0; i < 5; i++)
                            {
                                try
                                {
                                    if (!TransactionResourceHelper.ApplayResource(typeof(B), index, 0, 10000))
                                    {
                                        wcount++;
                                    }
                                }
                                catch
                                {
                                    Console.WriteLine($"index: {index} Apply Error");
                                }

                                Thread.Sleep(1);

                                try
                                {
                                    if (!TransactionResourceHelper.ApplayResource(typeof(A), index, 0, 10000))
                                    {
                                        wcount++;
                                    }
                                }
                                catch
                                {
                                    Console.WriteLine($"index: {index} Apply Error");
                                }

                                Thread.Sleep(1);

                                try
                                {
                                    TransactionResourceHelper.ReleaseResource(index);
                                }
                                catch
                                {
                                    Console.WriteLine($"index: {index} Release Error");
                                }

                                count++;

                                if (Environment.TickCount - time > 1000)
                                {
                                    Console.WriteLine($"index: {index}, count: {count}");
                                    Console.WriteLine($"index: {index}, wcount: {wcount}");
                                    time = Environment.TickCount;
                                    count = 0;
                                }
                            }

                            Thread.Sleep(1);
                        }
                    }
                });
            }

            Console.Read();
        }
    }
}