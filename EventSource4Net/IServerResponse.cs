using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EventSource4Net
{
    public interface IServerResponse : IDisposable
    {
        HttpStatusCode StatusCode { get; }

        Task<System.IO.Stream> GetResponseStream();

        Uri ResponseUri { get; }
    }
}
