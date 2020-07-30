using RedisCluster.Exceptions;
using RedisCluster.Factories;
using RedisCluster.Interfaces;
using System;
using System.Threading;

namespace RedisCluster.Models
{
    public class CircuitBreaker
    {
        private readonly ICircuitBreakerStateStore _stateStore = CircuitBreakerStateStoreFactory.Create();
        private readonly object _halfOpenSyncObject = new object();
        public bool IsClosed => _stateStore.IsClosed;
        public bool IsOpen => !IsClosed;
        public TimeSpan DurationOfBreak = TimeSpan.FromSeconds(30);
        public int ExceptionsAllowedBeforeBreaking = 5;
        private int _exceptionCount;

        public int ExceptionCount
        {
            get { return _exceptionCount; }
            private set { _exceptionCount = value; }
        }

        public CircuitBreaker()
        {
            ResetExceptionCount();
        }

        public void ExecuteAction(Action action)
        {
            if (IsOpen)
            {
                // The circuit breaker is Open. Check if the Open timeout has expired.
                // If it has, set the state to HalfOpen.
                if (_stateStore.LastStateChangedDateUtc + DurationOfBreak < DateTime.UtcNow)
                {
                    // The Open timeout has expired. Allow one operation to execute.
                    bool lockTaken = false;
                    try
                    {
                        Monitor.TryEnter(_halfOpenSyncObject, ref lockTaken);
                        if (lockTaken)
                        {
                            // Set the circuit breaker state to HalfOpen.
                            _stateStore.HalfOpen();

                            // Attempt the operation.
                            action();

                            // If this action succeeds, reset the state and allow other operations.
                            _stateStore.Reset();
                            ResetExceptionCount();
                        }
                    }
                    catch (Exception ex)
                    {
                        // If there's still an exception, trip the breaker again immediately.
                        _stateStore.Trip(ex);

                        // Throw the exception so that the caller knows which exception occurred.
                        throw;
                    }
                    finally
                    {
                        if (lockTaken)
                        {
                            Monitor.Exit(_halfOpenSyncObject);
                        }
                    }
                }
                // The Open timeout hasn't yet expired. Throw a CircuitBreakerOpen exception to
                // inform the caller that the call was not actually attempted,
                // and return the most recent exception received.
                throw new CircuitBreakerOpenException("Service call was not attempted due to a broken circuit", _stateStore.LastException);
            }

            // The circuit breaker is Closed, execute the action.
            try
            {
                action();
                if(_exceptionCount > 0)
                {
                    ResetExceptionCount();
                }
            }
            catch (Exception ex)
            {
                // If an exception still occurs here, simply
                // retrip the breaker immediately.
                TrackException(ex);

                // Throw the exception so that the caller can tell
                // the type of exception that was thrown.
                throw;
            }
        }

        private void TrackException(Exception ex)
        {
            int currentExceptionCount = Interlocked.Increment(ref _exceptionCount);
            if (currentExceptionCount >= ExceptionsAllowedBeforeBreaking)
            {
                _stateStore.Trip(ex);
            }
        }

        private void ResetExceptionCount()
        {
            _exceptionCount = 0;
        }
    }
}
