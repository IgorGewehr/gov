// Sub-navegação interna do módulo Tributos (entre Dívida Ativa, Cadastro
// Imobiliário e os Parâmetros do IPTU). Mantém UMA entrada na Sidebar e navega
// por aqui com NavLink acessível (aria-current via isActive), aparência de "tag".
import { NavLink } from 'react-router-dom';

interface Aba {
  to: string;
  label: string;
  /** Marca a aba ativa também na rota índice (Dívida Ativa). */
  end?: boolean;
}

const ABAS: Aba[] = [
  { to: '/tributos', label: 'Dívida Ativa', end: true },
  { to: '/tributos/imoveis', label: 'Imóveis' },
  { to: '/tributos/iptu/parametros', label: 'Parâmetros do IPTU' },
  { to: '/tributos/iss', label: 'ISS' },
  { to: '/tributos/itbi', label: 'ITBI' },
  { to: '/tributos/taxas', label: 'Taxas' },
  { to: '/tributos/cosip', label: 'COSIP' },
  { to: '/tributos/alvaras', label: 'Alvarás' },
  { to: '/tributos/melhoria', label: 'Contribuição de Melhoria' },
];

export function TributosSubNav() {
  return (
    <nav className="mb-4" aria-label="Seções de Tributos">
      <ul className="d-flex flex-wrap list-style-none p-0 m-0" style={{ gap: '0.5rem' }}>
        {ABAS.map((aba) => (
          <li key={aba.to}>
            <NavLink
              to={aba.to}
              end={aba.end}
              className={({ isActive }) => `br-button small ${isActive ? 'primary' : 'secondary'}`}
            >
              {aba.label}
            </NavLink>
          </li>
        ))}
      </ul>
    </nav>
  );
}
