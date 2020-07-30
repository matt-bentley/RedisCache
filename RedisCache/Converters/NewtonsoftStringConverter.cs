using Newtonsoft.Json;
using RedisCluster.Interfaces.Converters;

namespace RedisCluster.Converters
{
    public class NewtonsoftStringConverter : IStringConverter
    {
        public T Deserialize<T>(string value)
        {
            return JsonConvert.DeserializeObject<T>(value);
        }

        public string Serialize<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
    }
}
