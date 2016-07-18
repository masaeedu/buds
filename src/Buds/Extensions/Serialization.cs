using Newtonsoft.Json;

namespace Buds
{
    public static class SerializationExtensions
    {
        public static string SerializeAsJson(this object message)
        {
            return JsonConvert.SerializeObject(message, new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Objects
            });
        }

        public static T DeserializeFromJson<T>(this string data)
        {
            return JsonConvert.DeserializeObject<T>(data, new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Objects
            });
        }
    }
}
