using DurableTask.Core.Serializing;

namespace DurableTask.Client;

/// <summary>
/// System Text JSON data converter that uses default worker JSON serializer options.
/// </summary>
/// <param name="jsonSerializerOptions">JSON serializer options</param>
public sealed class SystemTextJsonDataConverter(
    IOptions<JsonSerializerOptions> jsonSerializerOptions) : DataConverter
{
    private readonly JsonSerializerOptions indentedSerializerOptions =
        new(jsonSerializerOptions.Value) { WriteIndented = true };

    public override string Serialize(object value)
    {
        return JsonSerializer.Serialize(value, jsonSerializerOptions.Value);
    }

    public override string Serialize(object value, bool formatted)
    {
        return JsonSerializer.Serialize(value, formatted ? this.indentedSerializerOptions : jsonSerializerOptions.Value);
    }

    public override object Deserialize(string data, Type objectType)
    {
        return JsonSerializer.Deserialize(data, objectType, jsonSerializerOptions.Value)!;
    }
}
