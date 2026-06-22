// Topbar institucional (landmark role=banner): marca Tensorroot.Gov, identificação
// do usuário/tenant e ação de sair. Em telas pequenas, botão para abrir a sidebar.
import { useAuth } from '../../auth/useAuth';

interface HeaderProps {
  /** Alterna a sidebar off-canvas (mobile). */
  onToggleMenu?: () => void;
}

export function Header({ onToggleMenu }: HeaderProps) {
  const { user, isAuthenticated, logout } = useAuth();

  return (
    <header className="tg-topbar" role="banner">
      <div className="tg-brand">
        {isAuthenticated && (
          <button
            type="button"
            className="tg-menu-toggle"
            aria-label="Abrir menu de módulos"
            onClick={onToggleMenu}
          >
            <i className="fas fa-bars" aria-hidden="true" />
          </button>
        )}
        <span className="tg-brand-mark" aria-hidden="true">
          TG
        </span>
        <span className="tg-brand-text">
          <span className="tg-brand-name">Tensorroot.Gov</span>
          <span className="tg-brand-sub">Gestão pública integrada</span>
        </span>
      </div>

      {isAuthenticated && user && (
        <div className="tg-topbar-user">
          <span className="tg-user-info">
            <span className="tg-user-name">{user.nome}</span>
            {user.tenantNome && <span className="tg-user-tenant">{user.tenantNome}</span>}
          </span>
          <button type="button" className="tg-sair" onClick={logout}>
            <i className="fas fa-arrow-right-from-bracket" aria-hidden="true" /> Sair
          </button>
        </div>
      )}
    </header>
  );
}
