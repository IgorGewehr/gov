using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;

/// <summary>Lancamento (credito/debito/prescricao) registrado no banco de horas.</summary>
/// <param name="BancoDeHorasId">Identificador do banco de horas.</param>
/// <param name="ServidorId">Servidor titular.</param>
/// <param name="Tipo">Natureza do lancamento.</param>
/// <param name="Minutos">Minutos do lancamento.</param>
/// <param name="SaldoMinutos">Saldo corrente apos o lancamento.</param>
public sealed record LancamentoBancoHorasRegistrado(
    BancoDeHorasId BancoDeHorasId,
    Guid ServidorId,
    TipoLancamentoBancoHoras Tipo,
    int Minutos,
    int SaldoMinutos) : IDomainEvent;

/// <summary>Creditos do banco de horas prescritos (perda por nao compensacao na janela).</summary>
/// <param name="BancoDeHorasId">Identificador do banco de horas.</param>
/// <param name="ServidorId">Servidor titular.</param>
/// <param name="MinutosPrescritos">Minutos prescritos.</param>
/// <param name="LimiteData">Data-limite da janela aplicada.</param>
public sealed record CreditosBancoHorasPrescritos(
    BancoDeHorasId BancoDeHorasId,
    Guid ServidorId,
    int MinutosPrescritos,
    DateOnly LimiteData) : IDomainEvent;
