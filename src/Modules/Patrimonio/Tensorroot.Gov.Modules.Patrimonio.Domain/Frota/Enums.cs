namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;

/// <summary>Situação (estado) de uma ordem de serviço de manutenção do veículo.</summary>
public enum SituacaoOrdemServico
{
    /// <summary>Ordem de serviço aberta.</summary>
    Aberta = 1,

    /// <summary>Manutenção concluída.</summary>
    Concluida = 2,

    /// <summary>Ordem cancelada.</summary>
    Cancelada = 3,
}

/// <summary>Situação (estado) de uma multa de trânsito atribuída ao veículo (CTB).</summary>
public enum SituacaoMulta
{
    /// <summary>Multa pendente de pagamento.</summary>
    Pendente = 1,

    /// <summary>Multa em recurso (defesa/JARI).</summary>
    EmRecurso = 2,

    /// <summary>Multa paga.</summary>
    Paga = 3,
}

/// <summary>Situação (estado) do licenciamento anual/IPVA de um exercício (CTB).</summary>
public enum SituacaoLicenciamento
{
    /// <summary>Licenciamento pendente no exercício.</summary>
    Pendente = 1,

    /// <summary>Licenciamento regular no exercício.</summary>
    Regular = 2,
}

/// <summary>
/// Situação (estado) de um pneu no seu ciclo de vida na frota: do estoque, passando pela
/// instalação/rodízio em veículos, até a recapagem ou o descarte (sucateamento).
/// </summary>
public enum SituacaoPneu
{
    /// <summary>Pneu novo/recapado disponível em estoque, ainda não instalado.</summary>
    EmEstoque = 1,

    /// <summary>Pneu instalado em uma posição de um veículo (em rodagem).</summary>
    Instalado = 2,

    /// <summary>Pneu removido do veículo, aguardando destinação (rodízio, recapagem ou descarte).</summary>
    Removido = 3,

    /// <summary>Pneu enviado para recapagem (reforma da banda de rodagem).</summary>
    EmRecapagem = 4,

    /// <summary>Pneu descartado/sucateado (fim de vida útil); estado terminal.</summary>
    Descartado = 5,
}

/// <summary>
/// Eixo de um veículo, no sentido dianteiro→traseiro, base do layout de posicionamento dos pneus
/// (CTB e prática de gestão de frota). O número de eixos varia conforme a configuração do veículo.
/// </summary>
public enum Eixo
{
    /// <summary>Primeiro eixo (dianteiro/direcional).</summary>
    Dianteiro = 1,

    /// <summary>Segundo eixo (traseiro ou de tração, conforme o veículo).</summary>
    Traseiro1 = 2,

    /// <summary>Terceiro eixo (traseiro adicional / truck).</summary>
    Traseiro2 = 3,

    /// <summary>Quarto eixo (reboques/semirreboques e configurações pesadas).</summary>
    Traseiro3 = 4,
}

/// <summary>Lado do veículo onde o pneu é montado, no layout de eixos (gestão de frota).</summary>
public enum LadoMontagem
{
    /// <summary>Lado esquerdo (motorista).</summary>
    Esquerdo = 1,

    /// <summary>Lado direito (passageiro).</summary>
    Direito = 2,

    /// <summary>Posição interna em eixo de rodado duplo (caminhões), lado esquerdo.</summary>
    EsquerdoInterno = 3,

    /// <summary>Posição interna em eixo de rodado duplo (caminhões), lado direito.</summary>
    DireitoInterno = 4,

    /// <summary>Estepe (pneu reserva), sem lado de rodagem.</summary>
    Estepe = 5,
}

/// <summary>
/// Categoria de uma apólice/cobertura de seguro de um veículo da frota: o seguro obrigatório
/// (DPVAT/SPVAT, conforme legislação vigente) e o seguro facultativo de frota (casco/RCF-V etc.).
/// </summary>
public enum CategoriaSeguro
{
    /// <summary>
    /// Seguro obrigatório de danos pessoais por acidentes de trânsito (DPVAT/SPVAT — Lei 6.194/1974,
    /// LC 207/2024), conforme a cobrança vigente. Vinculado ao licenciamento anual.
    /// </summary>
    Obrigatorio = 1,

    /// <summary>Seguro facultativo de frota (casco): danos ao próprio veículo.</summary>
    Casco = 2,

    /// <summary>Seguro de responsabilidade civil facultativa de veículos (RCF-V): danos a terceiros.</summary>
    ResponsabilidadeCivil = 3,

    /// <summary>Apólice multirrisco/empresarial de frota cobrindo casco + RCF-V e assistência.</summary>
    Frota = 4,
}

/// <summary>Situação (estado) de uma apólice de seguro de veículo no seu ciclo de vigência.</summary>
public enum SituacaoApolice
{
    /// <summary>Apólice contratada e dentro da vigência (cobertura ativa).</summary>
    Vigente = 1,

    /// <summary>Apólice cuja vigência expirou sem renovação (cobertura cessada).</summary>
    Vencida = 2,

    /// <summary>Apólice cancelada antes do fim da vigência (endosso de cancelamento); estado terminal.</summary>
    Cancelada = 3,
}

/// <summary>Situação (estado) de um condutor (motorista) habilitado da frota.</summary>
public enum SituacaoCondutor
{
    /// <summary>Condutor ativo, apto a ser escalado (se a CNH estiver válida).</summary>
    Ativo = 1,

    /// <summary>Condutor suspenso (medida administrativa interna), temporariamente impedido de dirigir.</summary>
    Suspenso = 2,

    /// <summary>Condutor inativo (desligado/afastado); estado terminal para escala de frota.</summary>
    Inativo = 3,
}
