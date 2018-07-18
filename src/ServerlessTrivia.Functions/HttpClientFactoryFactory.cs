using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ServerlessTrivia
{
    public static class HttpClientFactoryFactory
    {
        private static readonly ServiceProvider serviceProvider;

        static HttpClientFactoryFactory()
        {
            var services = new ServiceCollection();
            services.AddHttpClient();
            serviceProvider = services.BuildServiceProvider();
        }

        public static IHttpClientFactory CreateFactory()
        {
            return serviceProvider.GetService<IHttpClientFactory>();
        }
    }
}