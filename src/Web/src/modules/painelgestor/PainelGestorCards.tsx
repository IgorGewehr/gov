// Cards grandes e legíveis dos 5 KPIs do Painel do Gestor (visão prefeito/gestor).
// Cada card é autocontido: recebe seu DTO e renderiza valores + semáforo de cor
// gov.br. A COR comunica conformidade (mínimo atingido? pessoal dentro do limite
// LRF? remessas TCE em dia?), não é decoração.
//
// Usa os componentes reutilizáveis <CardSecao> (título/subtítulo/nota-caption) e
// <Metrica>/<MetricaGrade> (rótulo pequeno em cima, valor grande, secundário claro
// em linha própria) — referência de layout para as demais telas.
import { CardSecao, Metrica, MetricaGrade, Tag } from '../../components/ui';
import type { MetricaProps } from '../../components/ui';
import { formatarMoeda } from '../../i18n/format';
import type {
  ArrecadacaoDto,
  DespesaPessoalLrfDto,
  ExecucaoOrcamentariaDto,
  MinimoConstitucionalDto,
  PrestacaoContasDto,
} from './api';
import {
  formatarFracao,
  fundamentoMinimo,
  minimoAtingido,
  rotuloSetor,
  rotuloSituacaoLrf,
  rotuloSituacaoTce,
  variantePorSituacao,
} from './painelgestor.helpers';

/** (a) Execução orçamentária — empenhado/liquidado/pago vs dotação. */
export function ExecucaoCard({ d }: { d: ExecucaoOrcamentariaDto }) {
  return (
    <CardSecao
      className="mb-4"
      titulo="Execução orçamentária do exercício"
      subtitulo="Empenho → Liquidação → Pagamento (Lei 4.320/1964)."
      nota="Percentuais calculados sobre a dotação atualizada (LOA + créditos adicionais)."
    >
      <MetricaGrade>
        <Metrica label="Dotação atualizada" valor={formatarMoeda(d.dotacaoAtualizada)} />
        <Metrica
          label="Empenhado"
          valor={formatarMoeda(d.empenhado)}
          secundario={`${formatarFracao(d.percentualEmpenhado)} da dotação`}
        />
        <Metrica
          label="Liquidado"
          valor={formatarMoeda(d.liquidado)}
          secundario={`${formatarFracao(d.percentualLiquidado)} da dotação`}
        />
        <Metrica
          label="Pago"
          valor={formatarMoeda(d.pago)}
          secundario={`${formatarFracao(d.percentualPago)} da dotação`}
        />
      </MetricaGrade>
    </CardSecao>
  );
}

/** (b) Mínimos constitucionais (Saúde 15% / Educação 25%) com semáforo. */
export function MinimosCard({ minimos }: { minimos: MinimoConstitucionalDto[] }) {
  return (
    <CardSecao
      className="mb-4"
      titulo="Mínimos constitucionais (Saúde e Educação)"
      subtitulo="Aplicação mínima da receita exigida pela Constituição, com semáforo de conformidade."
    >
      {minimos.length === 0 ? (
        <p className="mb-0 text-gray-60">Sem apuração de mínimos para o exercício.</p>
      ) : (
        <div className="stack">
          {minimos.map((m) => {
            const atingido = minimoAtingido(m.situacao);
            return (
              <div key={m.setor}>
                <div className="tg-secao-cabecalho mb-2">
                  <strong>{rotuloSetor(m.setor)}</strong>
                  <Tag variant={atingido ? 'success' : 'danger'}>
                    {atingido ? 'Mínimo atingido' : 'Mínimo NÃO atingido'}
                  </Tag>
                </div>
                <MetricaGrade>
                  <Metrica
                    label="Aplicado"
                    valor={formatarFracao(m.percentualAplicado)}
                    tom={atingido ? 'sucesso' : 'perigo'}
                  />
                  <Metrica label="Mínimo vigente" valor={formatarFracao(m.percentualMinimo)} />
                  <Metrica label="Aplicado (R$)" valor={formatarMoeda(m.aplicado)} />
                </MetricaGrade>
                <p className="tg-caption mb-0 mt-2">{fundamentoMinimo(m.setor)}</p>
              </div>
            );
          })}
        </div>
      )}
    </CardSecao>
  );
}

/** (c) Arrecadação tributária + dívida ativa. */
export function ArrecadacaoCard({ d }: { d: ArrecadacaoDto }) {
  return (
    <CardSecao
      className="mb-4"
      titulo="Arrecadação e dívida ativa"
      subtitulo="Arrecadação tributária do exercício e situação do estoque da Dívida Ativa."
      nota="Estoque inscrito, parcela ajuizada e recuperação no exercício (cobrança da Dívida Ativa)."
    >
      <MetricaGrade>
        <Metrica label="Arrecadação tributária" valor={formatarMoeda(d.arrecadacaoTributaria)} />
        <Metrica label="Dívida ativa — inscrita" valor={formatarMoeda(d.dividaAtivaSaldoInscrito)} />
        <Metrica label="Dívida ativa — ajuizada" valor={formatarMoeda(d.dividaAtivaSaldoAjuizado)} />
        <Metrica label="Dívida ativa — recuperada" valor={formatarMoeda(d.dividaAtivaRecuperada)} />
      </MetricaGrade>
    </CardSecao>
  );
}

/** (d) Custo de pessoal + % da RCL com semáforo LRF (legal/prudencial/alerta). */
export function PessoalLrfCard({ d }: { d: DespesaPessoalLrfDto }) {
  const variante = variantePorSituacao(d.situacao);
  const tom: MetricaProps['tom'] =
    variante === 'danger' ? 'perigo' : variante === 'warning' ? 'alerta' : 'sucesso';
  return (
    <CardSecao
      className="mb-4"
      titulo="Despesa de pessoal — % da RCL (LRF)"
      subtitulo="Comparação da despesa de pessoal com a Receita Corrente Líquida e os limites legais."
      acao={<Tag variant={variante}>{rotuloSituacaoLrf(d.situacao)}</Tag>}
      nota="Limites de alerta/prudencial/legal parametrizáveis por tenant (LC 101/2000 — LRF). Para Executivo municipal o teto legal é 54% da RCL."
    >
      <MetricaGrade>
        <Metrica label="Pessoal / RCL" valor={formatarFracao(d.percentualDaRcl)} tom={tom} />
        <Metrica label="Limite de alerta" valor={formatarFracao(d.limiteAlerta)} />
        <Metrica label="Limite prudencial" valor={formatarFracao(d.limitePrudencial)} />
        <Metrica label="Limite legal" valor={formatarFracao(d.limiteLegal)} />
        <Metrica label="Despesa de pessoal" valor={formatarMoeda(d.despesaPessoal)} />
        <Metrica label="RCL (12 meses)" valor={formatarMoeda(d.receitaCorrenteLiquida)} />
      </MetricaGrade>
    </CardSecao>
  );
}

/** (e) Prontidão da prestação de contas (remessas TCE-RS) com semáforo. */
export function PrestacaoContasCard({ d }: { d: PrestacaoContasDto }) {
  return (
    <CardSecao
      className="mb-4"
      titulo="Prestação de contas (TCE-RS)"
      subtitulo="Situação das remessas obrigatórias ao Tribunal de Contas no exercício."
      acao={<Tag variant={variantePorSituacao(d.situacao)}>{rotuloSituacaoTce(d.situacao)}</Tag>}
      nota="Remessas ao TCE-RS (SIAPC/PAD) do exercício. Prazos parametrizáveis por tenant/calendário do Tribunal."
    >
      <MetricaGrade>
        <Metrica label="Remessas enviadas" valor={d.remessasEnviadas.toLocaleString('pt-BR')} />
        <Metrica
          label="Com prazo vencido"
          valor={d.remessasComPrazoVencido.toLocaleString('pt-BR')}
          tom={d.remessasComPrazoVencido > 0 ? 'perigo' : 'sucesso'}
        />
        <Metrica
          label="Situação geral"
          valor={d.emDia ? 'Em dia' : 'Pendências'}
          tom={d.emDia ? 'sucesso' : 'perigo'}
        />
      </MetricaGrade>
    </CardSecao>
  );
}
