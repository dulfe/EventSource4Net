using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EventSource4Net
{
    class ServerResponse : IServerResponse, IDisposable
    {
        #region IDisposable
        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (mHttpResponse != null)
                        mHttpResponse.Dispose();
                }
            }
            _disposed = true;
        }

        ~ServerResponse()
        {
            Dispose(false);
        }
        #endregion

        private HttpResponseMessage mHttpResponse;

        public ServerResponse(HttpResponseMessage webResponse)
        {
            this.mHttpResponse = webResponse;
        }

        public HttpStatusCode StatusCode
        {
            get
            {
                return mHttpResponse.StatusCode;
            }
        }

        public Task<System.IO.Stream> GetResponseStream()
        {
            return mHttpResponse.Content.ReadAsStreamAsync();
        }

        public Uri ResponseUri
        {
            get
            {
                return mHttpResponse.RequestMessage.RequestUri;
            }
        }
    }
}
