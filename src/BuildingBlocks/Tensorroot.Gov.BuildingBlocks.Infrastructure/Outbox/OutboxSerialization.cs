using System.Text.Json;

namespace Tensorroot.Gov.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// P0-3 (robustez da fundação): <see cref="JsonSerializerOptions"/> COMPARTILHADO do Outbox, usado
/// de forma IDÊNTICA nos dois lados do ciclo de vida de uma mensagem — a SERIALIZAÇÃO
/// (<c>ConvertDomainEventsToOutboxInterceptor</c> e <c>ModuleIntegrationEventWriter</c>, que gravam o
/// evento) e a DESSERIALIZAÇÃO (<c>OutboxPublisher</c>, que o reidrata ao drenar). Registra o
/// <see cref="ValueObjectJsonConverterFactory"/>, que reidrata os Value Objects de classe com
/// construtor privado (Competencia, Matricula, Cnpj, Hash e quaisquer futuros) — eliminando os
/// dead-letters silenciosos por falha de desserialização.
/// <para>
/// Instância ÚNICA e imutável (STJ recomenda reusar o mesmo options para cache de metadados). Sem
/// <c>PropertyNamingPolicy</c>: o conversor de VO casa propriedades↔parâmetros pelo nome CLR, então
/// serialize e deserialize precisam compartilhar a mesma convenção de nomes (a default).
/// </para>
/// </summary>
public static class OutboxSerialization
{
    /// <summary>Opções compartilhadas de (de)serialização das mensagens de Outbox.</summary>
    public static JsonSerializerOptions Options { get; } = CriarOptions();

    private static JsonSerializerOptions CriarOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.General);
        options.Converters.Add(new ValueObjectJsonConverterFactory());
        return options;
    }
}
