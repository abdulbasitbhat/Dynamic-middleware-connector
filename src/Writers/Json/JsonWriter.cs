namespace Writers.Json;
using Newtonsoft.Json.Linq;
using utils;
using System.Text.Json;

public class JsonWriter
{
    public JsonObject Write(Dictionary<string, object> data, Dictionary<string, string> valueMapping)
    {
        // empty JObject we will build up by placing values at expressions
        JObject root = new JObject();

        foreach (var mapping in valueMapping)
        {
            string dataKey    = mapping.Key;   // e.g. "parsed_primary_email"
            string expression = mapping.Value; // e.g. "$.email.primary"

            // soft fail — skip silently if key not found in data
            if (data.ContainsKey(dataKey))
            {
                string value = data[dataKey]?.ToString();
                SetValue(root, expression, value);
            }
        }

        // convert JObject back to plain dictionary
        // so JsonObject stays decoupled from Newtonsoft
        string json = root.ToString();
        var result  = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        return new JsonObject(result);
    }

    private void SetValue(JObject root, string expression, string value)
    {
        // strip "$." prefix and split by "."
        // "$.email.primary" → ["email", "primary"]
        string[] tokens = expression.TrimStart('$', '.').Split('.');

        JToken current = root;

        // traverse all parts except the last
        // last part is where value is actually written
        foreach (string token in tokens[..^1]) // [..^1] = all except last
        {
            current = GetOrCreate(current, token);  //get value
        }

        // write value at the final token
        SetFinal(current, tokens[^1], value); //set value at key last element of string
        // Console.WriteLine($"\nSetting value at \nExpr:'{expression}' \nValue: {value} \ncurrent: {current} \ntokens: {tokens[^1]}");
    }

    private JToken GetOrCreate(JToken current, string token)
    {
        if (token.Contains('['))
        {
            // array segment e.g. "addresses[0]"
            var (key, index) = ParseArrayPart(token);

            if (current[key] == null) current[key] = new JArray();

            JArray arr = (JArray)current[key];

            // pad with empty JObjects until index is reachable
            while (arr.Count <= index) arr.Add(new JObject());

            return arr[index]; // return object at index to continue traversal
        }

        // plain segment e.g. "email"
        // create empty JObject if node doesn't exist yet
        if (current[token] == null) current[token] = new JObject();

        return current[token];
    }

    private void SetFinal(JToken current, string token, string value)
    {
        if (token.Contains('['))
        {
            // last part is an array slot e.g. "tags[0]"
            var (key, index) = ParseArrayPart(token);

            if (current[key] == null) current[key] = new JArray();

            JArray arr = (JArray)current[key];

            // pad with nulls — writing a value not an object
            while (arr.Count <= index) arr.Add(null);

            arr[index] = value; // write actual value at this index
        }
        else
        {
            // plain final key e.g. "primary"
            current[token] = value;
        }
    }

    private (string key, int index) ParseArrayPart(string token)
    {
        // "addresses[0]" → key = "addresses", index = 0
        string key = token[..token.IndexOf('[')];
        int index  = int.Parse(token[token.IndexOf('[')..].Trim('[', ']'));
        return (key, index);
    }
}