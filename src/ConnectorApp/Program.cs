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

Dictionary<string, string> valueMapping = new Dictionary<string, string>();
valueMapping.Add("$.id","parsed_id");
valueMapping.Add("$.name","parsed_name");
valueMapping.Add("$.number","parsed_number");
valueMapping.Add("$.email.primary","parsed_primary_email");
valueMapping.Add("$.email.secondary","parsed_secondary_email");

JsonObject parsedOutput = parser.Parse(jsonString, valueMapping);


Console.WriteLine("Parsed Output:"+parsedOutput.ToString());

