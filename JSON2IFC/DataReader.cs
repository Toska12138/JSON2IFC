using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Scan2BimShared.Models;
using Scan2BimShared.Models.IfcEntities;
using Scan2BimShared.StaticData.IfcEnums;
using static Scan2BimConnect.Utilities.SJSONPlugin;

namespace Scan2BimConnect.Utilities
{
    class DataReader
    {
        public DataReader()
        {

        }
        public jsonStructure readJSONStructure(string strReadFilePath)
        {
            StreamReader srReadFile = new StreamReader(strReadFilePath);
            string jsonText = "";
            while (!srReadFile.EndOfStream)
            {
                jsonText += srReadFile.ReadLine();
            }
            jsonStructure jo = JsonSerializer.Deserialize<jsonStructure>(jsonText, new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            });
            return jo;
        }
        public jsonMEP readJSONMEP(string strReadFilePath)
        {
            StreamReader srReadFile = new StreamReader(strReadFilePath);
            string jsonText = "";
            while (!srReadFile.EndOfStream)
            {
                jsonText += srReadFile.ReadLine();
            }
            jsonMEP jo = JsonSerializer.Deserialize<jsonMEP>(jsonText, new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                PropertyNameCaseInsensitive = true,
            });
            return jo;
        }
        public jsonM readJSONM(string strReadFilePath)
        {
            StreamReader srReadFile = new StreamReader(strReadFilePath);
            string jsonText = "";
            while (!srReadFile.EndOfStream)
            {
                jsonText += srReadFile.ReadLine();
            }
            jsonM jo = JsonSerializer.Deserialize<jsonM>(jsonText, new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                PropertyNameCaseInsensitive = true,
            });
            return jo;
        }
        public Dictionary<string, List<PropertySet>> readProperties(string path)
        {
            StreamReader srReadFile = new StreamReader(path);
            string jsonText = "";
            while (!srReadFile.EndOfStream)
            {
                jsonText += srReadFile.ReadLine();
            }
            var serializeOptions = new JsonSerializerOptions();
            serializeOptions.Converters.Add(new StringConverter());
            //Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>> d = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>>>(jsonText, serializeOptions);
            //Object o = d.ToList().ConvertAll(e => new KeyValuePair<string, List<PropertySet>>(e.Key, e.Value.ToList().ConvertAll(e1 => new PropertySet() { name = e1.Key, properties = e1.Value.ToList().ConvertAll(e2 => new Property() { name = e2.Key, label = e2.Value.First().Key, value = e2.Value.First().Value }) }))).ToDictionary(e => e.Key, e => e.Value);
            //new DataWriter().writeJson(o, path + ".json");
            return JsonSerializer.Deserialize<Dictionary<string, List<PropertySet>>>(jsonText, serializeOptions);
        }
        public MetaModel readMetaData(string path)
        {
            StreamReader srReadFile = new StreamReader(path);
            string jsonText = "";
            while (!srReadFile.EndOfStream)
            {
                jsonText += srReadFile.ReadLine();
            }
            var serializeOptions = new JsonSerializerOptions();
            serializeOptions.Converters.Add(new StringConverter());
            return JsonSerializer.Deserialize<MetaModel>(jsonText, serializeOptions);
        }
        class StringConverter : JsonConverter<string>
        {
            public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {

                if (reader.TokenType == JsonTokenType.Number)
                {
                    var stringValue = reader.GetDouble();
                    return stringValue.ToString();
                }
                else if (reader.TokenType == JsonTokenType.False)
                {
                    var stringValue = reader.GetBoolean();
                    return stringValue.ToString();
                }
                else if (reader.TokenType == JsonTokenType.True)
                {
                    var stringValue = reader.GetBoolean();
                    return stringValue.ToString();
                }
                else if (reader.TokenType == JsonTokenType.String)
                {
                    return reader.GetString();
                }
                Console.WriteLine(reader.TokenType.ToString());
                throw new System.Text.Json.JsonException();
            }

            public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value);
            }

        }
        public Dictionary<BuildingComponent, Style> readAppearance(string path)
        {
            StreamReader srReadFile = new StreamReader(path);
            string jsonText = "";
            while (!srReadFile.EndOfStream)
            {
                jsonText += srReadFile.ReadLine();
            }
            var serializeOptions = new JsonSerializerOptions();
            serializeOptions.Converters.Add(new StringConverter());
            return JsonSerializer.Deserialize<Dictionary<BuildingComponent, Style>>(jsonText, serializeOptions);
        }
    }
}
