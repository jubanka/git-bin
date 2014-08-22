using System;
using System.Collections.Generic;
using GitBin.Remotes;
using System.Linq;

namespace GitBin.Remotes
{
    public interface IRemotesFactory
    {
        IRemote GetRemote();
    }

    public class RemotesFactory : IRemotesFactory
    {
        IRemote _remote;

        public RemotesFactory(IConfigurationProvider configurationProvider)
        {
            try
            {
                _remote = new S3Remote(configurationProvider);
            }
            catch
            {
                _remote = null;
            }

            if (_remote != null)
            {
                return;
            }

            try
            {
                _remote = new SSHRemote(configurationProvider);
            }
            catch
            {
                _remote = null;
            }

            if (_remote != null)
            {
                return;
            }

            throw new Exception("Cannot initialize any remotes. Check your configuration");
        }

        public IRemote GetRemote()
        {
            if (_remote == null)
            {
                throw new Exception("No valid IRemote providers found. Check your configuration");
            }
            return _remote;

        }
    }
}

