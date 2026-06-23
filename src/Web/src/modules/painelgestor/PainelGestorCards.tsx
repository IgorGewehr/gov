// Cards de KPI do Painel do Gestor (visão prefeito/gestor) — DASHBOARD EXECUTIVO.
// Cada card é autocontido: recebe seu DTO e renderiza VALORES + DATAVIZ (barras de
// progresso para execução/arrecadação, medidor de faixas para a LRF, barra com marca
// de mínimo para Saúde/Educação) + semáforo de cor gov.br. A COR é STATUS (conformidade),
// jamais decoração: status sempre vem acompanhado de rótulo/valor textual (eMAG/WCAG).
import { CardSecao, Tag } from '../../components/ui';
import { formatarMoeda } from '../../i18n/format';
import type {
  ArrecadacaoDto,
  DespesaPessoalLrfDto,
  ExecucaoOrcamentariaDto,
  MinimoConstitucionalDto,
  PrestacaoContasDto,
} from './api';
import { SituacaoLimite } from './api';
import {
  formatarFracao,
  fundamentoMinimo,
  minimoAtingido,
  rotuloSetor,
  rotuloSituacaoLrf,
  rotuloSituacaoTce,
  variantePorSituacao,
} from './painelgestor.helpers';
import {
  BarraProgresso,
  Dado,
  DadoGrade,
  MedidorLimite,
  type Tom,
} from './PainelGestorVis';

/** Mapeia o enum de semáforo para o tom dos visuais (consistente com as Tags). */
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

/** (a) Execução orçamentária — empenhado/liquidado/pago vs dotação (barras). */
export function ExecucaoCard({ d }: { d: ExecucaoOrcamentariaDto }) {
  const ctx = (valor: number) => `${formatarMoeda(valor)} de ${formatarMoeda(d.dotacaoAtualizada)}`;
  return (
    <CardSecao
      className="mb-4"
      titulo="Execução orçamentária do exercício"
      subtitulo="Empenho → Liquidação → Pagamento (Lei 4.320/1964), sobre a dotação atualizada."
      nota="Percentuais calculados sobre a dotação atualizada (LOA + créditos adicionais)."
    >
      <div className="stack">
        <BarraProgresso
          rotulo="Empenhado"
          fracao={d.percentualEmpenhado}
          valorTexto={formatarFracao(d.percentualEmpenhado)}
          rodape={ctx(d.empenhado)}
          tom="info"
        />
        <BarraProgresso
          rotulo="Liquidado"
          fracao={d.percentualLiquidado}
          valorTexto={formatarFracao(d.percentualLiquidado)}
          rodape={ctx(d.liquidado)}
          tom="info"
        />
        <BarraProgresso
          rotulo="Pago"
          fracao={d.percentualPago}
          valorTexto={formatarFracao(d.percentualPago)}
          rodape={ctx(d.pago)}
          tom="sucesso"
        />
      </div>
    </CardSecao>
  );
}

/** (b) Mínimos constitucionais (Saúde 15% / Educação 25%) — barra com marca de mínimo. */
export function MinimosCard({ minimos }: { minimos: MinimoConstitucionalDto[] }) {
  return (
    <CardSecao
      className="mb-4"
      titulo="Mínimos constitucionais (Saúde e Educação)"
      subtitulo="Aplicação mínima da receita exigida pela Constituição, com semáforo de conformidade."
    >
      {minimos.length === 0 ? (
        <p className="pg-vazio">Sem apuração de mínimos para o exercício.</p>
      ) : (
        <div className="stack">
          {minimos.map((m) => {
            const atingido = minimoAtingido(m.situacao);
            const tom: Tom = atingido ? 'sucesso' : 'perigo';
            return (
              <div key={m.setor} className={`pg-minimo ${atingido ? 'is-sucesso' : 'is-perigo'}`}>
                <div className="pg-minimo-cabecalho">
                  <h3 className="pg-minimo-titulo">
                    <i
                      className={`fas ${atingido ? 'fa-circle-check' : 'fa-triangle-exclamation'}`}
                      aria-hidden="true"
                    />
                    {rotuloSetor(m.setor)}
                  </h3>
                  <Tag variant={atingido ? 'success' : 'danger'}>
                    {atingido ? 'Mínimo atingido' : 'Mínimo NÃO atingido'}
                  </Tag>
                </div>
                <BarraProgresso
                  rotulo="Aplicado"
                  fracao={m.percentualAplicado}
                  valorTexto={formatarFracao(m.percentualAplicado)}
                  marcaLimite={m.percentualMinimo}
                  rodape={`Mínimo exigido (marca tracejada): ${formatarFracao(m.percentualMinimo)}`}
                  tom={tom}
                />
                <DadoGrade>
                  <Dado label="Aplicado (R$)" valor={formatarMoeda(m.aplicado)} />
                  <Dado label="Receita base" valor={formatarMoeda(m.receitaBase)} />
                  <Dado label="Mínimo vigente" valor={formatarFracao(m.percentualMinimo)} />
                </DadoGrade>
                <p className="tg-caption mb-0">{fundamentoMinimo(m.setor)}</p>
              </div>
            );
          })}
        </div>
      )}
    </CardSecao>
  );
}

/** (c) Arrecadação tributária + dívida ativa — taxa de recuperação da DA em barra. */
export function ArrecadacaoCard({ d }: { d: ArrecadacaoDto }) {
  const recuperacao = d.dividaAtivaSaldoInscrito > 0
    ? d.dividaAtivaRecuperada / d.dividaAtivaSaldoInscrito
    : 0;
  const ajuizamento = d.dividaAtivaSaldoInscrito > 0
    ? d.dividaAtivaSaldoAjuizado / d.dividaAtivaSaldoInscrito
    : 0;
  return (
    <CardSecao
      className="mb-4"
      titulo="Arrecadação e dívida ativa"
      subtitulo="Arrecadação tributária do exercício e situação do estoque da Dívida Ativa."
      nota="Estoque inscrito, parcela ajuizada e recuperação no exercício (cobrança da Dívida Ativa)."
    >
      <DadoGrade>
        <Dado label="Arrecadação tributária" valor={formatarMoeda(d.arrecadacaoTributaria)} />
        <Dado label="Dívida ativa — inscrita" valor={formatarMoeda(d.dividaAtivaSaldoInscrito)} />
        <Dado label="Dívida ativa — recuperada" valor={formatarMoeda(d.dividaAtivaRecuperada)} />
      </DadoGrade>
      <div className="stack" style={{ marginTop: 'var(--tg-space-5)' }}>
        <BarraProgresso
          rotulo="Recuperação da dívida ativa"
          fracao={recuperacao}
          valorTexto={formatarFracao(recuperacao)}
          rodape={`${formatarMoeda(d.dividaAtivaRecuperada)} recuperados do estoque inscrito`}
          tom={recuperacao >= 0.1 ? 'sucesso' : 'alerta'}
        />
        <BarraProgresso
          rotulo="Estoque ajuizado"
          fracao={ajuizamento}
          valorTexto={formatarFracao(ajuizamento)}
          rodape={`${formatarMoeda(d.dividaAtivaSaldoAjuizado)} em cobrança judicial`}
          tom="info"
        />
      </div>
    </CardSecao>
  );
}

/** (d) Custo de pessoal + % da RCL com MEDIDOR de faixas LRF (legal/prudencial/alerta). */
export function PessoalLrfCard({ d }: { d: DespesaPessoalLrfDto }) {
  const variante = variantePorSituacao(d.situacao);
  const tom = tomPorSituacao(d.situacao);
  return (
    <CardSecao
      className="mb-4"
      titulo="Despesa de pessoal — % da RCL (LRF)"
      subtitulo="Comparação da despesa de pessoal com a Receita Corrente Líquida e os limites legais."
      acao={<Tag variant={variante}>{rotuloSituacaoLrf(d.situacao)}</Tag>}
      nota="Limites de alerta/prudencial/legal parametrizáveis por tenant (LC 101/2000 — LRF). Para Executivo municipal o teto legal é 54% da RCL."
    >
      <MedidorLimite
        atual={d.percentualDaRcl}
        atualTexto={`${formatarFracao(d.percentualDaRcl)} da RCL`}
        tom={tom}
        faixas={[
          { rotulo: `Alerta ${formatarFracao(d.limiteAlerta)}`, fracao: d.limiteAlerta, classe: 'pg-ponto-alerta' },
          { rotulo: `Prudencial ${formatarFracao(d.limitePrudencial)}`, fracao: d.limitePrudencial, classe: 'pg-ponto-prudencial' },
          { rotulo: `Legal ${formatarFracao(d.limiteLegal)}`, fracao: d.limiteLegal, classe: 'pg-ponto-legal' },
        ]}
      />
      <DadoGrade>
        <Dado label="Despesa de pessoal" valor={formatarMoeda(d.despesaPessoal)} />
        <Dado label="RCL (12 meses)" valor={formatarMoeda(d.receitaCorrenteLiquida)} />
      </DadoGrade>
    </CardSecao>
  );
}

/** (e) Prontidão da prestação de contas (remessas TCE-RS) com semáforo. */
export function PrestacaoContasCard({ d }: { d: PrestacaoContasDto }) {
  const tom = tomPorSituacao(d.situacao);
  const vencidas = d.remessasComPrazoVencido;
  return (
    <CardSecao
      className="mb-4"
      titulo="Prestação de contas (TCE-RS)"
      subtitulo="Situação das remessas obrigatórias ao Tribunal de Contas no exercício."
      acao={<Tag variant={variantePorSituacao(d.situacao)}>{rotuloSituacaoTce(d.situacao)}</Tag>}
      nota="Remessas ao TCE-RS (SIAPC/PAD) do exercício. Prazos parametrizáveis por tenant/calendário do Tribunal."
    >
      <DadoGrade>
        <Dado label="Remessas enviadas" valor={d.remessasEnviadas.toLocaleString('pt-BR')} />
        <Dado label="Situação geral" valor={d.emDia ? 'Em dia' : 'Pendências'} />
      </DadoGrade>
      <div className={`pg-minimo ${vencidas > 0 ? 'is-perigo' : 'is-sucesso'}`} style={{ marginTop: 'var(--tg-space-4)' }}>
        <div className="pg-minimo-cabecalho">
          <h3 className="pg-minimo-titulo">
            <i
              className={`fas ${vencidas > 0 ? 'fa-triangle-exclamation' : 'fa-circle-check'}`}
              aria-hidden="true"
            />
            Remessas com prazo vencido
          </h3>
          <Tag variant={vencidas > 0 ? 'danger' : 'success'}>
            {vencidas.toLocaleString('pt-BR')} {vencidas === 1 ? 'remessa' : 'remessas'}
          </Tag>
        </div>
        <p className="pg-vazio" style={{ color: tom === 'perigo' ? 'var(--tg-danger-fg)' : 'var(--tg-gray-50)' }}>
          {vencidas > 0
            ? 'Há remessas fora do prazo — regularize para evitar apontamento do Tribunal.'
            : 'Nenhuma remessa em atraso no exercício.'}
        </p>
      </div>
    </CardSecao>
  );
}
