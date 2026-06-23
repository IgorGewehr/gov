// Toolbar / ActionBar — agrupa ações com hierarquia (mata os "botões jogados").
//
// Regra de hierarquia (Design System §4): no máximo UM botão primário por contexto,
// posicionado à DIREITA (último da fila); secundários/ghost à esquerda. Por isso o
// alinhamento padrão é `end`. Use <Toolbar.Group> para separar grupos (ex.: filtros |
// ações) com um divisor sutil. Usado em PageHeader.actions, topo de listas e rodapé
// de Modal.
//
// Uso:
//   <Toolbar>
//     <Button variant="ghost">Limpar</Button>
//     <Button variant="primary">Consultar</Button>
//   </Toolbar>
//
//   <Toolbar align="between">
//     <Toolbar.Group><span>123 registros</span></Toolbar.Group>
//     <Toolbar.Group><Paginacao /></Toolbar.Group>
//   </Toolbar>
import type { ReactNode } from 'react';

export interface ToolbarProps {
  children: ReactNode;
  /** Alinhamento horizontal das ações (default `end` — primário à direita). */
  align?: 'start' | 'end' | 'between';
  className?: string;
  /** aria-label quando a toolbar é um landmark de ações distinto. */
  'aria-label'?: string;
}

export interface ToolbarGroupProps {
  children: ReactNode;
  className?: string;
}

const ALIGN_CLASS: Record<NonNullable<ToolbarProps['align']>, string> = {
  start: 'tg-toolbar-start',
  end: 'tg-toolbar-end',
  between: 'tg-toolbar-between',
};

export function Toolbar({ children, align = 'end', className, ...rest }: ToolbarProps) {
  const classes = ['tg-toolbar', ALIGN_CLASS[align], className ?? ''].filter(Boolean).join(' ');
  return (
    <div className={classes} {...rest}>
      {children}
    </div>
  );
}

/** Grupo lógico dentro da toolbar (separado por divisor sutil do grupo anterior). */
function ToolbarGroup({ children, className }: ToolbarGroupProps) {
  return <div className={`tg-toolbar-group${className ? ` ${className}` : ''}`}>{children}</div>;
}

Toolbar.Group = ToolbarGroup;
