using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BackedFramework.Resources.Exceptions;

namespace BackedFramework.Server
{
    public class BackedServer
    {
        /// <summary>
        /// The static instance of the server.
        /// </summary>
        public static BackedServer Instance { get; private set; }
        /// <summary>
        /// Configuration instance of the server.
        /// </summary>
        public BackedConfig Config { get; set; }
        /// <summary>
        /// Default constructor for creating the instance of the server.
        /// </summary>
        /// <param name="config">Configuration for the server</param>
        /// <exception cref="MultiInstanceException">Thrown when trying to create more than one instance of a BackedServer.</exception>
        public BackedServer(BackedConfig config)
        {
            // only allow a single instance of the server to exist
            if(!Instance.Equals(null))
                throw new MultiInstanceException("Only one instance of BackedServer can be created.");
            
            // set our config and instance
            this.Config = config;
            Instance = this;
        }
    }
}
