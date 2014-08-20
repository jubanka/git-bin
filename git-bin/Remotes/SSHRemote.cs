using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Renci.SshNet;

namespace GitBin.Remotes
{
    public class SSHRemote : IRemote
    {
        private const string SSHHostConfigName = "sshhost";
        private const string SSHPortConfigName = "sshport";
        private const string SSHUsernameConfigName = "sshusername";
        private const string SSHKeyfileConfigName = "sshkeyfile";
        private const string SSHHostpathConfigName = "sshhostpath";

        private readonly string _host;
        private readonly string _hostpath;
        private readonly string _port;
        private readonly string _username;
        private readonly string _keyfile;

        private SftpClient _client;
        private long _flen;
        private object _sync;

        public SSHRemote(
            IConfigurationProvider configurationProvider)
        {
            _host = configurationProvider.GetString(SSHHostConfigName);
            _hostpath = configurationProvider.GetString(SSHHostpathConfigName);
            _port = configurationProvider.GetString(SSHPortConfigName);
            _username = configurationProvider.GetString(SSHUsernameConfigName);
            _keyfile = configurationProvider.GetString(SSHKeyfileConfigName);
            _sync = new object();
        }

        public GitBinFileInfo[] ListFiles()
        {
            var remoteFiles = new List<GitBinFileInfo>();
            lock (_sync) {
                var client = GetClient();

                var files = client.ListDirectory(".");
                var keys = files.Select(o => new GitBinFileInfo(o.Name, o.Length));

                remoteFiles.AddRange(keys);
            }
            return remoteFiles.ToArray();
        }

        public void UploadFile(string fullPath, string key)
        {
            lock(_sync) {
                var client = GetClient();

                using (var file = File.OpenRead(fullPath))
                {
                    _flen = file.Length;
                    client.UploadFile(file, key, ReportProgress);
                }
            }
        }

        public void DownloadFile(string fullPath, string key)
        {
            lock (_sync) {
                var client = GetClient();

                using (var file = File.OpenWrite(fullPath))
                {
                    var attrs = client.GetAttributes(key);
                    _flen = attrs.Size;
                    client.DownloadFile(key, file, ReportProgress);
                }
            }
        }

        private SftpClient GetClient()
        {
            if (_client == null)
            {
                _client = new SftpClient(_host, int.Parse(_port), _username, new PrivateKeyFile(_keyfile));
                _client.Connect();
                _client.ChangeDirectory(_hostpath);
            }
            return _client;
        }

        public event Action<int> ProgressChanged;

        private void ReportProgress(ulong bytes)
        {
            if (this.ProgressChanged != null)
            {
                ProgressChanged((int) (100.0 * bytes / _flen));
            }
        }

    }
}
