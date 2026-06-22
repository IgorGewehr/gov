// Cronometro visual de um orador na Tribuna. Calcula o tempo decorrido a partir
// do tempo consumido (fechado no servidor) somado ao trecho em andamento desde
// `iniciadoEm`, tickando localmente a cada segundo enquanto a fala esta ativa.
// Destaca o tempo EXCEDENTE (alem do concedido) em vermelho, com aria-live.
import { useEffect, useState } from 'react';
import { Tag } from '../../components/ui';
import { formatarDuracao, situacaoInscricaoTagVariant } from './legislativo.helpers';
import type { InscricaoResumo } from './tribuna.api';

/** Tempo decorrido (segundos) considerando o trecho em andamento, se houver. */
function calcularDecorrido(inscricao: InscricaoResumo, agora: number): number {
  const base = inscricao.tempoUtilizadoSegundos ?? 0;
  if (!inscricao.iniciadoEm) return base;
  const inicio = new Date(inscricao.iniciadoEm).getTime();
  if (Number.isNaN(inicio)) return base;
  return base + Math.max(0, Math.floor((agora - inicio) / 1000));
}

export function TribunaCronometro({ inscricao }: { inscricao: InscricaoResumo }) {
  const ativo = inscricao.iniciadoEm !== null;
  const [agora, setAgora] = useState(() => Date.now());

  // Tick local de 1s apenas enquanto a fala estiver em andamento (sem timer ocioso).
  useEffect(() => {
    if (!ativo) return;
    const handle = window.setInterval(() => setAgora(Date.now()), 1000);
    return () => window.clearInterval(handle);
  }, [ativo, inscricao.iniciadoEm]);

  const decorrido = calcularDecorrido(inscricao, agora);
  const concedido = inscricao.tempoConcedidoSegundos;
  const excedente = decorrido - concedido;
  const estourou = excedente > 0;
  const restante = Math.max(0, -excedente);

  return (
    <div className="d-flex align-items-center" style={{ gap: '1rem' }}>
      <div aria-live="polite" aria-atomic="true">
        <span
          className="text-up-02 text-semi-bold"
          style={{ fontVariantNumeric: 'tabular-nums', color: estourou ? 'var(--danger, #e60000)' : undefined }}
        >
          {formatarDuracao(decorrido)}
        </span>
        <span className="text-gray-60"> / {formatarDuracao(concedido)}</span>
      </div>
      {estourou ? (
        <Tag variant="danger">Excedido {formatarDuracao(excedente)}</Tag>
      ) : (
        <span className="text-gray-60 text-down-01">Restam {formatarDuracao(restante)}</span>
      )}
      <Tag variant={situacaoInscricaoTagVariant(inscricao.situacao)}>{inscricao.situacao}</Tag>
    </div>
  );
}
