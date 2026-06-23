// Primitivos de DATAVIZ do Painel do Gestor (visão prefeito/gestor): barra de
// progresso com semáforo, medidor de limite (gauge linear com faixas legais) e
// item da faixa-resumo de KPIs. São LOCAIS do módulo — o dashboard executivo
// precisa de "barras/medidores" que os componentes-base (genéricos) não expõem.
// Regra gov.br/eMAG: status NUNCA só por cor — cada visual carrega rótulo/valor
// textual e role/aria apropriados. Consome só tokens globais (via painelgestor.css).
import type { CSSProperties, ReactNode } from 'react';

/** Tom semântico compartilhado pelos visuais (status, não decoração). */
export type Tom = 'neutro' | 'sucesso' | 'alerta' | 'perigo' | 'info';

const TOM_SUFIXO: Record<Tom, string> = {
  neutro: '',
  sucesso: 'is-sucesso',
  alerta: 'is-alerta',
  perigo: 'is-perigo',
  info: 'is-info',
};

/** Limita um número ao intervalo [0, 100]. */
function pct(fracao: number): number {
  if (!Number.isFinite(fracao)) return 0;
  return Math.min(100, Math.max(0, fracao * 100));
}

// ---------------------------------------------------------------------------
// ResumoItem — célula da faixa-resumo (hero): número grande que "salta".
// ---------------------------------------------------------------------------

export interface ResumoItemProps {
  icone: string;
  label: ReactNode;
  valor: ReactNode;
  secundario?: ReactNode;
  tom?: Tom;
}

export function ResumoItem({ icone, label, valor, secundario, tom = 'neutro' }: ResumoItemProps) {
  return (
    <div className={`pg-resumo-item${TOM_SUFIXO[tom] ? ` ${TOM_SUFIXO[tom]}` : ''}`}>
      <div className="pg-resumo-cabecalho">
        <span className="pg-resumo-icone">
          <i className={icone} aria-hidden="true" />
        </span>
        <span className="pg-resumo-label">{label}</span>
      </div>
      <span className="pg-resumo-valor">{valor}</span>
      {secundario != null && <span className="pg-resumo-secundario">{secundario}</span>}
    </div>
  );
}

/** Wrapper da faixa-resumo (grid responsivo). */
export function ResumoGrade({ children }: { children: ReactNode }) {
  return <div className="pg-resumo">{children}</div>;
}

// ---------------------------------------------------------------------------
// BarraProgresso — % de um total, com semáforo e (opcional) marca de limite.
// ---------------------------------------------------------------------------

export interface BarraProgressoProps {
  rotulo: ReactNode;
  /** Fração 0..1 (preenchimento). */
  fracao: number;
  /** Texto do valor à direita do rótulo (ex.: "62,40%"). */
  valorTexto: ReactNode;
  /** Linha de contexto abaixo da barra (ex.: "R$ 12,3 mi de R$ 19,7 mi"). */
  rodape?: ReactNode;
  tom?: Tom;
  /** Marca tracejada do limite mínimo exigido (fração 0..1), p/ mínimos. */
  marcaLimite?: number;
}

export function BarraProgresso({
  rotulo,
  fracao,
  valorTexto,
  rodape,
  tom = 'info',
  marcaLimite,
}: BarraProgressoProps) {
  const valor = pct(fracao);
  return (
    <div className="pg-barra">
      <div className="pg-barra-topo">
        <span className="pg-barra-rotulo">{rotulo}</span>
        <span className="pg-barra-valor">{valorTexto}</span>
      </div>
      <div
        className="pg-barra-trilho"
        role="progressbar"
        aria-valuenow={Math.round(valor)}
        aria-valuemin={0}
        aria-valuemax={100}
      >
        <span
          className={`pg-barra-preenchimento${TOM_SUFIXO[tom] ? ` ${TOM_SUFIXO[tom]}` : ''}`}
          style={{ width: `${valor}%` }}
        />
        {marcaLimite != null && (
          <span className="pg-barra-limite" style={{ left: `${pct(marcaLimite)}%` }} />
        )}
      </div>
      {rodape != null && <span className="pg-barra-rodape">{rodape}</span>}
    </div>
  );
}

// ---------------------------------------------------------------------------
// MedidorLimite — gauge linear de % vs faixas legais (LRF). O trilho pinta as
// faixas (verde/amarelo/vermelho) nas posições alerta..legal; o ponteiro marca
// a posição atual. Escala normalizada para que o limite legal fique em ~90% da
// largura (margem visual para o "estouro").
// ---------------------------------------------------------------------------

export interface FaixaMedidor {
  rotulo: string;
  /** Fração 0..1. */
  fracao: number;
  classe: string;
}

export interface MedidorLimiteProps {
  /** Fração 0..1 da posição atual (ex.: pessoal/RCL). */
  atual: number;
  /** Texto grande do valor atual (ex.: "48,12%"). */
  atualTexto: ReactNode;
  /** Faixas legais (alerta, prudencial, legal) — frações 0..1. */
  faixas: FaixaMedidor[];
  tom?: Tom;
}

export function MedidorLimite({ atual, atualTexto, faixas, tom = 'neutro' }: MedidorLimiteProps) {
  // Escala: o maior limite (legal) ancora em 90% da largura; deixa 10% p/ estouro.
  const limiteLegal = faixas.length > 0 ? Math.max(...faixas.map((f) => f.fracao)) : 1;
  const limiteAlerta = faixas.length > 0 ? Math.min(...faixas.map((f) => f.fracao)) : 0;
  const escala = limiteLegal > 0 ? 90 / (limiteLegal * 100) : 1;
  const posicao = (fracao: number) => Math.min(100, Math.max(0, pct(fracao) * escala));

  const trilhoStyle = {
    '--pg-faixa-alerta': `${posicao(limiteAlerta)}%`,
    '--pg-faixa-legal': `${posicao(limiteLegal)}%`,
  } as CSSProperties;

  return (
    <div className="pg-medidor">
      <div className="pg-medidor-topo">
        <span className={`pg-medidor-atual${TOM_SUFIXO[tom] ? ` ${TOM_SUFIXO[tom]}` : ''}`}>
          {atualTexto}
        </span>
      </div>
      <div
        className="pg-medidor-trilho"
        style={trilhoStyle}
        role="meter"
        aria-valuenow={Math.round(pct(atual))}
        aria-valuemin={0}
        aria-valuemax={Math.round(limiteLegal * 100)}
      >
        {faixas.map((f) => (
          <span
            key={f.rotulo}
            className="pg-medidor-marca"
            style={{ left: `${posicao(f.fracao)}%` }}
          />
        ))}
        <span
          className={`pg-medidor-ponteiro${TOM_SUFIXO[tom] ? ` ${TOM_SUFIXO[tom]}` : ''}`}
          style={{ left: `${posicao(atual)}%` }}
        />
      </div>
      <div className="pg-medidor-legenda">
        {faixas.map((f) => (
          <span key={f.rotulo} className="pg-medidor-legenda-item">
            <span className={`pg-medidor-legenda-ponto ${f.classe}`} />
            {f.rotulo}
          </span>
        ))}
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Dado — par rótulo/valor compacto (linha de detalhe abaixo de uma barra/gauge).
// ---------------------------------------------------------------------------

export function Dado({ label, valor }: { label: ReactNode; valor: ReactNode }) {
  return (
    <div>
      <span className="pg-dado-label">{label}</span>
      <span className="pg-dado-valor">{valor}</span>
    </div>
  );
}

/** Grade compacta de <Dado> (detalhes secundários de um KPI). */
export function DadoGrade({ children }: { children: ReactNode }) {
  return <div className="pg-minimo-grade">{children}</div>;
}
