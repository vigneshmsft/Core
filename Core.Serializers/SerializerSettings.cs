namespace Core.Serializers
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    public static class SerializerSettings
    {
        public static JsonSerializerSettings Default = new JsonSerializerSettings
        {
            Converters = JsonConverters.Default,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
    }
}