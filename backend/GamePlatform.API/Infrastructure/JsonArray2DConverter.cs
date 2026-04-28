using System.Text.Json;
using System.Text.Json.Serialization;

namespace GamePlatform.API.Infrastructure;

public class JsonArray2DConverter<T> : JsonConverter<T[,]>
{
    public override T[,]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException();

        var list = new List<List<T>>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException();

            var innerList = new List<T>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                innerList.Add(JsonSerializer.Deserialize<T>(ref reader, options)!);
            }
            list.Add(innerList);
        }

        if (list.Count == 0) return new T[0, 0];

        int rows = list.Count;
        int cols = list[0].Count;
        var array = new T[rows, cols];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                array[i, j] = list[i][j];
            }
        }

        return array;
    }

    public override void Write(Utf8JsonWriter writer, T[,] value, JsonSerializerOptions options)
    {
        int rows = value.GetLength(0);
        int cols = value.GetLength(1);

        writer.WriteStartArray();
        for (int i = 0; i < rows; i++)
        {
            writer.WriteStartArray();
            for (int j = 0; j < cols; j++)
            {
                JsonSerializer.Serialize(writer, value[i, j], options);
            }
            writer.WriteEndArray();
        }
        writer.WriteEndArray();
    }
}

public class JsonArray2DConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsArray && typeToConvert.GetArrayRank() == 2;
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type elementType = typeToConvert.GetElementType()!;
        return (JsonConverter)Activator.CreateInstance(
            typeof(JsonArray2DConverter<>).MakeGenericType(elementType))!;
    }
}
