namespace Parsers.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using utils;

public class JsonParser{
    public JsonObject Parse(string jsonPayload, Dictionary<string, string> valueMapping)
    {
        JsonObject output = new JsonObject();
        var json = JObject.Parse(jsonPayload.ToString());
        
        foreach(var mapping in valueMapping)
        {
            string payloadKey = mapping.Key;
            string outputKey = mapping.Value;
            Object parsedValue = json.SelectToken(payloadKey)?.ToString() ?? "Key not found";
            output.Add(outputKey, parsedValue);
        }
        return output;
    }
}