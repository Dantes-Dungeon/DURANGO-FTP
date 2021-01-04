// <copyright file="FtpTracer.cs" company="Zhaoquan Huang">
// Copyright (c) Zhaoquan Huang. All rights reserved
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Zhaobang.FtpServer.Trace
{
    /// <summary>
    /// The class for tracing FTP commands and replies.
    /// </summary>
    public class FtpTracer
    {
        private readonly ObservableCollection<IPEndPoint> connectedUsers = new ObservableCollection<IPEndPoint>();

        private readonly ReadOnlyObservableCollection<IPEndPoint> connectedUsersView;

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpTracer"/> class.
        /// </summary>
        internal FtpTracer()
        {
            connectedUsersView = new ReadOnlyObservableCollection<IPEndPoint>(connectedUsers);
        }

        /// <summary>
        /// Event handler for tracing FTP commands.
        /// </summary>
        /// <param name="command">The command received by the server.</param>
        /// <param name="remoteAddress">The remote endpoint that sent the command.</param>
        public delegate void FtpCommandInvokedHandler(string command, IPEndPoint remoteAddress);

        /// <summary>
        /// The event handler to handle user changes.
        /// </summary>
        /// <param name="remoteAddress">The remote address of the user.</param>
        public delegate void UserEventHandler(IPEndPoint remoteAddress);

        /// <summary>
        /// Event handler for tracing FTP replies.
        /// </summary>
        /// <param name="replyCode">The reply code sent by the server.</param>
        /// <param name="remoteAddress">The remote endpoint that the reply is sent to.</param>
        public delegate void FtpReplyInvokedHandler(string replyCode, IPEndPoint remoteAddress);

        /// <summary>
        /// The event is fired when a reply is sent from the server
        /// </summary>
        public event FtpReplyInvokedHandler ReplyInvoked;

        /// <summary>
        /// The event is fired when a command is received by the server
        /// </summary>
        public event FtpCommandInvokedHandler CommandInvoked;

        /// <summary>
        /// Fires when a user connects to the FTP server
        /// </summary>
        public event UserEventHandler UserConnected;

        /// <summary>
        /// Fires when a user disconnects from the FTP server
        /// </summary>
        public event UserEventHandler UserDisconnected;

        /// <summary>
        /// Gets the read-only collection of currently connected users. Lock <see cref="ConnectedUsersView"/>
        /// when accessing this.
        /// </summary>
        public ReadOnlyObservableCollection<IPEndPoint> ConnectedUsersView => connectedUsersView;

        /// <summary>
        /// Gets the sync root for <see cref="ConnectedUsersView"/>.
        /// </summary>
        public object ConnectedUsersSyncRoot { get; } = new object();

        /// <summary>
        /// Trace the event that a client sent a command.
        /// </summary>
        /// <param name="command">The command that is sent.</param>
        /// <param name="remoteAddress">The client that sent the command.</param>
        internal void TraceCommand(string command, IPEndPoint remoteAddress)
        {
            Task.Run(() =>
            {
                try
                {
                    CommandInvoked?.Invoke(command, remoteAddress);
                }
                catch { }
            });
        }

        /// <summary>
        /// Trace the event that a reply is sent to a client.
        /// </summary>
        /// <param name="replyCode">The code of the reply.</param>
        /// <param name="remoteAddress">The client that the command is sent to.</param>
        internal void TraceReply(string replyCode, IPEndPoint remoteAddress)
        {
            Task.Run(() =>
            {
                try
                {
                    ReplyInvoked?.Invoke(replyCode, remoteAddress);
                }
                catch { }
            });
        }

        /// <summary>
        /// Trace the event that a client connected.
        /// </summary>
        /// <param name="remoteAddress">The client that connected.</param>
        internal void TraceUserConnection(IPEndPoint remoteAddress)
        {
            Task.Run(() =>
            {
                lock (ConnectedUsersSyncRoot)
                    connectedUsers.Add(remoteAddress);
                try
                {
                    UserConnected?.Invoke(remoteAddress);
                }
                catch { }
            });
        }

        /// <summary>
        /// Trace the event that a client disconnected.
        /// </summary>
        /// <param name="remoteAddress">The client that disconnected.</param>
        internal void TraceUserDisconnection(IPEndPoint remoteAddress)
        {
            Task.Run(() =>
            {
                lock (ConnectedUsersSyncRoot)
                    connectedUsers.Remove(remoteAddress);
                try
                {
                    UserDisconnected?.Invoke(remoteAddress);
                }
                catch { }
            });
        }
    }
}
