using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Afastamentos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;

/// <summary>
/// Afastamento tipado do servidor registrado (agregado <see cref="Afastamento"/> vigente); dispara o
/// evento eSocial do tipo (S-2230 e congeneres). Substitui o antigo evento generico sem tipo nem efeito.
/// </summary>
/// <param name="ServidorId">Servidor afastado.</param>
/// <param name="Tipo">Tipo legal do afastamento.</param>
/// <param name="Inicio">Inicio do afastamento.</param>
/// <param name="FimPrevisto">Fim previsto (nulo quando indeterminado).</param>
/// <param name="ContaTempo">Se o periodo conta tempo de servico (estabilidade/aposentadoria).</param>
public sealed record AfastamentoRegistrado(
    ServidorId ServidorId,
    TipoAfastamento Tipo,
    DateOnly Inicio,
    DateOnly? FimPrevisto,
    bool ContaTempo) : IDomainEvent;

/// <summary>Afastamento tipado encerrado (retorno do servidor); fecha o periodo para o S-2230/2231.</summary>
/// <param name="ServidorId">Servidor que retornou.</param>
/// <param name="Tipo">Tipo legal do afastamento encerrado.</param>
/// <param name="Inicio">Inicio do afastamento.</param>
/// <param name="FimEfetivo">Fim efetivo (retorno).</param>
public sealed record AfastamentoEncerrado(
    ServidorId ServidorId,
    TipoAfastamento Tipo,
    DateOnly Inicio,
    DateOnly FimEfetivo) : IDomainEvent;
