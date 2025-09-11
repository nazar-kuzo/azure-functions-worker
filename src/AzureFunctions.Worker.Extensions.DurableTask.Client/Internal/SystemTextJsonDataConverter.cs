using DurableTask.Core.Serializing;

namespace AzureFunctions.Worker.Extensions.DurableTask.Client.Internal;

/// <summary>
/// System Text JSON data converter that uses default worker JSON serializer options.
/// </summary>
/// <param name="jsonSerializerOptions">JSON serializer options</param>
public sealed class SystemTextJsonDataConverter(
    IOptions<JsonSerializerOptions> jsonSerializerOptions) : DataConverter
{
    public override string Serialize(object value)
    {
        return JsonSerializer.Serialize(value, jsonSerializerOptions.Value);
    }

    public override string Serialize(object value, bool formatted)
    {
        var serializerOptions = formatted
            ? new JsonSerializerOptions(jsonSerializerOptions.Value) { WriteIndented = true }
            : jsonSerializerOptions.Value;

        return JsonSerializer.Serialize(value, serializerOptions);
    }

    public override object Deserialize(string data, Type objectType)
    {
        return JsonSerializer.Deserialize(data, objectType, jsonSerializerOptions.Value)!;
    }
}
