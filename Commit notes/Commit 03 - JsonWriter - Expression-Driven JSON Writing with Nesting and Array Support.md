# Commit 03 — JsonWriter: Expression-Driven JSON Writing with Nesting and Array Support

## What Was Done

Created a `JsonWriter` class in a new `Writers` library. The writer is the exact reversal of the parser — given a flat data dictionary and a value mapping, it places each value at the correct path in the output JSON, supporting nesting and array indexing via JSONPath expressions. Tested end-to-end in `ConnectorApp` with a full round trip: parse → carry → write.

This took a week to figure out in the Java project. In .NET with the right right AI assisstance, it came together in one session. Most of this was AI assisted — but nothing was accepted without understanding it first, and the decisions and the codebase still feel like mine. This component surprized me on the capabilities of Claude. But also made me realize that I gave it context of what to build, the direction, the architecture acceptable and also my vision of what i plan to add in future helped me write prompt accordingly. The knowledge from project I made in my firm drove my development. I realized the importance of taking time and understanding each line. No matter who writes the code, you must be the owner. Once you blindly let AI build, you lose ownership and within time when new feature need to be added, prompts will be complex and you may have to read codebase to create those. And sometimes you may need a complete redesign. Also collaboration becomes hectic.

---

## Structure Changes

```
Dynamic-middleware-connector/
├── ConnectorMiddleware.sln
└── src/
    ├── Parsers/
    │   ├── Parsers.csproj
    │   └── Json/
    │       └── JsonParser.cs
    ├── Writers/                        ← new: writer library
    │   ├── Writers.csproj
    │   └── Json/
    │       └── JsonWriter.cs           ← new: expression-driven JSON writer
    ├── Utils/
    │   ├── Utils.csproj
    │   └── JsonObject.cs
    └── ConnectorApp/
        ├── ConnectorApp.csproj
        └── Program.cs                  ← updated: full round trip parse + write
```

---

## Commands Run

```powershell
# Create the Writers class library
dotnet new classlib -n Writers -o src/Writers

# Register Writers in the solution
dotnet sln add src/Writers/Writers.csproj

# Add Utils as a dependency of Writers
cd src/Writers
dotnet add reference ../Utils/Utils.csproj

# Add Newtonsoft to Writers
dotnet add package Newtonsoft.Json

# Add Writers as a dependency of ConnectorApp
cd src/ConnectorApp
dotnet add reference ../Writers/Writers.csproj
```

---

## Current State of the Code

**JsonWriter.cs** — only place Newtonsoft is used in the Writers library:

```csharp
namespace Writers;
using Newtonsoft.Json.Linq;
using utils;

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

        // JObject → string → Dictionary
        // round trip forces nested arrays and objects into plain .NET types
        // so JsonObject.ToString() serializes them correctly
        string json = root.ToString();
        var result  = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
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
        foreach (string token in tokens[..^1])
        {
            current = GetOrCreate(current, token);
        }

        SetFinal(current, tokens[^1], value);
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

            return arr[index];
        }

        // plain segment e.g. "email"
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

            arr[index] = value;
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
```

**Program.cs** — full round trip:

```csharp
using Parsers.Json;
using Writers;
using utils;

string jsonString = @"{
    ""id"": ""101"",
    ""name"": ""Abdul Basit"",
    ""number"": ""9876543210"",
    ""email"": {
        ""primary"": ""abdulbasit@gmail.com"",
        ""secondary"": ""basit.work@outlook.com""
    },
    ""tags"": [""backend"", ""dotnet""],
    ""addresses"": [
        { ""city"": ""Srinagar"",  ""postal"": ""193101"" },
        { ""city"": ""Hyderabad"", ""postal"": ""500081"" }
    ]
}";

var parser = new JsonParser();

Dictionary<string, string> parseMapping = new Dictionary<string, string>
{
    { "$.id",                    "parsed_id" },
    { "$.name",                  "parsed_name" },
    { "$.number",                "parsed_number" },
    { "$.email.primary",         "parsed_primary_email" },
    { "$.email.secondary",       "parsed_secondary_email" },
    { "$.tags[0]",               "parsed_tag_0" },
    { "$.tags[1]",               "parsed_tag_1" },
    { "$.addresses[0].city",     "parsed_address_0_city" },
    { "$.addresses[0].postal",   "parsed_address_0_postal" },
    { "$.addresses[1].city",     "parsed_address_1_city" },
    { "$.addresses[1].postal",   "parsed_address_1_postal" }
};

JsonObject parsedOutput = parser.Parse(jsonString, parseMapping);
Console.WriteLine("Parsed Output:");
Console.WriteLine(parsedOutput.ToString());

var writer = new JsonWriter();

Dictionary<string, string> writeMapping = new Dictionary<string, string>
{
    { "parsed_id",               "$.id" },
    { "parsed_name",             "$.name" },
    { "parsed_number",           "$.number" },
    { "parsed_primary_email",    "$.email.primary" },
    { "parsed_secondary_email",  "$.email.secondary" },
    { "parsed_tag_0",            "$.tags[0]" },
    { "parsed_tag_1",            "$.tags[1]" },
    { "parsed_address_0_city",   "$.addresses[0].city" },
    { "parsed_address_0_postal", "$.addresses[0].postal" },
    { "parsed_address_1_city",   "$.addresses[1].city" },
    { "parsed_address_1_postal", "$.addresses[1].postal" }
};

JsonObject writtenOutput = writer.Write(parsedOutput.GetDictionary(), writeMapping);
Console.WriteLine("\nWritten Output:");
Console.WriteLine(writtenOutput.ToString());
```

---

## How the Writer Works — Functional Flow

The writer traverses the expression path and builds the JSON tree as it goes, creating nodes that don't exist yet.

For `"parsed_address_0_city": "$.addresses[0].city"` with value `"Srinagar"`:

```
SetValue  → "$.addresses[0].city"
          → strip + split → ["addresses[0]", "city"]
          → traverse "addresses[0]" via GetOrCreate
              → detects '[', parses key="addresses", index=0
              → creates JArray at root["addresses"]
              → pads to index 0 with empty JObject
              → returns arr[0]
          → SetFinal at "city"
              → plain key, sets current["city"] = "Srinagar"

result: addresses[0] = { "city": "Srinagar" }
```