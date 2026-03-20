using Parsers.Json;
using Writers.Json;
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

