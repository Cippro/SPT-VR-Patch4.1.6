using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using TarkovVR.Source.Compatibility;

class Program
{
    static int assertions;

    static void Main(string[] args)
    {
        if (args.Length != 1) throw new ArgumentException("Usage: JsonRegression <raw-Managed-directory>");
        string directory = Path.GetFullPath(args[0]);
        AssemblyLoadContext.Default.Resolving += (_, name) =>
        {
            string path = Path.Combine(directory, name.Name + ".dll");
            return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
        };
        Run();
    }

    // Resolve the supplied game assemblies only after the resolver is installed.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Run()
    {
        var original = JsonConvert.DefaultSettings;
        try
        {
            JsonConvert.DefaultSettings = null;
            try
            {
                ReadUpd(30);
                throw new Exception("Baseline must reproduce the reported item-count error.");
            }
            catch (JsonSerializationException ex)
            {
                Assert(ex.Message.Contains("UnparsedData") && ex.Message.Contains("StackObjectsCount"), "Reported baseline exception");
            }
            var numericRequest = JObject.Parse(JsonConvert.SerializeObject(new { Side = EFT.ESideType.Pmc, Location = "factory4_day" }));
            Assert(numericRequest["Side"].Type == JTokenType.Integer, "Baseline raid Side is numeric");
            try
            {
                System.Text.Json.JsonSerializer.Deserialize<ServerRaidTimeRequest>(numericRequest.ToString());
                throw new Exception("Numeric Side must fail the SPT server's string model.");
            }
            catch (System.Text.Json.JsonException) { assertions++; }

            // Exercise the real game's Unity converter initializer, not a replacement fixture.
            Console.WriteLine(GameJsonBootstrap.Initialize());
            CheckItemsAndRaid();
            var installed = JsonConvert.DefaultSettings;
            GameJsonBootstrap.Initialize();
            Assert(installed == JsonConvert.DefaultSettings, "Bootstrap is idempotent");

            var custom = new MarkerConverter();
            var shared = new JsonSerializerSettings { DateFormatString = "yyyy-MM", Converters = new List<JsonConverter> { custom } };
            JsonConvert.DefaultSettings = () => shared;
            Console.WriteLine(GameJsonBootstrap.Initialize());
            Assert(JsonConvert.DefaultSettings().DateFormatString == "yyyy-MM", "Existing settings survive");
            Assert(JsonConvert.DefaultSettings().Converters[0] == custom, "Custom converter retains priority");
            Assert(shared.Converters.Count == 3, "Only the two required converters are added");
            CheckItemsAndRaid();

            int calls = 0;
            JsonConvert.DefaultSettings = () =>
            {
                calls++;
                return new JsonSerializerSettings { Converters = new List<JsonConverter> { custom, new StringEnumConverter() } };
            };
            Console.WriteLine(GameJsonBootstrap.Initialize());
            CheckItemsAndRaid();
            Assert(calls > 3, "Fresh-settings factory is preserved and used");
            var converters = JsonConvert.DefaultSettings().Converters;
            Assert(converters.Count(c => c is EFT.UnparsedDataConverter) == 1, "No duplicate UnparsedData converter");
            Assert(converters.Count(c => c is StringEnumConverter) == 1, "No duplicate string-enum converter");
            Assert(converters.ToList().FindIndex(c => c is EFT.UnparsedDataConverter) < converters.ToList().FindIndex(c => c is StringEnumConverter), "Specific converter precedes enum fallback");
        }
        finally { JsonConvert.DefaultSettings = original; }
        Console.WriteLine($"JSON regression: {assertions} assertions passed using supplied SPT 4.1.6 assemblies.");
    }

    static Dictionary<string, UnparsedData> ReadUpd(int count)
    {
        return JObject.Parse("{\"StackObjectsCount\":" + count + ",\"sptPresetId\":\"fixture\",\"Repairable\":{\"Durability\":87.5},\"SpawnedInSession\":true}")
            .ToObject<Dictionary<string, UnparsedData>>();
    }

    static void CheckItemsAndRaid()
    {
        foreach (int count in new[] { 1, 30, 60, 500000, 9999999 })
        {
            var data = ReadUpd(count);
            Assert(data["StackObjectsCount"].JToken.Value<int>() == count, "Money/ammo/trader quantity survives: " + count);
            Assert(data["sptPresetId"].JToken.Value<string>() == "fixture", "SPT preset ID survives");
            Assert(data["Repairable"].JToken["Durability"].Value<double>() == 87.5, "Nested durability survives");
            Assert(data["SpawnedInSession"].JToken.Value<bool>(), "Boolean item state survives");
            Assert(JToken.Parse(JsonConvert.SerializeObject(data["StackObjectsCount"])).Value<int>() == count, "Item count round trip");
        }
        foreach (EFT.ESideType side in new[] { EFT.ESideType.Pmc, EFT.ESideType.Savage })
        {
            string request = JsonConvert.SerializeObject(new { Side = side, Location = "factory4_day" });
            var server = System.Text.Json.JsonSerializer.Deserialize<ServerRaidTimeRequest>(request);
            Assert(server.Side == side.ToString() && server.Location == "factory4_day", "SPT server accepts the raid request");
        }
    }

    static void Assert(bool condition, string description)
    {
        if (!condition) throw new Exception(description);
        assertions++;
    }

    public class ServerRaidTimeRequest
    {
        public string Side { get; set; }
        public string Location { get; set; }
    }

    class MarkerConverter : JsonConverter
    {
        public override bool CanConvert(Type type) => false;
        public override object ReadJson(JsonReader reader, Type type, object value, JsonSerializer serializer) => throw new NotSupportedException();
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new NotSupportedException();
    }
}
