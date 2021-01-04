// <copyright file="SimpleFileProviderFactory.cs" company="Zhaoquan Huang">
// Copyright (c) Zhaoquan Huang. All rights reserved
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Zhaobang.FtpServer.File
{
    /// <summary>
    /// Manager that provides <see cref="SimpleFileProvider"/> for each user.
    /// The provider shares a root directory accross all users.
    /// </summary>
    public class SimpleFileProviderFactory : IFileProviderFactory
    {
        private string baseDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleFileProviderFactory"/> class.
        /// Every user shares the same root directory.
        /// </summary>
        /// <param name="baseDirectory">The root directory of FTP.</param>
        public SimpleFileProviderFactory(string baseDirectory)
        {
            if (!Directory.Exists(baseDirectory))
            {
                throw new IOException("Base directory doesn't exist");
            }
            this.baseDirectory = baseDirectory;
        }

        /// <summary>
        /// Gets provider for the specified user.
        /// </summary>
        /// <param name="user">The name of the user.</param>
        /// <returns>The <see cref="SimpleFileProvider"/> for that user.</returns>
        public IFileProvider GetProvider(string user)
        {
            return new SimpleFileProvider(baseDirectory);
        }
    }
}
