using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zhaobang.FtpServer.File;

namespace UniversalFtpServer
{
    class UwpFileProviderFactory : IFileProviderFactory
    {
        string rootFolder;

        public UwpFileProviderFactory(string rootFolder)
        {
            this.rootFolder = rootFolder;
        }

        public IFileProvider GetProvider(string user)
        {
            return new UwpFileProvider(rootFolder);
        }
    }
}
