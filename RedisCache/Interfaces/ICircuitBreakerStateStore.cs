using RedisCluster.Models;
using System;

namespace RedisCluster.Interfaces
{
    public interface ICircuitBreakerStateStore
    {
        CircuitBreakerState State { get; }

        Exception LastException { get; }

        DateTime LastStateChangedDateUtc { get; }

        void Trip(Exception ex);

        void Reset();

        void HalfOpen();

        bool IsClosed { get; }
    }
}
