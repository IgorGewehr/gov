using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Configurations;

/// <summary>
/// Conversor de valor do owned struct <see cref="Identificacao"/> (dados civis sensiveis — LGPD art. 11)
/// para uma unica coluna JSON. Necessario porque o construtor do Value Object recebe um parametro de
/// guarda (<c>hoje</c>) nao mapeavel, incompativel com a materializacao por construtor dos complex types
/// do EF Core 8. Na leitura, a data de nascimento persistida e reutilizada como referencia da guarda
/// de "nao futuro", preservando a invariante.
/// </summary>
public sealed class IdentificacaoConverter : ValueConverter<Identificacao, string>
{
    private static readonly JsonSerializerOptions Opcoes = new(JsonSerializerDefaults.General);

    /// <summary>Cria o conversor JSON do <see cref="Identificacao"/>.</summary>
    public IdentificacaoConverter()
        : base(
            identificacao => Serializar(identificacao),
            json => Desserializar(json))
    {
    }

    private static string Serializar(Identificacao identificacao)
        => JsonSerializer.Serialize(
            new Estado(
                identificacao.Nome,
                identificacao.NomeSocial,
                identificacao.DataNascimento,
                identificacao.Sexo,
                identificacao.Cpf?.Digitos),
            Opcoes);

    private static Identificacao Desserializar(string json)
    {
        var estado = JsonSerializer.Deserialize<Estado>(json, Opcoes)
            ?? throw new InvalidOperationException("Identificacao persistida invalida.");
        var cpf = string.IsNullOrWhiteSpace(estado.Cpf) ? null : Cpf.Create(estado.Cpf);
        return new Identificacao(
            estado.Nome,
            estado.DataNascimento,
            estado.Sexo,
            estado.NomeSocial,
            cpf,
            estado.DataNascimento);
    }

    private sealed record Estado(
        [property: JsonPropertyName("nome")] string Nome,
        [property: JsonPropertyName("nomeSocial")] string? NomeSocial,
        [property: JsonPropertyName("dataNascimento")] DateOnly DataNascimento,
        [property: JsonPropertyName("sexo")] Sexo Sexo,
        [property: JsonPropertyName("cpf")] string? Cpf);
}
