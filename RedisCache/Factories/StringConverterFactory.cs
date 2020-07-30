using RedisCluster.Converters;
using RedisCluster.Interfaces.Converters;

namespace RedisCluster.Factories
{
    public static class StringConverterFactory
    {
        public static IStringConverter Create()
        {
            return new NewtonsoftStringConverter();
        }
    }
}
