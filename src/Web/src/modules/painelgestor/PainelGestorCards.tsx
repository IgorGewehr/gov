// Cards grandes e legíveis dos 5 KPIs do Painel do Gestor (visão prefeito/gestor).
// Cada card é autocontido: recebe seu DTO e renderiza valores + semáforo de cor
// gov.br. A COR comunica conformidade (mínimo atingido? pessoal dentro do limite
// LRF? remessas TCE em dia?), não é decoração.
import type { ReactNode } from 'react';
import { Card, Tag } from '../../components/ui';
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

/** Item rotulado de um KPI (grande e legível — visão executiva). */
function Indicador({ rotulo, children }: { rotulo: string; children: ReactNode }) {
  return (
    <div className="col-sm-6 col-lg-4 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-up-01 text-semi-bold">{children}</dd>
    </div>
  );
}

/** (a) Execução orçamentária — empenhado/liquidado/pago vs dotação. */
export function ExecucaoCard({ d }: { d: ExecucaoOrcamentariaDto }) {
  return (
    <Card
      className="mb-4"
      header={<strong>Execução orçamentária do exercício</strong>}
    >
      <dl className="row">
        <Indicador rotulo="Dotação atualizada">{formatarMoeda(d.dotacaoAtualizada)}</Indicador>
        <Indicador rotulo="Empenhado">
          {formatarMoeda(d.empenhado)}{' '}
          <span className="text-gray-60 text-down-01">({formatarFracao(d.percentualEmpenhado)})</span>
        </Indicador>
        <Indicador rotulo="Liquidado">
          {formatarMoeda(d.liquidado)}{' '}
          <span className="text-gray-60 text-down-01">({formatarFracao(d.percentualLiquidado)})</span>
        </Indicador>
        <Indicador rotulo="Pago">
          {formatarMoeda(d.pago)}{' '}
          <span className="text-gray-60 text-down-01">({formatarFracao(d.percentualPago)})</span>
        </Indicador>
      </dl>
      <p className="mb-0 text-gray-60 text-down-01">
        Percentuais sobre a dotação atualizada (LOA + créditos). Empenho → Liquidação → Pagamento
        (Lei 4.320/1964).
      </p>
    </Card>
  );
}

/** (b) Mínimos constitucionais (Saúde 15% / Educação 25%) com semáforo. */
export function MinimosCard({ minimos }: { minimos: MinimoConstitucionalDto[] }) {
  return (
    <Card className="mb-4" header={<strong>Mínimos constitucionais (Saúde e Educação)</strong>}>
      {minimos.length === 0 ? (
        <p className="mb-0 text-gray-60">Sem apuração de mínimos para o exercício.</p>
      ) : (
        <div className="row">
          {minimos.map((m) => {
            const atingido = minimoAtingido(m.situacao);
            return (
              <div className="col-md-6 mb-3" key={m.setor}>
                <div className="d-flex justify-content-between align-items-center mb-2">
                  <strong>{rotuloSetor(m.setor)}</strong>
                  <Tag variant={atingido ? 'success' : 'danger'}>
                    {atingido ? 'Mínimo atingido' : 'Mínimo NÃO atingido'}
                  </Tag>
                </div>
                <dl className="row">
                  <Indicador rotulo="Aplicado">
                    <span className={atingido ? 'text-success' : 'text-danger'}>
                      {formatarFracao(m.percentualAplicado)}
                    </span>
                  </Indicador>
                  <Indicador rotulo="Mínimo vigente">{formatarFracao(m.percentualMinimo)}</Indicador>
                  <Indicador rotulo="Aplicado (R$)">{formatarMoeda(m.aplicado)}</Indicador>
                </dl>
                <p className="mb-0 text-gray-60 text-down-01">{fundamentoMinimo(m.setor)}</p>
              </div>
            );
          })}
        </div>
      )}
    </Card>
  );
}

/** (c) Arrecadação tributária + dívida ativa. */
export function ArrecadacaoCard({ d }: { d: ArrecadacaoDto }) {
  return (
    <Card className="mb-4" header={<strong>Arrecadação e dívida ativa</strong>}>
      <dl className="row">
        <Indicador rotulo="Arrecadação tributária">{formatarMoeda(d.arrecadacaoTributaria)}</Indicador>
        <Indicador rotulo="Dívida ativa — inscrita">
          {formatarMoeda(d.dividaAtivaSaldoInscrito)}
        </Indicador>
        <Indicador rotulo="Dívida ativa — ajuizada">
          {formatarMoeda(d.dividaAtivaSaldoAjuizado)}
        </Indicador>
        <Indicador rotulo="Dívida ativa — recuperada">
          {formatarMoeda(d.dividaAtivaRecuperada)}
        </Indicador>
      </dl>
      <p className="mb-0 text-gray-60 text-down-01">
        Estoque inscrito, parcela ajuizada e recuperação no exercício (cobrança da Dívida Ativa).
      </p>
    </Card>
  );
}

/** (d) Custo de pessoal + % da RCL com semáforo LRF (legal/prudencial/alerta). */
export function PessoalLrfCard({ d }: { d: DespesaPessoalLrfDto }) {
  const variante = variantePorSituacao(d.situacao);
  return (
    <Card
      className="mb-4"
      header={
        <div className="d-flex justify-content-between align-items-center">
          <strong>Despesa de pessoal — % da RCL (LRF)</strong>
          <Tag variant={variante}>{rotuloSituacaoLrf(d.situacao)}</Tag>
        </div>
      }
    >
      <dl className="row">
        <Indicador rotulo="Pessoal / RCL">
          <span
            className={
              variante === 'danger'
                ? 'text-danger'
                : variante === 'warning'
                  ? 'text-warning'
                  : 'text-success'
            }
          >
            {formatarFracao(d.percentualDaRcl)}
          </span>
        </Indicador>
        <Indicador rotulo="Limite de alerta">{formatarFracao(d.limiteAlerta)}</Indicador>
        <Indicador rotulo="Limite prudencial">{formatarFracao(d.limitePrudencial)}</Indicador>
        <Indicador rotulo="Limite legal">{formatarFracao(d.limiteLegal)}</Indicador>
        <Indicador rotulo="Despesa de pessoal">{formatarMoeda(d.despesaPessoal)}</Indicador>
        <Indicador rotulo="RCL (12 meses)">{formatarMoeda(d.receitaCorrenteLiquida)}</Indicador>
      </dl>
      <p className="mb-0 text-gray-60 text-down-01">
        Limites de alerta/prudencial/legal são parametrizáveis por tenant (LC 101/2000 — LRF). Para
        Executivo municipal o teto legal é 54% da RCL.
      </p>
    </Card>
  );
}

/** (e) Prontidão da prestação de contas (remessas TCE-RS) com semáforo. */
export function PrestacaoContasCard({ d }: { d: PrestacaoContasDto }) {
  return (
    <Card
      className="mb-4"
      header={
        <div className="d-flex justify-content-between align-items-center">
          <strong>Prestação de contas (TCE-RS)</strong>
          <Tag variant={variantePorSituacao(d.situacao)}>{rotuloSituacaoTce(d.situacao)}</Tag>
        </div>
      }
    >
      <dl className="row">
        <Indicador rotulo="Remessas enviadas">{d.remessasEnviadas.toLocaleString('pt-BR')}</Indicador>
        <Indicador rotulo="Com prazo vencido">
          <span className={d.remessasComPrazoVencido > 0 ? 'text-danger' : 'text-success'}>
            {d.remessasComPrazoVencido.toLocaleString('pt-BR')}
          </span>
        </Indicador>
        <Indicador rotulo="Situação geral">
          <span className={d.emDia ? 'text-success' : 'text-danger'}>
            {d.emDia ? 'Em dia' : 'Pendências'}
          </span>
        </Indicador>
      </dl>
      <p className="mb-0 text-gray-60 text-down-01">
        Remessas ao TCE-RS (SIAPC/PAD) do exercício. Prazos parametrizáveis por tenant/calendário do
        Tribunal.
      </p>
    </Card>
  );
}
