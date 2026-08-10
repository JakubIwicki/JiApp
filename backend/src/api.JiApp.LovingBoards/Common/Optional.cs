using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace api.JiApp.LovingBoards.Common;

[JsonConverter(typeof(OptionalJsonConverterFactory))]
public readonly struct Optional<T>
{
    public bool IsSet { get; }
    public T? Value { get; }

    public Optional(T? value)
    {
        IsSet = true;
        Value = value;
    }
}

public sealed class OptionalJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Optional<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(OptionalJsonConverter<>).MakeGenericType(valueType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

public sealed class OptionalJsonConverter<T> : JsonConverter<Optional<T>>
{
    public override Optional<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = JsonSerializer.Deserialize<T>(ref reader, options);
        return new Optional<T>(value);
    }

    public override void Write(Utf8JsonWriter writer, Optional<T> value, JsonSerializerOptions options)
    {
        if (!value.IsSet)
            throw new NotSupportedException(
                $"{nameof(Optional<T>)} is an inbound-only PATCH contract — an unset value cannot be serialized as JSON " +
                $"because the property name is already written by the serializer and a converter cannot skip it. " +
                $"Serializing an unset value as null would make the receiving side clear a field the client never mentioned. " +
                $"Only serialize an explicitly-set value (including an explicit null).");

        JsonSerializer.Serialize(writer, value.Value, options);
    }
}
