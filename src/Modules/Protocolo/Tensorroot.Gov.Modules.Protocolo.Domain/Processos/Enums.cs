namespace Tensorroot.Gov.Modules.Protocolo.Domain.Processos;

/// <summary>Nível de acesso (visibilidade) de um processo administrativo eletrônico.</summary>
public enum NivelDeAcesso
{
    /// <summary>Acesso público (LAI).</summary>
    Publico = 1,

    /// <summary>Acesso restrito a perfis autorizados.</summary>
    Restrito = 2,

    /// <summary>Sigiloso — nega acesso não autorizado e audita a tentativa.</summary>
    Sigiloso = 3,
}

/// <summary>Situação (estado) do processo no ciclo de vida do PAE.</summary>
public enum SituacaoProcesso
{
    /// <summary>Autuado (estado inicial após a geração do NUP).</summary>
    Autuado = 1,

    /// <summary>Em tramitação entre setores/responsáveis.</summary>
    EmTramitacao = 2,

    /// <summary>Sobrestado (andamento temporariamente suspenso).</summary>
    Sobrestado = 3,

    /// <summary>Arquivado (terminal); guarda conforme TTD/CONARQ.</summary>
    Arquivado = 4,
}
