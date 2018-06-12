﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MMONET.Sockets;
using System.Diagnostics;
using MMONET.Sockets.Test;

namespace RemoteTestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            ConAsync();
            Console.WriteLine("Hello World!");
            Console.ReadLine();
        }
        static int MessageCount = 1;
        static int RemoteCount = 1;
        private static async void ConAsync()
        {
            Remote.AddFormatterLookUpTabal(new TestLut());
            
            //ThreadPool.QueueUserWorkItem((A) =>
            //{
            //    while (true)
            //    {
            //        Remote.Update(0);
            //        //Thread.Sleep(0);
            //    }

            //});

            ///性能测试
            TestSpeed();
            ///连接测试
            //TestConnect();
        }


        #region 性能测试


        /// <summary>
        /// 
        /// </summary>
        private static void TestSpeed()
        {
            for (int i = 0; i < RemoteCount; i++)
            {
                NewRemote(i);
            }
        }

        private static async void NewRemote(int clientIndex)
        {
            Remote remote = new Remote();
            var res = await remote.ConnectAsync(IPAddress.Loopback, 54321);
            if (res == null)
            {
                Console.WriteLine($"Remote{clientIndex}:Success");
            }
            else
            {
                throw res;
            }

            remote.ReceiveAsync((new Receiver() { Index = clientIndex }).TestReceive);
            Stopwatch look1 = new Stopwatch();
            var msg = new TestPacket1 { Value = 0 };
            look1.Start();

            await Task.Run(() =>
            {
                for (int i = 0; i < MessageCount; i++)
                {
                    //Console.WriteLine($"Remote{clientIndex}:发送{nameof(Packet1)}=={i}");
                    msg.Value = i;
                    remote.Send(msg);
                }
            });


            look1.Stop();

            Console.WriteLine($"Remote{clientIndex}: SendAsync{MessageCount} ------ {look1.ElapsedMilliseconds}----- 每秒:{MessageCount * 1000 / (look1.ElapsedMilliseconds+1)}");

            //Remote.BroadCastAsync(new Packet1 { Value = -99999 },remote);

            //var (Result, Excption) = await remote.SendAsync<Packet2>(new Packet1 { Value = 100 });
            //Console.WriteLine($"RPC接收消息{nameof(Packet2)}--{Result.Value}");
        }

        class Receiver
        {
            public int Index { get; set; }
            Stopwatch stopwatch = new Stopwatch();

            public async ValueTask<object> TestReceive(object message)
            {
                switch (message)
                {
                    case TestPacket1 packet1:
                        Console.WriteLine($"Remote{Index}:接收消息{nameof(TestPacket1)}--{packet1.Value}");
                        return new TestPacket2 { Value = packet1.Value };
                    case TestPacket2 packet2:
                        Console.WriteLine($"Remote{Index}:接收消息{nameof(TestPacket2)}--{packet2.Value}");
                        if (packet2.Value == 0)
                        {
                            stopwatch.Restart();
                        }
                        if (packet2.Value == MessageCount - 1)
                        {
                            stopwatch.Stop();

                            Console.WriteLine($"Remote{Index}:TestReceive{MessageCount} ------ {stopwatch.ElapsedMilliseconds}----- 每秒:{MessageCount * 1000 / (stopwatch.ElapsedMilliseconds +1)}");
                        }
                        return null;
                    default:
                        break;
                }
                return null;
            }
        }

        #endregion

        #region 连接测试


        private static async void TestConnect()
        {
            for (int i = 0; i < RemoteCount; i++)
            {
                Connect(i);
            }
        }

        private static async void Connect(int index)
        {
            Remote remote = new Remote();
            var res = await remote.ConnectAsync(IPAddress.Loopback, 54321);
            if (res == null)
            {
                Console.WriteLine($"Remote{index}:Success");
            }
            else
            {
                Console.WriteLine($"Remote:{res}");
            }

            //remote.SendAsync(new Packet1());
        }

        #endregion
    }


    public struct TestStruct
    {

    }
}