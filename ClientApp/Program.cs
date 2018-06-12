﻿using System;
using System.Net;
using System.Threading;
using Message;
using MMONET;
using MMONET.Sockets;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            ///将协议类的程序集注册进查找表中
            MessagePackLUT.Regist(typeof(Login).Assembly);
            ///添加查找表到网络层
            Remote.AddFormatterLookUpTabal(MessagePackLUT.Instance);

            ///建立主线程 或指定的任何线程 轮询。（确保在unity中使用主线程轮询）
            ///MainThreadScheduler保证网络底层的各种回调函数切换到主线程执行以保证执行顺序。
            ThreadPool.QueueUserWorkItem((A) =>
            {
                while (true)
                {
                    MainThreadScheduler.Update(0);
                    Thread.Yield();
                }

            });

            ConnectAsync();
            Console.ReadLine();
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        private static async void ConnectAsync()
        {
            Remote remote = new Remote(RemoteChannal.TCP);
            var ex = await remote.ConnectAsync(IPAddress.IPv6Loopback,54321);

            if (ex == null)
            {
                ///没有异常，连接成功
                

                ///开始接收消息，此处 接收消息回调函数为空，不处理任何消息
                remote.ReceiveAsync(null);

                Console.WriteLine("连接成功");

                ///创建一个登陆消息
                Login2Gate login = new Login2Gate
                {
                    Account = $"TestClient",
                    Password = "123456"
                };

                ///有返回值，这个是一个RPC过程，Exception在网络中传递
                ///只要你调用了接收函数，即使回调函数为空，RPC过程的消息仍能正确处理。
                var (result, excp) = await remote.RpcSendAsync<Login2GateResult>(login);
                if (excp == null)
                {
                    if (result.IsSuccess)
                    {
                        Console.WriteLine("登陆成功");
                    }
                }

                ///没有返回值，不是RPC过程
            }
            else
            {
                ///连接失败
                Console.WriteLine(ex.ToString());
            }
        }



    }
}