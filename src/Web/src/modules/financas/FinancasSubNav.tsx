// Sub-navegação interna do módulo Finanças (entre os agregados do ciclo da despesa:
// Dotações, Empenhos, Liquidações, Pagamentos e Restos a Pagar). Mantém UMA entrada na
// Sidebar e navega por aqui com NavLink acessível (aria-current), aparência de "tag".
import { NavLink } from 'react-router-dom';

interface Aba {
  to: string;
  label: string;
  /** Marca a aba ativa também na rota índice (Dotações). */
  end?: boolean;
}

const ABAS: Aba[] = [
  { to: '/financas', label: 'Dotações', end: true },
  { to: '/financas/empenhos', label: 'Empenhos' },
  { to: '/financas/liquidacoes', label: 'Liquidações' },
  { to: '/financas/pagamentos', label: 'Pagamentos' },
  { to: '/financas/restos-a-pagar', label: 'Restos a Pagar' },
  { to: '/financas/contabilidade/plano-de-contas', label: 'Plano de Contas' },
  { to: '/financas/contabilidade/balancete', label: 'Balancete' },
  { to: '/financas/contabilidade/lancamentos', label: 'Lançamentos' },
  { to: '/financas/contabilidade/demonstracoes', label: 'Demonstrações' },
  { to: '/financas/contabilidade/msc', label: 'MSC' },
];

export function FinancasSubNav() {
  return (
    <nav className="mb-4" aria-label="Seções de Finanças">
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
