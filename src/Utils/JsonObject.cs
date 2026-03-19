namespace utils;
using System.Text.Json;

public class JsonObject
{
    public Dictionary<string, object> _data = new Dictionary<string, object>();

    public JsonObject()
    {
        _data = new Dictionary<string, object>();
    }

    public JsonObject(Dictionary<string, object> data)
    {
        _data = data;
    }

    public string GetString(string key){
        return _data[key]?.ToString() ?? "Not parsed";
    }

    public JsonObject GetObject(string key){
        return new JsonObject(
            ((JsonElement) _data[key]).Deserialize<Dictionary<string, object>>()
        );
    }

    public Dictionary<string,object> GetDictionary(){
        return _data;
    }

    public void Add(string key,object value){
        _data.Add(key,value);
    }

    public string ToString(){
        return JsonSerializer.Serialize(_data,new JsonSerializerOptions {WriteIndented = true});
    }

}
