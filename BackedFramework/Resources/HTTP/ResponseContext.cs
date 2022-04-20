using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackedFramework.Resources.HTTP
{
    public class ResponseContext : ResponseBase
    {
        internal ResponseContext(HTTPParser parser) : base(parser)
        {
        }

        public void Redirect(string location)
        {
            base.StatusCode = 302;
            base.Headers.Add("location", location);
        }
    }
}
