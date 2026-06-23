// Faixa-resumo (hero) do Painel do Gestor: os 5 KPIs do exercício destilados em
// números que SALTAM — execução paga, conformidade dos mínimos, % pessoal/RCL,
// arrecadação e prestação de contas. Cada célula carrega o semáforo (cor + ícone +
// valor textual) para leitura imediata pelo prefeito/gestor. É a primeira coisa
// que aparece abaixo do cabeçalho; os cards abaixo detalham cada indicador.
import { formatarFracao, formatarMoedaCompacta, minimoAtingido } from './painelgestor.helpers';
import { SituacaoLimite } from './api';
import type { PainelGestorDto } from './api';
import { ResumoGrade, ResumoItem, type Tom } from './PainelGestorVis';

function tomPorSituacao(situacao: SituacaoLimite): Tom {
  switch (situacao) {
    case SituacaoLimite.Adequado:
      return 'sucesso';
    case SituacaoLimite.Alerta:
      return 'alerta';
    case SituacaoLimite.Excedido:
      return 'perigo';
    default:
      return 'neutro';
  }
}

export function PainelGestorResumo({ dados }: { dados: PainelGestorDto }) {
  const totalMinimos = dados.minimos.length;
  const minimosOk = dados.minimos.filter((m) => minimoAtingido(m.situacao)).length;
  const minimosTom: Tom =
    totalMinimos === 0 ? 'neutro' : minimosOk === totalMinimos ? 'sucesso' : 'perigo';

  const pessoalTom = tomPorSituacao(dados.pessoalLrf.situacao);
  const tceTom = tomPorSituacao(dados.prestacaoContas.situacao);

  return (
    <ResumoGrade>
      <ResumoItem
        icone="fas fa-money-check-dollar"
        label="Execução paga"
        valor={formatarFracao(dados.execucaoOrcamentaria.percentualPago)}
        secundario={`${formatarMoedaCompacta(dados.execucaoOrcamentaria.pago)} pagos no exercício`}
        tom="info"
      />
      <ResumoItem
        icone="fas fa-scale-balanced"
        label="Mínimos constitucionais"
        valor={totalMinimos === 0 ? '—' : `${minimosOk}/${totalMinimos}`}
        secundario={
          totalMinimos === 0
            ? 'Sem apuração no exercício'
            : minimosOk === totalMinimos
              ? 'Saúde e Educação atingidos'
              : 'Há mínimo NÃO atingido'
        }
        tom={minimosTom}
      />
      <ResumoItem
        icone="fas fa-users-gear"
        label="Pessoal / RCL (LRF)"
        valor={formatarFracao(dados.pessoalLrf.percentualDaRcl)}
        secundario={`Teto legal ${formatarFracao(dados.pessoalLrf.limiteLegal)}`}
        tom={pessoalTom}
      />
      <ResumoItem
        icone="fas fa-hand-holding-dollar"
        label="Arrecadação tributária"
        valor={formatarMoedaCompacta(dados.arrecadacao.arrecadacaoTributaria)}
        secundario={`Dívida ativa: ${formatarMoedaCompacta(dados.arrecadacao.dividaAtivaSaldoInscrito)} inscritos`}
        tom="neutro"
      />
      <ResumoItem
        icone="fas fa-file-circle-check"
        label="Prestação de contas (TCE)"
        valor={dados.prestacaoContas.emDia ? 'Em dia' : 'Pendências'}
        secundario={`${dados.prestacaoContas.remessasComPrazoVencido.toLocaleString('pt-BR')} remessa(s) vencida(s)`}
        tom={tceTom}
      />
    </ResumoGrade>
  );
}
