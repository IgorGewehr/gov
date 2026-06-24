// Navegação lateral (landmark <nav>): lista os módulos do registry (espelha os
// módulos licenciados por tenant). Item ativo com aria-current (NavLink) + destaque
// visual. Pode COLAPSAR para um rail só de ícones (toggle no rodapé, com
// aria-expanded e persistência via useSidebarColapsada); no rail, cada item mostra
// o nome via tooltip (title) + aria-label. Em mobile, abre/fecha como painel
// off-canvas (prop `aberta`).
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
  /** Off-canvas aberta (mobile). */
  aberta?: boolean;
  /** Rail só de ícones (desktop). */
  colapsada?: boolean;
  /** Alterna colapsada/expandida (desktop). */
  onAlternar?: () => void;
}

export function Sidebar({ aberta = false, colapsada = false, onAlternar }: SidebarProps) {
  const { hasPermission } = useAuth();

  const itensVisiveis = moduleNavItems.filter(
    (item) =>
      !item.permissions ||
      item.permissions.length === 0 ||
      item.permissions.some((p) => hasPermission(p)),
  );

  const classes = ['tg-sidebar'];
  if (aberta) classes.push('aberta');
  if (colapsada) classes.push('colapsada');

  return (
    <nav className={classes.join(' ')} aria-label="Módulos">
      {/* Título visual decorativo (duplica o nome do landmark <nav aria-label>):
          aria-hidden sempre, para leitores não ouvirem "Módulos" duas vezes. No
          rail colapsado ele some por CSS (não fica truncado como "MÓDULO"). */}
      <p className="tg-sidebar-title" aria-hidden="true">
        Módulos
      </p>
      <ul className="tg-nav">
        {itensVisiveis.map((item) => (
          <li key={item.path}>
            <NavLink
              to={item.path}
              className={({ isActive }) => (isActive ? 'active' : undefined)}
              // No rail, o texto fica visualmente oculto; o nome do módulo é
              // exposto por tooltip nativo (title) e por aria-label (leitores).
              title={colapsada ? item.label : undefined}
              aria-label={colapsada ? item.label : undefined}
            >
              <i className={`tg-ico ${item.icon ?? 'fas fa-folder'}`} aria-hidden="true" />
              <span className="tg-nav-label">{item.label}</span>
            </NavLink>
          </li>
        ))}
      </ul>

      {onAlternar && (
        <div className="tg-sidebar-footer">
          <button
            type="button"
            className="tg-sidebar-toggle"
            onClick={onAlternar}
            aria-expanded={!colapsada}
            aria-label={colapsada ? 'Expandir menu de módulos' : 'Recolher menu de módulos'}
            title={colapsada ? 'Expandir menu' : 'Recolher menu'}
          >
            <i
              className={`fas ${colapsada ? 'fa-angles-right' : 'fa-angles-left'}`}
              aria-hidden="true"
            />
            <span className="tg-nav-label">Recolher menu</span>
          </button>
        </div>
      )}
    </nav>
  );
}
