namespace Core.Serializers
{
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public static class JsonConverters
    {
        public static IList<JsonConverter> Default = new List<JsonConverter>
        {
            new StringEnumConverter()
        };
    }
}
