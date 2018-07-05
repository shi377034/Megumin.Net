﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MMONET.Remote
{
    public class TCPRemoteListener : IRemoteListener<TCPRemote>
    {
        private TcpListener tcpListener;

        public IPAddress Address { get; set; }
        public int Port { get; set; }

        public TCPRemoteListener(int port)
        {
            this.Port = port;
        }

        public async Task<TCPRemote> ListenAsync()
        {
            if (tcpListener == null)
            {
                ///同时支持IPv4和IPv6
                tcpListener = TcpListener.Create(Port);

                tcpListener.AllowNatTraversal(true);
            }

            tcpListener.Start();
            Socket remoteSocket = null;
            try
            {
                ///此处有远程连接拒绝异常
                remoteSocket = await tcpListener.AcceptSocketAsync();
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine(e);
                ///出现异常重新开始监听
                tcpListener = null;
                ListenAsync();
            }
            TCPRemote remote = new TCPRemote(remoteSocket);
            return remote;
        }

        public void Stop() => tcpListener?.Stop();
    }
}