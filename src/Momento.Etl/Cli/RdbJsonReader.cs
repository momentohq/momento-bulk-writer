using Momento.Etl.Model;
using Momento.Etl.Validation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Momento.Etl.Cli;


public abstract record JsonParseResult
{
    public record OK : JsonParseResult
    {
        public RedisItem Item { get; private set; }
        public OK(RedisItem item) => Item = item;
    }

    public record Error : JsonParseResult
    {
        public string Message { get; private set; }
        public Error(string message) => this.Message = message;

        private static Error _invalidJson = new Error(ErrorMessage.INVALID_JSON);
        public static Error InvalidJson { get => _invalidJson; }

        private static Error _dataTypeNotSupported = new Error(ErrorMessage.DATA_TYPE_NOT_SUPPORTED);
        public static Error DataTypeNotSupported { get => _dataTypeNotSupported; }
    }
}

public static class RdbJsonReader
{
    public static JsonParseResult ParseJson(string json)
    {
        JObject jsonObject;
        try
        {
            jsonObject = JObject.Parse(json);
        }
        catch (JsonReaderException)
        {
            return JsonParseResult.Error.InvalidJson;
        }

        if (!jsonObject.ContainsKey("type"))
        {
            return JsonParseResult.Error.InvalidJson;
        }
        var dataTypeJToken = jsonObject["type"];
        string dataType;
        try
        {
            dataType = (string)dataTypeJToken!;
        }
        catch (Exception)
        {
            return JsonParseResult.Error.InvalidJson;
        }

        RedisItem? item;
        try
        {
            switch (dataType)
            {
                case "string":
                    item = JsonConvert.DeserializeObject<RedisString>(json);
                    break;
                case "hash":
                    item = JsonConvert.DeserializeObject<RedisHash>(json);
                    break;
                case "list":
                    item = JsonConvert.DeserializeObject<RedisList>(json);
                    break;
                case "set":
                    item = JsonConvert.DeserializeObject<RedisSet>(json);
                    break;
                default:
                    return JsonParseResult.Error.DataTypeNotSupported;
            }
            if (item is null)
            {
                return JsonParseResult.Error.InvalidJson;
            }
        }
        catch (Exception)
        {
            return JsonParseResult.Error.InvalidJson;
        }
        return new JsonParseResult.OK(item);
    }
}
