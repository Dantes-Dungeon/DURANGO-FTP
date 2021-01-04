// <copyright file="LocalDataConnection.cs" company="Zhaoquan Huang">
// Copyright (c) Zhaoquan Huang. All rights reserved
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Zhaobang.FtpServer.Connections
{
    /// <summary>
    /// Establish data connection from local sever.
    /// </summary>
    public class LocalDataConnection : IDisposable, IDataConnection
    {
        private const int MinPort = 1024;
        private const int MaxPort = 65535;
        private static int lastUsedPort = new Random().Next(MinPort, MaxPort);

        private readonly IPAddress listeningIP;

        private TcpClient tcpClient;

        /// <summary>
        /// The port number used in passive mode.
        /// If changed to active mode, set to -1.
        /// </summary>
        private int listeningPort;
        private TcpListener tcpListener;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalDataConnection"/> class.
        /// The class is used to maintain FTP data connection for ONE user.
        /// NO connection will be initiated immediately.
        /// </summary>
        /// <param name="localIP">The IP which was connected by the user.</param>
        public LocalDataConnection(IPAddress localIP)
        {
            listeningIP = localIP;
        }

        /// <summary>
        /// Gets a value indicating whether a data connection is open.
        /// </summary>
        public bool IsOpen
        {
            get { return TcpClient != null && TcpClient.Connected; }
        }

        /// <summary>
        /// Gets the supported protocal IDs in passive mode (defined in RFC 2824).
        /// </summary>
        public IEnumerable<int> SupportedPassiveProtocal
        {
            get
            {
                switch (listeningIP.AddressFamily)
                {
                    case AddressFamily.InterNetwork:
                        return new[] { 1 };
                    case AddressFamily.InterNetworkV6:
                        return new[] { 2 };
                    default:
                        return new int[0];
                }
            }
        }

        /// <summary>
        /// Gets the supported protocal IDs in active mode (defined in RFC 2824).
        /// </summary>
        public IEnumerable<int> SupportedActiveProtocal
        {
            get => new int[] { 1, 2 };
        }

        private TcpClient TcpClient
        {
            get => tcpClient;
            set
            {
                {
                    if (tcpClient != null)
                    {
                        tcpClient.Dispose();
                        listeningPort = -1;
                    }
                    if (tcpClient != value)
                        tcpClient = value;
                }
            }
        }

        /// <summary>
        /// Initiates a data connection in FTP active mode.
        /// </summary>
        /// <param name="remoteIP">The IP to connect to.</param>
        /// <param name="remotePort">The port to connect to.</param>
        /// <param name="protocal">Protocal ID defined in RFC 2428.</param>
        /// <returns>The task to await.</returns>
        public async Task ConnectActiveAsync(IPAddress remoteIP, int remotePort, int protocal)
        {
            AddressFamily addressFamily;
            switch (protocal)
            {
                case 1:
                    addressFamily = AddressFamily.InterNetwork;
                    break;
                case 2:
                    addressFamily = AddressFamily.InterNetworkV6;
                    break;
                default:
                    throw new NotSupportedException();
            }
            listeningPort = -1;
            TcpClient = new TcpClient(addressFamily);
            await TcpClient.ConnectAsync(remoteIP, remotePort);
        }

        /// <summary>
        /// Listens for FTP passive connection and returns the listening end point.
        /// </summary>
        /// <returns>The end point listening at.</returns>
        public IPEndPoint Listen()
        {
            if (tcpListener != null)
            {
                try
                {
                    tcpListener.Start();
                    return new IPEndPoint(listeningIP, listeningPort);
                }
                catch
                {
                    // If can't start the listener, proceed below to create a new one.
                }
            }
            int port = lastUsedPort + 1;
            int startPort = lastUsedPort + 1;
            do
            {
                if (port > MaxPort)
                    port = MinPort;
                try
                {
                    listeningPort = port;
                    var listeningEP = new IPEndPoint(listeningIP, listeningPort);
                    tcpListener = new TcpListener(listeningEP);
                    tcpListener.Start();
                    lastUsedPort = port;
                    return listeningEP;
                }
                catch
                {
                    port++;
                }
            }
            while (port != startPort);
            throw new Exception("There are no ports available");
        }

        /// <summary>
        /// Listens for FTP EPSV connection and returns the listening port.
        /// </summary>
        /// <param name="protocal">The protocal ID to use. Defined in RFC 2824.</param>
        /// <returns>The port listening at.</returns>
        public int ExtendedListen(int protocal)
        {
            if (SupportedPassiveProtocal.Contains(protocal))
                return Listen().Port;
            else
                throw new NotSupportedException();
        }

        /// <summary>
        /// Accepts a FTP passive mode connection.
        /// </summary>
        /// <returns>The task to await.</returns>
        public async Task AcceptAsync()
        {
            tcpClient = await tcpListener.AcceptTcpClientAsync();
            tcpListener.Stop();
            tcpListener = null;
        }

        /// <summary>
        /// Disconnects any open connection.
        /// </summary>
        /// <returns>The task to await.</returns>
#pragma warning disable CS1998
        public async Task DisconnectAsync()
#pragma warning restore CS1998
        {
            TcpClient = null;
        }

        /// <summary>
        /// Copies content to data connection.
        /// </summary>
        /// <param name="streamToRead">The stream to copy from.</param>
        /// <returns>The task to await.</returns>
        public async Task SendAsync(Stream streamToRead)
        {
            var stream = tcpClient.GetStream();
            await streamToRead.CopyToAsync(stream);
            await stream.FlushAsync();
        }

        /// <summary>
        /// Copies content from data connection.
        /// </summary>
        /// <param name="streamToWrite">The stream to copy to.</param>
        /// <returns>The task to await.</returns>
        public async Task RecieveAsync(Stream streamToWrite)
        {
            var stream = tcpClient.GetStream();
            await stream.CopyToAsync(streamToWrite);
        }

        /// <summary>
        /// Close the connection and listener.
        /// </summary>
        public void Close()
        {
            if (TcpClient != null)
            {
                ((IDisposable)TcpClient).Dispose();
            }
            tcpListener?.Stop();
        }

        /// <summary>
        /// Dispose of the connection and listener.
        /// </summary>
        public void Dispose()
        {
            Close();
        }
    }
}
