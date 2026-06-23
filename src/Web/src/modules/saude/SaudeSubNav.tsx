// Sub-navegação interna do módulo Saúde (entre Pacientes, Regulação e o Painel
// Fiscal). Mantém UMA entrada na Sidebar e navega por aqui com NavLink acessível
// (aria-current via isActive), com aparência de "tag" (padrão de TributosSubNav).
import { NavLink } from 'react-router-dom';

interface Aba {
  to: string;
  label: string;
  /** Marca a aba ativa também na rota índice (Pacientes). */
  end?: boolean;
}

const ABAS: Aba[] = [
  { to: '/saude', label: 'Pacientes', end: true },
  { to: '/saude/regulacao', label: 'Regulação' },
  { to: '/saude/fiscal', label: 'Painel Fiscal' },
];

export function SaudeSubNav() {
  return (
    <nav className="mb-4" aria-label="Seções de Saúde">
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
