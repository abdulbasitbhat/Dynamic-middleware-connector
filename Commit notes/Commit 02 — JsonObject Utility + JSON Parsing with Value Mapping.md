# Commit 02 — JsonObject Utility + JSON Parsing with Value Mapping

## What Was Done

Created a `JsonObject` utility class in a `utils` namespace to act as a decoupled data carrier. Updated `JsonParser` to accept a value mapping dictionary and return a populated `JsonObject`. Tested end-to-end in `ConnectorApp`.

---

## Structure Changes

```
Dynamic-middleware-connector/
├── ConnectorMiddleware.sln
└── src/
    ├── Parsers/
    │   ├── Parsers.csproj
    │   └── Json/
    │       └── JsonParser.cs       ← updated: now accepts valueMapping, returns JsonObject
    ├── Utils/                      ← new: utility library
    │   ├── Utils.csproj
    │   └── JsonObject.cs           ← new: decoupled JSON data carrier
    └── ConnectorApp/
        ├── ConnectorApp.csproj
        └── Program.cs              ← updated: wires valueMapping and prints output
```

## Commands Run
 
```powershell
# Create the Utils class library
dotnet new classlib -n Utils -o src/Utils
 
# Register Utils in the solution
dotnet sln add src/Utils/Utils.csproj
 
# Add Utils as a dependency of ConnectorApp
cd src/ConnectorApp
dotnet add reference ../Utils/Utils.csproj
 
# Add Utils as a dependency of Parsers
cd src/Parsers
dotnet add reference ../Utils/Utils.csproj
```
 
## Using in Code
 
```csharp
// In JsonParser.cs and Program.cs
using utils;
```

---

## Current State of the Code

**JsonObject.cs** — decoupled data carrier, no external dependencies:

```csharp
namespace utils;
using System.Text.Json;

public class JsonObject
{
    public Dictionary<string, object> _data = new Dictionary<string, object>();

    public JsonObject() { _data = new Dictionary<string, object>(); }

    public JsonObject(Dictionary<string, object> data) { _data = data; }

    public string GetString(string key) => _data[key]?.ToString() ?? "Not parsed";

    public JsonObject GetObject(string key) =>
        new JsonObject(
            ((JsonElement)_data[key]).Deserialize<Dictionary<string, object>>()
        );

    public Dictionary<string, object> GetDictionary() => _data;

    public void Add(string key, object value) => _data.Add(key, value);

    public string ToString() =>
        JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
}
```

**JsonParser.cs** — only place Newtonsoft is used:

```csharp
namespace Parsers.Json;
using Newtonsoft.Json.Linq;
using utils;

public class JsonParser
{
    public JsonObject Parse(string jsonPayload, Dictionary<string, string> valueMapping)
    {
        JsonObject output = new JsonObject();
        var json = JObject.Parse(jsonPayload);

        foreach (var mapping in valueMapping)
        {
            string payloadKey = mapping.Key;
            string outputKey  = mapping.Value;
            object parsedValue = json.SelectToken(payloadKey)?.ToString() ?? "Key not found";
            output.Add(outputKey, parsedValue);
        }
        return output;
    }
}
```

**Program.cs** — wires everything together:

```csharp
using Parsers.Json;
using utils;

var parser = new JsonParser();

string jsonString = @"{
  ""id"": ""101"",
  ""name"": ""Abdul Basit"",
  ""number"": ""9876543210"",
  ""email"": {
    ""primary"": ""abdulbasit@gmail.com"",
    ""secondary"": ""basit.work@outlook.com""
  }
}";

Dictionary<string, string> valueMapping = new Dictionary<string, string>
{
    { "$.id",              "parsed_id" },
    { "$.name",            "parsed_name" },
    { "$.number",          "parsed_number" },
    { "$.email.primary",   "parsed_primary_email" },
    { "$.email.secondary", "parsed_primary_email" }
};

JsonObject parsedOutput = parser.Parse(jsonString, valueMapping);
Console.WriteLine("Parsed Output:" + parsedOutput.ToString());
```

```csharp
cd ConnectorApp
dotnet run
```
---

## Concepts Used

**`JObject` and `SelectToken`**
`JObject.Parse()` parses a raw JSON string into a queryable object (Newtonsoft).
`SelectToken("$.email.primary")` uses JSONPath syntax to extract values — dot notation maps to nested object depth. Returns `null` if path not found, so `?.ToString()` is used for null safety.

**`JsonElement`**
When deserializing JSON into `Dictionary<string, object>`, .NET (System.Text.Json) does not know the concrete type of nested objects at runtime, so it stores them as `JsonElement` — a raw JSON chunk not yet converted to a C# type. To use a nested object, you cast it: `((JsonElement)_data[key])` and then call `.Deserialize<T>()` on it.

**`JsonSerializer.Serialize` with `WriteIndented`**
`JsonSerializer.Serialize(_data)` converts the internal dictionary back to a JSON string.
`WriteIndented = true` formats it with line breaks and indentation for readable output.

---

## Key Decisions

**Why a custom `JsonObject` instead of using `JObject` everywhere?**
`JObject` is a Newtonsoft type. Passing it around the codebase would couple every class to Newtonsoft. If the parsing library is swapped in the future, changes would spread everywhere. `JsonObject` is a plain internal type — it has no library dependency, so the rest of the codebase never needs to change regardless of what parser is used underneath.

**Why does `JsonParser` still use Newtonsoft?**
Newtonsoft's `SelectToken` with JSONPath is the right tool for dynamic path-based extraction — paths are stored as strings in the mapping and not known at compile time. `System.Text.Json` has no equivalent for this. The containment is intentional: Newtonsoft is used in one class, in one method, for one purpose.

**Why `Dictionary<string, string>` for value mapping?**
Key is the JSONPath expression (source), value is the output field name (target). Simple, readable, and easy to populate from a config or database later. No custom class needed at this stage.