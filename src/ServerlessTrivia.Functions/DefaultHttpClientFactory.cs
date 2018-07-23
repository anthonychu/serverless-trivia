using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ServerlessTrivia
{
    internal static class DefaultHttpClientFactory
    {
        private static readonly ServiceProvider serviceProvider;

        static DefaultHttpClientFactory()
        {
            var services = new ServiceCollection();
            services.AddHttpClient();
            serviceProvider = services.BuildServiceProvider();
        }

        internal static HttpClient CreateClient()
        {
            return serviceProvider.GetService<IHttpClientFactory>().CreateClient();
        }
    }
}