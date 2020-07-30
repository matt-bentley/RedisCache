using Microsoft.Extensions.DependencyInjection;
using RedisCluster.Factories;
using RedisCluster.Interfaces;
using RedisCluster.Interfaces.Converters;
using StackExchange.Redis;
using System;

namespace RedisCluster
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var services = new ServiceCollection();
            services.AddStackExchangeRedisCache(options =>
            {
                options.InstanceName = "RedisCluster";
                var config = new ConfigurationOptions()
                {
                    Password = "!Lt6J6*[b;Ru;Mkx"
                };
                config.EndPoints.Add("localhost:6379");
                config.EndPoints.Add("localhost:6378");
                options.ConfigurationOptions = config;
            });
            services.AddMemoryCache();
            services.AddSingleton<ICachingService, DistributedCachingService>();
            services.AddSingleton<IStringConverter>(StringConverterFactory.Create());

            var serviceProvider = services.BuildServiceProvider();
            var cache = serviceProvider.GetRequiredService<ICachingService>();

            var item = cache.GetOrCreate("test1", () => "Hello World");
        }
    }
}
