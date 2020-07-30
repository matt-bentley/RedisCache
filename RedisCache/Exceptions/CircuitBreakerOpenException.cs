using System;

namespace RedisCluster.Exceptions
{
    public class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException(string message, Exception exception) : base(message, exception)
        {
        }
    }
}
