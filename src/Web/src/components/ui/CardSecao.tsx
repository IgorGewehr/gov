// Seção de card reutilizável (dashboards/telas): cabeçalho com TÍTULO (h2),
// SUBTÍTULO muted opcional e área de ação à direita (ex.: Tag/semáforo de status).
// O corpo recebe o conteúdo; a `nota` de rodapé vira uma CAPTION discreta (não
// texto solto) — ideal para fundamentos legais ("Percentuais sobre a dotação...").
// Hierarquia tipográfica consistente em todas as telas que usam este componente.
import type { ReactNode } from 'react';
import { Card } from './Card';

export interface CardSecaoProps {
  /** Título da seção (h2). */
  titulo: ReactNode;
  /** Subtítulo muted, largura legível controlada. */
  subtitulo?: ReactNode;
  /** Ação/indicador à direita do título (ex.: <Tag> de semáforo). */
  acao?: ReactNode;
  /** Nota de rodapé discreta (fundamento legal/explicação). */
  nota?: ReactNode;
  children: ReactNode;
  className?: string;
}

export function CardSecao({ titulo, subtitulo, acao, nota, children, className }: CardSecaoProps) {
  return (
    <Card
      className={className}
      header={
        <div className="tg-secao-cabecalho">
          <div className="tg-secao-titulos">
            <h2 className="tg-secao-titulo">{titulo}</h2>
            {subtitulo != null && <p className="tg-secao-subtitulo">{subtitulo}</p>}
          </div>
          {acao != null && <div className="tg-secao-acao">{acao}</div>}
        </div>
      }
      footer={nota != null ? <p className="tg-caption mb-0">{nota}</p> : undefined}
    >
      {children}
    </Card>
  );
}
