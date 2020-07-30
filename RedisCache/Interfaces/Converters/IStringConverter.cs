
namespace RedisCluster.Interfaces.Converters
{
	public interface IStringConverter
	{
		string Serialize<T>(T obj);

		T Deserialize<T>(string value);
	}
}
