using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("RedisCache.Tests")]
namespace RedisCluster.Models
{
	// This is needed to support caching null values
	internal class CacheWrapper<T>
	{
		public CacheWrapper(T data)
		{
			Data = data;
		}

        public readonly T Data;
        private string cacheItem;
    }
}
