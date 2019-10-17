using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EventSource4Net
{
    class WebRequesterFactory : IWebRequesterFactory
    {
        readonly HttpClient _httpClient;

        public WebRequesterFactory() : this(null) { }

        public WebRequesterFactory(HttpClient httpClient)
        {
            _httpClient = httpClient ?? new HttpClient();
        }
        public IWebRequester Create()
        {
            return new WebRequester(_httpClient);
        }
    }
}
