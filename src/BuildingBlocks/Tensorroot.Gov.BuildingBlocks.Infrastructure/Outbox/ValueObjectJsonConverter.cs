using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// P0-3 (robustez da fundação): conversor GENÉRICO de <see cref="ValueObject"/> para o
/// <see cref="System.Text.Json"/>. Os Value Objects de CLASSE do domínio têm CONSTRUTOR PRIVADO e
/// propriedades getter-only (ex.: <c>Competencia</c>, <c>Matricula</c>, <c>Cnpj</c>, <c>Hash</c>):
/// o STJ default não consegue desserializá-los — lança ao reidratar um evento do Outbox, o que
/// transformava eventos LEGÍTIMOS (folha fechada, fornecedor cadastrado, documento juntado) em
/// dead-letter silencioso. Este conversor cobre QUALQUER subtipo de <see cref="ValueObject"/> (atuais
/// e futuros), sem que o BuildingBlocks precise referenciar o Domain de cada módulo — opera por
/// reflexão sobre a forma do VO (propriedades públicas de leitura + construtor compatível).
/// <para>
/// Serialização: objeto JSON com as propriedades públicas de instância (getter), na ordem declarada.
/// Desserialização: lê o objeto, casa o melhor construtor (público OU privado) cujos parâmetros
/// correspondem às propriedades (por nome, case-insensitive) e o invoca — reidratando o VO já válido,
/// com as MESMAS invariantes do domínio. É a fonte de verdade compartilhada entre o serialize do
/// interceptor/writer e o deserialize do publisher (mesmas <see cref="JsonSerializerOptions"/>).
/// </para>
/// </summary>
public sealed class ValueObjectJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);
        return typeof(ValueObject).IsAssignableFrom(typeToConvert);
    }

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);
        var conversorTipo = typeof(ValueObjectJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(conversorTipo)!;
    }
}

/// <summary>Conversor STJ de um <see cref="ValueObject"/> concreto <typeparamref name="T"/>.</summary>
/// <typeparam name="T">Tipo concreto de Value Object.</typeparam>
public sealed class ValueObjectJsonConverter<T> : JsonConverter<T>
    where T : ValueObject
{
    // Propriedades públicas de instância com getter, na ordem declarada — superfície (de)serializada.
    private static readonly PropertyInfo[] Propriedades = typeof(T)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
        .ToArray();

    // Construtores candidatos (público OU privado), do mais específico ao menos — match por nome.
    private static readonly ConstructorInfo[] Construtores = typeof(T)
        .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        .OrderByDescending(c => c.GetParameters().Length)
        .ToArray();

    /// <inheritdoc />
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (reader.TokenType == JsonTokenType.Null)
        {
            return null!;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"Esperado objeto JSON para o Value Object '{typeof(T).Name}'.");
        }

        var valores = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        using (var documento = JsonDocument.ParseValue(ref reader))
        {
            foreach (var propriedade in documento.RootElement.EnumerateObject())
            {
                valores[propriedade.Name] = propriedade.Value.Clone();
            }
        }

        var construtor = SelecionarConstrutor(valores)
            ?? throw new JsonException(
                $"Nenhum construtor de '{typeof(T).Name}' é compatível com o JSON desserializado do Outbox.");

        var parametros = construtor.GetParameters();
        var argumentos = new object?[parametros.Length];
        for (var i = 0; i < parametros.Length; i++)
        {
            var parametro = parametros[i];
            argumentos[i] = valores.TryGetValue(parametro.Name!, out var elemento)
                ? elemento.Deserialize(parametro.ParameterType, options)
                : ValorPadrao(parametro);
        }

        return (T)construtor.Invoke(argumentos);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(options);

        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();
        foreach (var propriedade in Propriedades)
        {
            // Escreve SEMPRE pelo nome CLR da propriedade: o Read casa os parâmetros do construtor por
            // esse mesmo nome (case-insensitive). Simetria garantida com as JsonSerializerOptions
            // compartilhadas (sem PropertyNamingPolicy no Outbox).
            writer.WritePropertyName(propriedade.Name);
            JsonSerializer.Serialize(writer, propriedade.GetValue(value), propriedade.PropertyType, options);
        }

        writer.WriteEndObject();
    }

    // Casa o construtor cujos parâmetros (por nome, case-insensitive) estão TODOS presentes no JSON.
    // Prefere o mais específico; ao final aceita um sem-parâmetro como último recurso.
    private static ConstructorInfo? SelecionarConstrutor(Dictionary<string, JsonElement> valores)
    {
        foreach (var construtor in Construtores)
        {
            var parametros = construtor.GetParameters();
            if (parametros.Length == 0)
            {
                continue;
            }

            if (parametros.All(p => valores.ContainsKey(p.Name!)))
            {
                return construtor;
            }
        }

        return Construtores.FirstOrDefault(c => c.GetParameters().Length == 0);
    }

    private static object? ValorPadrao(ParameterInfo parametro)
        => parametro.HasDefaultValue
            ? parametro.DefaultValue
            : parametro.ParameterType.IsValueType
                ? Activator.CreateInstance(parametro.ParameterType)
                : null;
}
