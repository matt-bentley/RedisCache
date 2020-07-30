using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RedisCluster;
using RedisCluster.Models;
using RedisCluster.Factories;
using RedisCluster.Interfaces;
using System;
using RedisCluster.Interfaces.Converters;
using System.Text;

namespace RedisCache.Tests
{
    [TestClass]
    public class DistrobutedCachingServiceTests
    {
        private ICachingService _cachingService;
        private IStringConverter _converter;
        private Mock<IDistributedCache> _distributedCache;
        private MemoryCache _memoryCache = new MemoryCache(Options.Create<MemoryCacheOptions>(new MemoryCacheOptions()));

        public DistrobutedCachingServiceTests()
        {
            _distributedCache = new Mock<IDistributedCache>();
            _converter = StringConverterFactory.Create();
            _cachingService = new DistributedCachingService(_distributedCache.Object, _memoryCache, _converter);
        }

        [TestMethod]
        public void GivenGetOrCreate_WhenNotExistsInDistributedCache_ThenInvokeFactory()
        {
            string cacheItem = "This is a test";
            string key = "test";
            bool factoryInvoked = false;
            _distributedCache.Setup(s => s.Get(key)).Returns(GetResult(null));
            var response = _cachingService.GetOrCreate(key, () =>
            {
                factoryInvoked = true;
                return cacheItem;
            });

            factoryInvoked.Should().BeTrue();
            response.Should().Be(cacheItem);
        }

        [TestMethod]
        public void GivenGetOrCreate_WhenExistsInDistributedCache_ThenReadNotWrite()
        {
            bool factoryInvoked = false;
            string cacheItem = "This is a test";
            string key = "test";
            _distributedCache.Setup(s => s.Get(key)).Returns(GetResult(GetSerializedCacheWrapper(cacheItem)));
            var response = _cachingService.GetOrCreate(key, () =>
            {
                factoryInvoked = true;
                return cacheItem;
            });

            factoryInvoked.Should().BeFalse();
            response.Should().Be(cacheItem);
            _distributedCache.Verify(c => c.Get(key), Times.Once);
            _distributedCache.Verify(c => c.Set(key, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>()), Times.Never);
        }

        [TestMethod]
        public void GivenGetOrCreate_WhenReadFromDistributedCache_ThenReadFromMemoryCacheNext()
        {
            string cacheItem = "This is a test";
            string key = "test";
            _distributedCache.Setup(s => s.Get(key)).Returns(GetResult(GetSerializedCacheWrapper(cacheItem)));
            var response = _cachingService.GetOrCreate(key, () => cacheItem);
            response = _cachingService.GetOrCreate(key, () => cacheItem);

            response.Should().Be(cacheItem);
            _distributedCache.Verify(c => c.Get(key), Times.Once);
            _distributedCache.Verify(c => c.Set(key, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>()), Times.Never);
        }

        [TestMethod]
        public void GivenGetOrCreate_WhenNotExistsInDistributedCache_ThenCacheReadWriteCalled()
        {
            string cacheItem = "This is a test";
            string key = "test";
            _distributedCache.Setup(s => s.Get(It.IsAny<string>())).Returns(GetResult(null));
            var response = _cachingService.GetOrCreate(key, () => cacheItem);

            _distributedCache.Verify(c => c.Get(key), Times.Once);
            _distributedCache.Verify(c => c.Set(key, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>()), Times.Once);
        }

        [TestMethod]
        public void GivenGetOrCreate_WhenErrorReading_ThenDontWrite()
        {
            string cacheItem = "This is a test";
            string key = "test";
            _distributedCache.Setup(s => s.Get(It.IsAny<string>())).Throws<Exception>();
            var response = _cachingService.GetOrCreate(key, () => cacheItem);

            _distributedCache.Verify(c => c.Get(key), Times.Once);
            _distributedCache.Verify(c => c.Set(key, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>()), Times.Never);
        }

        [TestMethod]
        public void GivenGetOrCreate_WhenExistsInMemoryCache_ThenDontUseDistributed()
        {
            string cacheItem = "This is a test";
            string key = "test";
            _memoryCache.Set(key, GetSerializedCacheWrapper(cacheItem));
            var response = _cachingService.GetOrCreate(key, () => cacheItem);

            response.Should().Be(cacheItem);
            _distributedCache.Verify(c => c.Get(key), Times.Never);
            _distributedCache.Verify(c => c.Set(key, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>()), Times.Never);
        }

        [TestMethod]
        public void GivenGetOrCreate_WhenNullWrapperInMemoryCache_ThenDontUseDistributed()
        {
            string cacheItem = null;
            string key = "test";;
            _memoryCache.Set(key, GetSerializedCacheWrapper(cacheItem));
            var response = _cachingService.GetOrCreate(key, () => cacheItem);

            response.Should().Be(cacheItem);
            _distributedCache.Verify(c => c.Get(key), Times.Never);
            _distributedCache.Verify(c => c.Set(key, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>()), Times.Never);
        }

        [TestMethod]
        public void GivenGetOrCreate_WhenBreakWriteCircuit_ThenDontWrite()
        {
            string cacheItem = "This is a test";
            string key = "test";
            _distributedCache.Setup(s => s.Get(It.IsAny<string>())).Returns(GetResult(null));
            _distributedCache.Setup(s => s.Set(key, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>())).Throws<Exception>();

            int tries = new CircuitBreaker().ExceptionsAllowedBeforeBreaking;

            for(int i = 0; i < tries + 1; i++)
            {
                try
                {
                    var response = _cachingService.GetOrCreate(key, () => cacheItem);
                    _memoryCache.Remove(key);
                }
                catch 
                {

                }
            }       

            _distributedCache.Verify(c => c.Get(key), Times.Exactly(tries + 1));
            _distributedCache.Verify(c => c.Set(key, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>()), Times.Exactly(tries));
        }

        [TestMethod]
        public void GivenGetOrCreate_WhenBreakReadCircuit_ThenDontRead()
        {
            string cacheItem = "This is a test";
            string key = "test";
            _distributedCache.Setup(s => s.Get(It.IsAny<string>())).Throws<Exception>();

            int tries = new CircuitBreaker().ExceptionsAllowedBeforeBreaking;

            for (int i = 0; i < tries + 1; i++)
            {
                try
                {
                    var response = _cachingService.GetOrCreate(key, () => cacheItem);
                    _memoryCache.Remove(key);
                }
                catch
                {

                }
            }

            _distributedCache.Verify(c => c.Get(key), Times.Exactly(tries));
            _distributedCache.Verify(c => c.Set(key, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>()), Times.Never);
        }

        private string GetSerializedCacheWrapper<T>(T cacheItem)
        {
            return _converter.Serialize(new CacheWrapper<T>(cacheItem));
        }

        private byte[] GetResult(string cacheItem)
        {
            if(cacheItem == null)
            {
                return null;
            }
            return Encoding.ASCII.GetBytes(cacheItem);
        }
    }
}
