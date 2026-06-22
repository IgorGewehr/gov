// Navegacao interna (secoes) do modulo Legislativo. A Sidebar expoe um unico
// item por modulo; este sub-nav torna as demais telas alcancaveis a partir da
// raiz. Landmark <nav> com aria-current (NavLink) para acessibilidade.
import { NavLink } from 'react-router-dom';

interface Secao {
  path: string;
  label: string;
  icon: string;
  /** Raiz do modulo usa `end` para nao ficar ativa nas demais rotas. */
  end?: boolean;
}

const SECOES: ReadonlyArray<Secao> = [
  { path: '/legislativo', label: 'Proposições', icon: 'fas fa-folder-open', end: true },
  { path: '/legislativo/sessoes', label: 'Sessões', icon: 'fas fa-calendar-day' },
  { path: '/legislativo/votacoes', label: 'Votações', icon: 'fas fa-square-poll-vertical' },
  { path: '/legislativo/painel', label: 'Painel ao vivo', icon: 'fas fa-tower-broadcast' },
  { path: '/legislativo/vereadores', label: 'Vereadores', icon: 'fas fa-users' },
  { path: '/legislativo/ata', label: 'Ata', icon: 'fas fa-file-lines' },
];

export function LegislativoSecoesNav() {
  return (
    <nav className="mb-4" aria-label="Seções do Legislativo">
      <ul className="d-flex flex-wrap" style={{ gap: '0.5rem', listStyle: 'none', padding: 0 }}>
        {SECOES.map((s) => (
          <li key={s.path}>
            <NavLink
              to={s.path}
              end={s.end}
              className={({ isActive }) =>
                `br-button small ${isActive ? 'primary' : 'secondary'}`
              }
            >
              <i className={s.icon} aria-hidden="true" /> {s.label}
            </NavLink>
          </li>
        ))}
      </ul>
    </nav>
  );
}
