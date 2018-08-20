﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MMONET;
using MMONET.Message;
using MMONET.Message.TestMessage;
using MMONET.Remote;
using Network.Remote;

namespace UnitFunc
{
    [TestClass]
    public class UnitTestRemote
    {
        [TestMethod]
        public void TestMethodTcpConnect()
        {
            CancellationTokenSource cancellation = new CancellationTokenSource();
            const int Port = 54321;
            StartTcpListen(Port, cancellation);

            List<IRemote> remotes = new List<IRemote>();
            for (int i = 0; i < 10; i++)
            {
                remotes.Add(new TCPRemote());
            }
            TestConnect(remotes, Port).Wait();
            cancellation.Cancel();
        }

        [TestMethod]
        public void TestMethodUDPConnect()
        {
            CancellationTokenSource cancellation = new CancellationTokenSource();
            const int Port = 44321;
            StartUdpListen(Port, cancellation);

            List<IRemote> remotes = new List<IRemote>();
            for (int i = 0; i < 1; i++)
            {
                remotes.Add(new UDPRemote());
            }
            TestConnect(remotes, Port).Wait();
            cancellation.Cancel();
        }

        [TestMethod]
        public void TestTcpSend()
        {
            const int Port = 54323;
            CancellationTokenSource cancellation = new CancellationTokenSource();
            PrepareEnvironment(cancellation);
            StartTcpListen(Port, cancellation);

            TCPRemote remote = new TCPRemote();
            remote.RpcCallbackPool.RpcTimeOutMilliseconds = 2000;
            remote.ConnectAsync(new IPEndPoint(IPAddress.Loopback, Port)).Wait();
            TestSendAsync(remote).Wait();
            cancellation.Cancel();
        }

        [TestMethod]
        public void TestUdpSend()
        {
            const int Port = 44323;
            CancellationTokenSource cancellation = new CancellationTokenSource();
            PrepareEnvironment(cancellation);
            StartUdpListen(Port, cancellation);

            UDPRemote remote = new UDPRemote();
            remote.RpcCallbackPool.RpcTimeOutMilliseconds = 2000;
            remote.ConnectAsync(new IPEndPoint(IPAddress.IPv6Loopback, Port)).Wait();
            //remote.Receive(null);
            TestSendAsync(remote).Wait();
            cancellation.Cancel();
        }

        [TestMethod]
        public void TestLazyTcpSend()
        {
            const int Port = 54324;
            CancellationTokenSource cancellation = new CancellationTokenSource();
            PrepareEnvironment(cancellation);
            StartTcpListen(Port, cancellation);

            TCPRemote remote = new TCPRemote();
            remote.RpcCallbackPool.RpcTimeOutMilliseconds = 2000;
            remote.ConnectAsync(new IPEndPoint(IPAddress.Loopback, Port)).Wait();
            TestLazySendAsync(remote).Wait();
            cancellation.Cancel();
        }

        [TestMethod]
        public void TestLazyUdpSend()
        {
            const int Port = 44323;
            CancellationTokenSource cancellation = new CancellationTokenSource();
            PrepareEnvironment(cancellation);
            StartUdpListen(Port, cancellation);

            UDPRemote remote = new UDPRemote();
            remote.RpcCallbackPool.RpcTimeOutMilliseconds = 2000;
            remote.ConnectAsync(new IPEndPoint(IPAddress.IPv6Loopback, Port)).Wait();
            //remote.Receive(null);
            TestLazySendAsync(remote).Wait();
            cancellation.Cancel();
        }









        private static void PrepareEnvironment(CancellationTokenSource cancellation)
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    MainThreadScheduler.Update(0);
                    Thread.Yield();
                }
            },cancellation.Token, TaskCreationOptions.LongRunning,TaskScheduler.Default);
            //ThreadPool.QueueUserWorkItem((A) =>
            //{
            //    while (!cancellation.Token.IsCancellationRequested)
            //    {
            //        MainThreadScheduler.Update(0);
            //        Thread.Yield();
            //    }
            //});
            Task.Delay(50).Wait();
        }

        private static void StartTcpListen(int port, CancellationTokenSource cancellation)
        {
            ThreadPool.QueueUserWorkItem(async state =>
            {
                TCPRemoteListener listener = new TCPRemoteListener(port);
                while (!cancellation.Token.IsCancellationRequested)
                {
                    var r = await listener.ListenAsync();
                    r.Receive(Receive);
                }
            });
            Task.Delay(50).Wait();
        }

        private static void StartUdpListen(int port, CancellationTokenSource cancellation)
        {
            ThreadPool.QueueUserWorkItem(async state =>
            {
                UDPRemoteListener listener = new UDPRemoteListener(port);
                while (!cancellation.Token.IsCancellationRequested)
                {
                    var r = await listener.ListenAsync();
                    r.Receive(Receive);
                }
            });
            Task.Delay(200).Wait();
        }

        private static async ValueTask<object> Receive(object message)
        {
            switch (message)
            {
                case TestPacket1 packet1:
                    Console.WriteLine($"接收消息{nameof(TestPacket1)}--{packet1.Value}");
                    return null;
                case TestPacket2 packet2:
                    Console.WriteLine($"接收消息{nameof(TestPacket2)}--{packet2.Value}");
                    return new TestPacket2 { Value = packet2.Value };
                default:
                    break;
            }
            return null;
        }

        async Task TestConnect(IList<IRemote> remotes,int port)
        {
            foreach (var item in remotes)
            {
                var res = await item.ConnectAsync(new IPEndPoint(IPAddress.IPv6Loopback, port));
                Assert.AreEqual(null, res);
                item.Disconnect();
            }
            return;
        }

        private static async Task TestSendAsync(IRemote remote)
        {
            await RpcSendAsync(remote);
            await RpcSendAsyncTimeOut(remote);
            await RpcSendAsyncTypeError(remote);
            //await Task.Delay(-1);
        }

        private static async Task TestLazySendAsync(ISuperRemote remote)
        {
            await SafeRpcSendAsync(remote);
            await SafeRpcSendAsyncTimeOut(remote);
            await SafeRpcSendAsyncTypeError(remote);
            //await Task.Delay(-1);
        }

        private static async Task SafeRpcSendAsync(ISuperRemote remote)
        {
            TestPacket2 packet2 = new TestPacket2() { Value = new Random().Next() };
            var res = await remote.SendAsyncSafeAwait<TestPacket2>(packet2);
            Assert.AreEqual(packet2.Value, res.Value);
        }

        private static async Task SafeRpcSendAsyncTypeError(ISuperRemote remote)
        {
            TestPacket2 packet2 = new TestPacket2() { Value = new Random().Next() };
            TaskCompletionSource<Exception> source = new TaskCompletionSource<Exception>();
            remote.SendAsyncSafeAwait<TestPacket1>(packet2, ex =>
             {
                 source.SetResult(ex);
             });

            var (result, complete) = await source.Task.WaitAsync(3000);
            Assert.AreEqual(true, complete);
            Assert.AreEqual(typeof(InvalidCastException), result.GetType());
        }

        private static async Task SafeRpcSendAsyncTimeOut(ISuperRemote remote)
        {
            TestPacket1 packet2 = new TestPacket1() { Value = new Random().Next() };
            TaskCompletionSource<Exception> source = new TaskCompletionSource<Exception>();
            remote.SendAsyncSafeAwait<TestPacket2>(packet2,ex=>
            {
                source.SetResult(ex);
            });

            var (result, complete) = await source.Task.WaitAsync(3000);
            Assert.AreEqual(true, complete);
            Assert.AreEqual(typeof(TimeoutException), result.GetType());
        }

        private static async Task RpcSendAsync(IRemote remote)
        {
            TestPacket2 packet2 = new TestPacket2() { Value = new Random().Next() };
            var (result, exception) = await remote.SendAsync<TestPacket2>(packet2);
            Assert.AreEqual(null, exception);
            Assert.AreEqual(packet2.Value, result.Value);
        }

        private static async Task RpcSendAsyncTimeOut(IRemote remote)
        {
            TestPacket1 packet2 = new TestPacket1() { Value = new Random().Next() };
            var (result, exception) = await remote.SendAsync<TestPacket2>(packet2);
            Assert.AreEqual(typeof(TimeoutException), exception.GetType());
            Assert.AreEqual(null, result);
        }

        private static async Task RpcSendAsyncTypeError(IRemote remote)
        {
            TestPacket2 packet2 = new TestPacket2() { Value = new Random().Next() };
            var (result, exception) = await remote.SendAsync<TestPacket1>(packet2);
            Assert.AreEqual(typeof(InvalidCastException), exception.GetType());
            Assert.AreEqual(null, result);
        }

        #region 反编译分析使用

        public async Task TestAsync()
        {
            TCPRemote remote = new TCPRemote();
            var res = await remote.SendAsyncSafeAwait<TestPacket1>(null);
            res.ToString();
            await Task.Delay(10);
            res.ToString();
        }

        public async void TestAsync2()
        {
            TCPRemote remote = new TCPRemote();
            var res = await remote.SendAsyncSafeAwait<TestPacket1>(null);
            res.ToString();
            await Task.Delay(10);
            res.ToString();
        }

        public async void TestAsync3()
        {
            TCPRemote remote = new TCPRemote();
            var res = await remote.SendAsync<TestPacket1>(null);
            res.ToString();
            await Task.Delay(10);
            res.ToString();
        }

        public async Task TestAsync4()
        {
            TCPRemote remote = new TCPRemote();
            var res = await remote.SendAsync<TestPacket1>(null);
            res.ToString();
            await Task.Delay(10);
            res.ToString();
        }

        #endregion
    }
}
