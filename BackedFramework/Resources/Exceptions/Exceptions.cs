using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackedFramework.Resources.Exceptions
{
    /// <summary>
    /// An exception class relating to a constructable object having more than one instances.
    /// </summary>
    public class MultiInstanceException : Exception
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public MultiInstanceException()
        {
        }
        /// <summary>
        /// Constructor with a message.
        /// </summary>
        /// <param name="message">Message of the exception.</param>
        public MultiInstanceException(string message) : base(message)
        {
        }
    }
}
