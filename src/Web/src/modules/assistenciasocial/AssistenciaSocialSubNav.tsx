// Sub-navegacao interna do modulo AssistenciaSocial (entre os agregados Familia/CadUnico,
// Beneficio e Prontuario SUAS). Usa NavLink acessivel (aria-current) com aparencia de
// "tag/button" do gov.br DS. Mantem o modulo com UMA entrada na Sidebar, navegando entre
// seus contextos por aqui. Mesmo padrao de RhSubNav (modulo RecursosHumanos).
import { NavLink } from 'react-router-dom';

interface Aba {
  to: string;
  label: string;
  /** Marca a aba ativa tambem na rota indice (Familias). */
  end?: boolean;
}

const ABAS: Aba[] = [
  { to: '/assistenciasocial', label: 'Famílias / CadÚnico', end: true },
  { to: '/assistenciasocial/beneficios', label: 'Benefícios' },
  { to: '/assistenciasocial/beneficios/concessoes', label: 'Concessões por competência' },
  { to: '/assistenciasocial/prontuarios', label: 'Prontuário SUAS' },
];

export function AssistenciaSocialSubNav() {
  return (
    <nav className="mb-4" aria-label="Seções de Assistência Social">
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
