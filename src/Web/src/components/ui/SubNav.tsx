// Sub-navegação interna de módulo (entre os contextos/agregados de um Bounded Context).
// Mantém UMA entrada por módulo na Sidebar; a navegação entre seções acontece aqui.
//
// Por que existe: havia 6 sub-navs quase idênticas (RH, Tributos, Finanças, Saúde,
// Educação, Assistência Social) + a do Legislativo, com risco de drift. Pior: módulos
// com MUITAS abas (RH/Tributos/Finanças/Legislativo têm 9–10) podiam transbordar na
// horizontal sem affordance — o usuário não via que existiam mais abas e deixava de
// usar áreas inteiras. Este componente padroniza tudo e GARANTE a quebra de linha
// (flex-wrap) via CSS próprio (.tg-subnav), sem depender de utilitárias globais que
// podem ser sobrescritas. Todas as abas ficam SEMPRE visíveis — sem scroll escondido.
//
// Acessibilidade: landmark <nav aria-label> + NavLink com aria-current (gov.br DS/eMAG).
import { NavLink } from 'react-router-dom';
import { useAuth } from '../../auth/useAuth';

export interface SubNavItem {
  /** Rota de destino. */
  to: string;
  /** Rótulo visível da aba. */
  label: string;
  /** Marca a aba ativa apenas na correspondência exata (use na rota índice do módulo). */
  end?: boolean;
  /** Classe de ícone Font Awesome (ex.: 'fas fa-folder-open'); opcional. */
  icon?: string;
  /** Quando presente, a aba só aparece se o usuário tiver esta permissão. */
  perm?: string;
}

export interface SubNavProps {
  /** Texto do aria-label do landmark (ex.: 'Seções de Recursos Humanos'). */
  ariaLabel: string;
  /** Abas da sub-navegação, na ordem de exibição. */
  itens: ReadonlyArray<SubNavItem>;
}

export function SubNav({ ariaLabel, itens }: SubNavProps) {
  // hasPermission é estável; filtramos as abas protegidas por permissão (gating de UI,
  // espelhando o RBAC do backend — a autorização real continua server-side).
  const { hasPermission } = useAuth();
  const visiveis = itens.filter((item) => !item.perm || hasPermission(item.perm));
  return (
    <nav className="tg-subnav mb-4" aria-label={ariaLabel}>
      <ul className="tg-subnav-lista">
        {visiveis.map((item) => (
          <li key={item.to}>
            <NavLink
              to={item.to}
              end={item.end}
              className={({ isActive }) => `tg-tab${isActive ? ' active' : ''}`}
            >
              {item.icon ? <i className={item.icon} aria-hidden="true" /> : null}
              {item.label}
            </NavLink>
          </li>
        ))}
      </ul>
    </nav>
  );
}
