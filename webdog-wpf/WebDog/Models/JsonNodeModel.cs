using System.Collections.ObjectModel;
using System.Text.Json;

namespace WebDog.Models
{
    /// <summary>Bindable node for the JSON tree view.</summary>
    public class JsonNodeModel
    {
        public string Key { get; set; } = "";
        public string DisplayValue { get; set; } = "";
        public string Type { get; set; } = ""; // object, array, string, number, boolean, null
        public ObservableCollection<JsonNodeModel> Children { get; set; } = new();
        public bool IsExpanded { get; set; }

        public string Display
        {
            get
            {
                if (Type == "object" || Type == "array")
                {
                    var count = Children.Count;
                    return Type == "array" ? $"[{count}]" : $"{{{count}}}";
                }
                return DisplayValue;
            }
        }

        public static JsonNodeModel BuildFromJson(string json)
        {
            using var doc = JsonDocument.Parse(json);
            return FromElement("", doc.RootElement);
        }

        private static JsonNodeModel FromElement(string key, JsonElement el)
        {
            var node = new JsonNodeModel { Key = key };
            switch (el.ValueKind)
            {
                case JsonValueKind.Object:
                    node.Type = "object";
                    node.IsExpanded = true;
                    foreach (var p in el.EnumerateObject())
                        node.Children.Add(FromElement(p.Name, p.Value));
                    break;
                case JsonValueKind.Array:
                    node.Type = "array";
                    node.IsExpanded = true;
                    int i = 0;
                    foreach (var item in el.EnumerateArray())
                        node.Children.Add(FromElement($"[{i++}]", item));
                    break;
                case JsonValueKind.String:
                    node.Type = "string";
                    node.DisplayValue = $"\"{el.GetString()}\"";
                    break;
                case JsonValueKind.Number:
                    node.Type = "number";
                    node.DisplayValue = el.GetRawText();
                    break;
                case JsonValueKind.True:
                    node.Type = "boolean";
                    node.DisplayValue = "true";
                    break;
                case JsonValueKind.False:
                    node.Type = "boolean";
                    node.DisplayValue = "false";
                    break;
                case JsonValueKind.Null:
                    node.Type = "null";
                    node.DisplayValue = "null";
                    break;
                default:
                    node.Type = "string";
                    node.DisplayValue = el.GetRawText();
                    break;
            }
            return node;
        }
    }
}
