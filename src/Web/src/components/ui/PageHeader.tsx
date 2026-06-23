// Cabeçalho de página: eyebrow (overline do módulo) opcional, título (h1 — um por
// página/main), descrição e slot de AÇÕES à direita. As ações devem receber um
// <Toolbar> (hierarquia 1ª/2ª), nunca botões soltos. Borda inferior sutil separa
// o cabeçalho do conteúdo (tokens em components.css).
import type { ReactNode } from 'react';

export interface PageHeaderProps {
  title: ReactNode;
  description?: ReactNode;
  /** Overline maiúsculo (ex.: nome do módulo / contexto). Opcional. */
  eyebrow?: ReactNode;
  /** Ações à direita (preferir <Toolbar align="end">). */
  actions?: ReactNode;
}

export function PageHeader({ title, description, eyebrow, actions }: PageHeaderProps) {
  return (
    <div className="tg-page-header">
      <div className="tg-page-header-titulos">
        {eyebrow && <span className="tg-page-eyebrow">{eyebrow}</span>}
        <h1 className="tg-page-title mb-1">{title}</h1>
        {description && <p className="tg-page-subtitulo mb-0">{description}</p>}
      </div>
      {actions && <div className="tg-page-header-acoes">{actions}</div>}
    </div>
  );
}
