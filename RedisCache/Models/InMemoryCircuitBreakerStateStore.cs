using RedisCluster.Interfaces;
using System;

namespace RedisCluster.Models
{
    public class InMemoryCircuitBreakerStateStore : ICircuitBreakerStateStore
    {
        public CircuitBreakerState State { get; private set; }
        public Exception LastException { get; private set; }
        public DateTime LastStateChangedDateUtc { get; private set; }
        public bool IsClosed => State == CircuitBreakerState.Closed;

        public InMemoryCircuitBreakerStateStore()
        {
            Reset();
        }

        public void HalfOpen()
        {
            State = CircuitBreakerState.HalfOpen;
            SetLastStateChanged();
        }

        public void Reset()
        {
            State = CircuitBreakerState.Closed;
            SetLastStateChanged();
        }

        public void Trip(Exception ex)
        {
            LastException = ex;
            State = CircuitBreakerState.Open;
            SetLastStateChanged();
        }

        private void SetLastStateChanged()
        {
            LastStateChangedDateUtc = DateTime.UtcNow;
        }
    }
}
