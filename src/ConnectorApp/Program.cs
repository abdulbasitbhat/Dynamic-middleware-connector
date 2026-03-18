using Parsers.Json;

var parser = new JsonParser();
var result = parser.Parse("{\"key\": \"value\"}");
Console.WriteLine(result);
