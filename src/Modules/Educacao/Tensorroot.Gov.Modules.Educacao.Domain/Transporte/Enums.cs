namespace Tensorroot.Gov.Modules.Educacao.Domain.Transporte;

/// <summary>Modalidade de execucao do transporte escolar (PNATE).</summary>
public enum ModalidadeTransporte
{
    /// <summary>Frota propria do ente (veiculo do agregado Veiculo da Frota — Patrimonio).</summary>
    Proprio = 0,

    /// <summary>Servico terceirizado (contratado).</summary>
    Terceirizado = 1,
}

/// <summary>Situacao da rota de transporte (maquina de estados Planejada -&gt; Ativa -&gt; Encerrada).</summary>
public enum SituacaoRotaTransporte
{
    /// <summary>Em planejamento (admite vinculo de alunos; ainda nao em operacao).</summary>
    Planejada = 0,

    /// <summary>Em operacao (atende alunos no turno).</summary>
    Ativa = 1,

    /// <summary>Encerrada (estado terminal).</summary>
    Encerrada = 2,
}
