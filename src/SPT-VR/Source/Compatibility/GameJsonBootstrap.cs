using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace TarkovVR.Source.Compatibility
{
    internal static class GameJsonBootstrap
    {
        private static Func<JsonSerializerSettings> installedDefaults;

        // ItemDeserializer uses JToken.ToObject without explicit settings. SPT's
        // raid-time request also uses JsonConvert's defaults and requires string enums.
        // Initialize the game's Unity converter discovery after SPT has loaded, before XR.
        internal static string Initialize()
        {
            JsonConverter[] nativeConverters = EftJsonConverters.Converters
                .Where(converter => converter is UnparsedDataConverter || converter.GetType() == typeof(StringEnumConverter))
                .ToArray();
            if (JsonConvert.DefaultSettings == null)
            {
                var assembly = Assembly.Load("Newtonsoft.Json.UnityConverters");
                var initializer = assembly.GetType("Newtonsoft.Json.UnityConverters.UnityConverterInitializer", true);
                var init = initializer.GetMethod("Init", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (init == null)
                    throw new MissingMethodException(initializer.FullName, "Init");
                init.Invoke(null, null);
            }

            var previous = JsonConvert.DefaultSettings;
            var settings = previous?.Invoke();
            bool repaired = !HasNativeConverters(settings, nativeConverters);
            if (repaired && previous != installedDefaults)
            {
                // Retain other mods' settings, their factory, and Unity converters.
                // A factory may create fresh settings on every call, so merge on each call.
                installedDefaults = () => AddNativeConverters(previous?.Invoke(), nativeConverters);
                JsonConvert.DefaultSettings = installedDefaults;
            }

            // Fail visibly before profile loading if either contract still does not work.
            var data = JObject.Parse("{\"StackObjectsCount\":30}")
                .ToObject<Dictionary<string, UnparsedData>>();
            if (data["StackObjectsCount"].JToken.Value<int>() != 30)
                throw new InvalidOperationException("The native item-count JSON converter is not working.");
            var side = JToken.Parse(JsonConvert.SerializeObject(ESideType.Pmc));
            if (side.Type != JTokenType.String || side.Value<string>() != "Pmc")
                throw new InvalidOperationException("SPT raid requests require Side to serialize as a string.");

            return "JSON defaults " + (repaired ? "repaired" : "verified")
                + ": native item counts and string raid-side values are working.";
        }

        private static bool HasNativeConverters(JsonSerializerSettings settings, JsonConverter[] nativeConverters)
        {
            return settings != null && nativeConverters.All(native =>
                settings.Converters.Any(existing => existing.GetType() == native.GetType()));
        }

        private static JsonSerializerSettings AddNativeConverters(JsonSerializerSettings settings, JsonConverter[] nativeConverters)
        {
            settings = settings ?? new JsonSerializerSettings();
            if (HasNativeConverters(settings, nativeConverters))
                return settings;

            var existing = settings.Converters.ToArray();
            var missing = nativeConverters.Where(native => !existing.Any(converter =>
                converter.GetType() == native.GetType()));
            // Specialized native enum converters must precede the general string-enum fallback.
            settings.Converters = existing.Where(converter => !(converter is StringEnumConverter))
                .Concat(missing)
                .Concat(existing.Where(converter => converter is StringEnumConverter))
                .ToList();
            return settings;
        }
    }
}
