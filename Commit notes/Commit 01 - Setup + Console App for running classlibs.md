# Commit 01 — Project Setup: Parsers Library + Console App

## What Was Done

Created the initial .NET solution for the Dynamic Middleware Connector. The first piece is a `Parsers` class library with a `JsonParser`, and a `ConnectorApp` console project to run and test it. Also fixed an initial mistake where `Main` was placed inside `JsonParser` — moved it to `Program.cs` in `ConnectorApp` where it belongs.

---

## Structure

```
Dynamic-middleware-connector/
├── ConnectorMiddleware.sln         ← solution file, groups all projects
└── src/
    ├── Parsers/                    ← class library, produces a DLL, not runnable
    │   ├── Parsers.csproj
    │   └── Json/
    │       └── JsonParser.cs
    └── ConnectorApp/               ← console app, entry point, references Parsers
        ├── ConnectorApp.csproj
        └── Program.cs
```

---

## Commands Run

```powershell
# Create the solution file
dotnet new sln -n ConnectorMiddleware

# Create the Parsers class library (produces a DLL, not an executable)
dotnet new classlib -n Parsers -o src/Parsers

# Register Parsers in the solution
dotnet sln add src/Parsers/Parsers.csproj

# Create the console app (the runnable entry point)
dotnet new console -n ConnectorApp -o src/ConnectorApp

# Register ConnectorApp in the solution
dotnet sln add src/ConnectorApp/ConnectorApp.csproj

# Add Parsers as a dependency of ConnectorApp (by project path, not package)
cd src/ConnectorApp
dotnet add reference ../Parsers/Parsers.csproj

# Run the app
dotnet run
```

---

## Current State of the Code

**JsonParser.cs** — only parsing logic, no `Main`:

```csharp
// src/Parsers/Json/JsonParser.cs
namespace Parsers.Json;

public class JsonParser
{
    public string Parse(string input)
    {
        return input.Trim();
    }
}
```

**Program.cs** — the only entry point in the entire solution:

```csharp
// src/ConnectorApp/Program.cs
using Parsers.Json;

var parser = new JsonParser();
var result = parser.Parse("{\"key\": \"value\"}");
Console.WriteLine(result);
```

---

## Testing New Parsers via ConnectorApp

Every time you add a new parser to the `Parsers` library, you test it by calling it from `Program.cs` in `ConnectorApp`. The library never changes — just add a new class, then call it from the console app.

**Adding a new parser — example for XML:**

```csharp
// src/Parsers/Xml/XmlParser.cs
namespace Parsers.Xml;

public class XmlParser
{
    public string Parse(string input)
    {
        // your logic here
        return input.Trim();
    }
}
```

**Call it in Program.cs:**

```csharp
// src/ConnectorApp/Program.cs
using Parsers.Json;
using Parsers.Xml;

var jsonParser = new JsonParser();
Console.WriteLine(jsonParser.Parse("{\"key\": \"value\"}"));

var xmlParser = new XmlParser();
Console.WriteLine(xmlParser.Parse("<root><key>value</key></root>"));
```

**Run it:**

```powershell
# From src/ConnectorApp
dotnet run
```

No other setup needed. Because `ConnectorApp` already has a `ProjectReference` to `Parsers`, any new class you add to the library is immediately available — just add the `using` and call it.

---

## Key Decisions

**Should the class library have a `Main`?** No. A `classlib` must never have a `Main` — that would make it an executable, which defeats its purpose. Only `ConnectorApp` has an entry point. The library's only job is to expose classes that other projects import and call.

**Why two projects?** A library (`classlib`) cannot be run directly — it has no entry point. The console app exists purely to consume and test the library. This mirrors the Java pattern of a separate module for parsers and a separate app module that imports it.

**Why `ProjectReference` and not `PackageReference`?** `ProjectReference` links directly to the source on disk. It's used during active development — no packaging step needed, changes reflect immediately on build. `PackageReference` is for published packages and comes later when the library is stable.

**What is the `.sln` for?** It lets `dotnet build` and Visual Studio find all projects in one place and build them together. It has no configuration of its own — it is just an organizer.

Note: "dotnet add reference" directly edits the ConnectorApp.csproj file and adds the <ProjectReference> line into it. You can open the file and see it there. You never have to touch the .csproj manually for this.