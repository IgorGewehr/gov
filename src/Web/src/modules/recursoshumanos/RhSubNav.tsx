// Sub-navegação interna do módulo RecursosHumanos (entre os agregados Servidor, Cargo e
// Folha). Usa NavLink acessível (aria-current) com aparência de "tag" do gov.br DS.
// Mantém o módulo com UMA entrada na Sidebar, navegando entre seus contextos por aqui.
import { NavLink } from 'react-router-dom';

interface Aba {
  to: string;
  label: string;
  /** Marca a aba ativa também na rota índice (Servidores). */
  end?: boolean;
}

const ABAS: Aba[] = [
  { to: '/recursoshumanos', label: 'Servidores', end: true },
  { to: '/recursoshumanos/cargos', label: 'Cargos' },
  { to: '/recursoshumanos/folhas', label: 'Folha de Pagamento' },
];

export function RhSubNav() {
  return (
    <nav className="mb-4" aria-label="Seções de Recursos Humanos">
      <ul className="d-flex flex-wrap list-style-none p-0 m-0" style={{ gap: '0.5rem' }}>
        {ABAS.map((aba) => (
          <li key={aba.to}>
            <NavLink
              to={aba.to}
              end={aba.end}
              className={({ isActive }) =>
                `br-button small ${isActive ? 'primary' : 'secondary'}`
              }
            >
              {aba.label}
            </NavLink>
          </li>
        ))}
      </ul>
    </nav>
  );
}
