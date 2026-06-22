using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.BuildingBlocks.Application.Messaging;

/// <summary>
/// Marca uma QUERY como leitura de dado pessoal SENSIVEL sob LGPD (PEP Saude, prontuario SUAS,
/// dado de menor). LG-2: o pipeline (<c>TrilhaAcessoSensivelBehavior</c>) gera uma trilha de
/// ACESSO append-only para toda query assim marcada — quem leu, de qual entidade, sob qual base
/// legal, quando, de qual IP — eliminando a leitura sem rastro (art. 37 LGPD, CLAUDE.md §6).
/// <para>
/// A trilha e emitida APOS o handler concluir com sucesso, com os metadados expostos por esta
/// interface (a propria query os calcula a partir de seus parametros — ex.: o id consultado).
/// </para>
/// </summary>
public interface ISensivelLgpd
{
    /// <summary>Nome da entidade/recurso sensivel lido (ex.: "Paciente", "ProntuarioSuas").</summary>
    string EntidadeSensivel { get; }

    /// <summary>
    /// Identificador do recurso lido, quando conhecido pela query (ex.: o id do paciente). Pode ser
    /// nulo quando a leitura e por chave de negocio resolvida no handler (ex.: por CNS).
    /// </summary>
    string? EntidadeId { get; }

    /// <summary>Hipotese legal (LGPD) que autoriza o acesso, gravada estruturada na trilha (LG-A2).</summary>
    BaseLegalLgpd BaseLegal { get; }
}
