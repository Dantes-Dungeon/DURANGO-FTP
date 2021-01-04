// <copyright file="SslLocalDataConnection.cs" company="Zhaoquan Huang">
// Copyright (c) Zhaoquan Huang. All rights reserved
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

#if NETSTANDARD2_1
namespace Zhaobang.FtpServer.Connections
{
    /// <summary>
    /// Establish data connection from local sever.
    /// </summary>
    public class SslLocalDataConnection : IDisposable, IDataConnection, ISslDataConnection
    {
        private const int MinPort = 1024;
        private const int MaxPort = 65535;
        private static int lastUsedPort = new Random().Next(MinPort, MaxPort);

        private readonly IPAddress listeningIP;
        private readonly X509Certificate certificate;
        private TcpClient tcpClient;
        private Stream tcpStream;

        /// <summary>
        /// The port number used in passive mode.
        /// If changed to active mode, set to -1.
        /// </summary>
        private int listeningPort;
        private TcpListener tcpListener;

        /// <summary>
        /// Initializes a new instance of the <see cref="SslLocalDataConnection"/> class.
        /// The class is used to maintain FTP data connection for ONE user.
        /// NO connection will be initiated immediately.
        /// </summary>
        /// <param name="localIP">The IP which was connected by the user.</param>
        /// <param name="certificate">The certificate to upgrade to encrypted stream.</param>
        public SslLocalDataConnection(IPAddress localIP, X509Certificate certificate)
        {
            listeningIP = localIP;
            this.certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
        }

        /// <summary>
        /// Gets a value indicating whether a data connection is open.
        /// </summary>
        public bool IsOpen
        {
            get { return TcpClient != null && TcpClient.Connected; }
        }

        /// <summary>
        /// Gets the supported protocal IDs in active mode (defined in RFC 2824).
        /// </summary>
        public IEnumerable<int> SupportedActiveProtocal
        {
            get => new int[] { 1, 2 };
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

        private TcpClient TcpClient
        {
            get => tcpClient;
            set
            {
                {
                    if (tcpClient != null)
                    {
                        tcpStream.Dispose();
                        tcpClient.Dispose();
                        listeningPort = -1;
                    }
                    if (tcpClient != value)
                    {
                        tcpClient = value;
                        tcpStream = tcpClient?.GetStream();
                    }
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
                catch { }
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
            TcpClient = await tcpListener.AcceptTcpClientAsync();
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
            if (tcpStream is SslStream sslStream)
            {
                await sslStream.ShutdownAsync();
            }
            TcpClient = null;
        }

        /// <summary>
        /// Copies content to data connection.
        /// </summary>
        /// <param name="streamToRead">The stream to copy from.</param>
        /// <returns>The task to await.</returns>
        public async Task SendAsync(Stream streamToRead)
        {
            await streamToRead.CopyToAsync(tcpStream);
            await tcpStream.FlushAsync();
        }

        /// <summary>
        /// Copies content from data connection.
        /// </summary>
        /// <param name="streamToWrite">The stream to copy to.</param>
        /// <returns>The task to await.</returns>
        public async Task RecieveAsync(Stream streamToWrite)
        {
            await tcpStream.CopyToAsync(streamToWrite);
        }

        /// <summary>
        /// Close the connection and listener.
        /// </summary>
        public void Close()
        {
            TcpClient = null;
            tcpListener.Stop();
        }

        /// <summary>
        /// Dispose of the connection and listener.
        /// </summary>
        public void Dispose()
        {
            Close();
        }

        /// <summary>
        /// Upgrade the connection to SSL stream.
        /// </summary>
        /// <returns>The task of the async operation.</returns>
        public async Task UpgradeToSslAsync()
        {
            var sslStream = new SslStream(tcpStream);
            await sslStream.AuthenticateAsServerAsync(certificate);
            tcpStream = sslStream;
        }
    }
}
#endif
