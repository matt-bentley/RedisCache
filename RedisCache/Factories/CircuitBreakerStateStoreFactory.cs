using RedisCluster.Interfaces;
using RedisCluster.Models;

namespace RedisCluster.Factories
{
    public static class CircuitBreakerStateStoreFactory
    {
        public static ICircuitBreakerStateStore Create()
        {
            return new InMemoryCircuitBreakerStateStore();
        }
    }
}
