using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace UnitTests
{
    internal class SuppressExecutionContextHandler : DelegatingHandler
    {
        public SuppressExecutionContextHandler(HttpMessageHandler innerHandler) : base(innerHandler)
        {
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // NOTE: We DO NOT want to 'await' the task inside this using. We're just suppressing execution context flow
            // while the task itself is created (which is what would capture the context). After that we just return the
            // (now detached task) to the caller.
            Task<HttpResponseMessage> t;
            using (ExecutionContext.SuppressFlow())
            {
                t = Task.Run(() => base.SendAsync(request, cancellationToken));
            }
            return t;
        }
    }
}
