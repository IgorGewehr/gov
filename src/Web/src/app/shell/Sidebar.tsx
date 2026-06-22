// Navegação lateral (landmark <nav>): lista os módulos do registry (espelha os
// módulos licenciados por tenant). Item ativo com aria-current (NavLink) + destaque
// visual. Em mobile, abre/fecha como painel off-canvas (prop `aberta`).
//
// Gating de visibilidade: itens com `nav.permissions` (ex.: Administração do
// Sistema) só aparecem para quem possui ALGUMA dessas permissões (claim "perm",
// semântica "OU"). Itens sem `permissions` são visíveis a qualquer sessão
// autenticada. Isto é experiência do usuário ("negar por padrão", CLAUDE.md §6);
// a autorização real continua no backend.
import { NavLink } from 'react-router-dom';
import { moduleNavItems } from '../../modules/registry';
import { useAuth } from '../../auth/useAuth';

interface SidebarProps {
  aberta?: boolean;
}

export function Sidebar({ aberta = false }: SidebarProps) {
  const { hasPermission } = useAuth();

  const itensVisiveis = moduleNavItems.filter(
    (item) =>
      !item.permissions ||
      item.permissions.length === 0 ||
      item.permissions.some((p) => hasPermission(p)),
  );

  return (
    <nav className={`tg-sidebar${aberta ? ' aberta' : ''}`} aria-label="Módulos">
      <p className="tg-sidebar-title">Módulos</p>
      <ul className="tg-nav">
        {itensVisiveis.map((item) => (
          <li key={item.path}>
            <NavLink to={item.path} className={({ isActive }) => (isActive ? 'active' : undefined)}>
              <i className={`tg-ico ${item.icon ?? 'fas fa-folder'}`} aria-hidden="true" />
              <span>{item.label}</span>
            </NavLink>
          </li>
        ))}
      </ul>
    </nav>
  );
}
