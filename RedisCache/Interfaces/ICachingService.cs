using System;

namespace RedisCluster.Interfaces
{
	public interface ICachingService
	{
		T GetOrCreate<T>(string key, Func<T> factory);
	}
}
