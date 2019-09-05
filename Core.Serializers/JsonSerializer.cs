using Newtonsoft.Json;

namespace Core.Serializers
{
    /// <summary>
    /// Wrapper for JsonConvert with <see cref="SerializerSettings.Default"/>
    /// </summary>
    public static class JsonSerializer
    {
        public static string AsJson<TObj>(TObj obj)
        {
            return JsonConvert.SerializeObject(obj, SerializerSettings.Default);
        }

        public static TObj FromJson<TObj>(string jsonObject)
        {
            return JsonConvert.DeserializeObject<TObj>(jsonObject, SerializerSettings.Default);
        }
    }
}