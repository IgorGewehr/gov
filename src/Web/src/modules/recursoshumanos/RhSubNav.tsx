// Sub-navegação interna do módulo RecursosHumanos (entre os agregados Servidor, Cargo e
// Folha). Usa NavLink acessível (aria-current) com aparência de "tag" do gov.br DS.
// Mantém o módulo com UMA entrada na Sidebar, navegando entre seus contextos por aqui.
import { NavLink } from 'react-router-dom';
import { useHasPermission } from '../../auth/Can';
import { PERM_RH_AUTOSSERVICO } from './recursosHumanos.helpers';

interface Aba {
  to: string;
  label: string;
  /** Marca a aba ativa também na rota índice (Servidores). */
  end?: boolean;
  /** Quando presente, a aba só aparece se o usuário tiver esta permissão. */
  perm?: string;
}

const ABAS: Aba[] = [
  { to: '/recursoshumanos', label: 'Servidores', end: true },
  { to: '/recursoshumanos/cargos', label: 'Cargos' },
  { to: '/recursoshumanos/rubricas', label: 'Rubricas' },
  { to: '/recursoshumanos/tabelas-legais', label: 'Tabelas Legais' },
  { to: '/recursoshumanos/folhas', label: 'Folha de Pagamento' },
  { to: '/recursoshumanos/ciclo-anual', label: 'Ciclo Anual' },
  { to: '/recursoshumanos/ponto', label: 'Ponto' },
  { to: '/recursoshumanos/esocial', label: 'eSocial' },
  { to: '/recursoshumanos/minha-folha', label: 'Minha Folha', perm: PERM_RH_AUTOSSERVICO },
];

export function RhSubNav() {
  const podeAutosservico = useHasPermission(PERM_RH_AUTOSSERVICO);
  const abasVisiveis = ABAS.filter((aba) => !aba.perm || podeAutosservico);
  return (
    <nav className="mb-4" aria-label="Seções de Recursos Humanos">
      <ul className="d-flex flex-wrap list-style-none p-0 m-0" style={{ gap: '0.5rem' }}>
        {abasVisiveis.map((aba) => (
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
