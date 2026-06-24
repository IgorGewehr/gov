// Shell da area "Meus dados" do cidadao. Gated: sem sessao de cidadao -> /portal-cidadao
// (login do realm externo), preservando o destino. Header/Footer institucionais + uma
// navegacao propria (NAO a SubNav do admin, que depende do AuthContext do back-office) +
// identificacao do cidadao e sair. NavLink simples (NavLink usa o contexto do router, nao
// o de auth). Cada secao e dado-PROPRIO; aviso LGPD na barra.
import { NavLink, Navigate, Outlet, useLocation } from 'react-router-dom';
import { Button } from '../../components/ui';
import { Footer } from '../../app/shell/Footer';
import { CidadaoTopbar } from './CidadaoTopbar';
import { useCidadaoAuth } from './useCidadaoAuth';
import { formatarDocumento } from './cidadao.helpers';

const SECOES = [
  { to: '/portal-cidadao/meus-debitos', label: 'Meus debitos', icon: 'fas fa-file-invoice-dollar', end: false },
  { to: '/portal-cidadao/minha-divida-ativa', label: 'Minha divida ativa', icon: 'fas fa-balance-scale', end: false },
  { to: '/portal-cidadao/meus-processos', label: 'Meus protocolos', icon: 'fas fa-folder-open', end: false },
];

export function CidadaoLayout() {
  const { cidadao, isAutenticado, sair } = useCidadaoAuth();
  const location = useLocation();

  if (!isAutenticado || !cidadao) {
    return <Navigate to="/portal-cidadao" replace state={{ from: location }} />;
  }

  return (
    <div className="app-shell">
      <a className="br-skip-link" href="#conteudo" accessKey="1">
        Ir para o conteudo
      </a>
      <CidadaoTopbar />
      <div className="tg-content" style={{ maxWidth: '64rem', margin: '0 auto', padding: '1.5rem 1rem' }}>
        <div
          className="d-flex justify-content-between align-items-center mb-3"
          style={{ flexWrap: 'wrap', gap: '0.75rem' }}
        >
          <div>
            <p className="mb-0 text-down-01 text-gray-60">Bem-vindo(a)</p>
            <strong className="text-up-01">{cidadao.nome}</strong>
            <span className="text-gray-60"> &middot; {formatarDocumento(cidadao.documento)}</span>
          </div>
          <Button variant="secondary" size="sm" onClick={sair}>
            <i className="fas fa-sign-out-alt" aria-hidden="true" /> Sair
          </Button>
        </div>

        <nav className="tg-subnav mb-4" aria-label="Minhas informacoes">
          <ul className="tg-subnav-lista">
            {SECOES.map((item) => (
              <li key={item.to}>
                <NavLink
                  to={item.to}
                  end={item.end}
                  className={({ isActive }) => `tg-tab${isActive ? ' active' : ''}`}
                >
                  <i className={item.icon} aria-hidden="true" /> {item.label}
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>

        <main id="conteudo" tabIndex={-1}>
          <Outlet />
        </main>
      </div>
      <Footer />
    </div>
  );
}
