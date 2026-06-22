// Landing do módulo de Administração do Sistema (/admin). Apresenta cards de
// atalho para cada área administrativa. Cada card é exibido SOMENTE quando o
// usuário possui a permissão correspondente (gating via <Can>), espelhando o
// "negar por padrão" do RBAC do backend (CLAUDE.md §6).
import { Link } from 'react-router-dom';
import { Card, PageHeader } from '../../components/ui';
import { Can } from '../../auth/Can';
import {
  PERM_USUARIOS_GERENCIAR,
  PERM_MODULOS_CONFIGURAR,
  PERM_AUDITORIA_VER,
} from './admin.permissoes';

interface AdminArea {
  to: string;
  label: string;
  description: string;
  icon: string;
  permission: string;
}

const AREAS: AdminArea[] = [
  {
    to: '/admin/usuarios',
    label: 'Usuários',
    description: 'Cadastro de servidores, atribuição de papéis e ativação de contas.',
    icon: 'fas fa-users',
    permission: PERM_USUARIOS_GERENCIAR,
  },
  {
    to: '/admin/unidades',
    label: 'Estrutura Organizacional',
    description: 'Hierarquia de unidades organizacionais (secretarias, departamentos, setores) do órgão.',
    icon: 'fas fa-sitemap',
    permission: PERM_USUARIOS_GERENCIAR,
  },
  {
    to: '/admin/papeis',
    label: 'Papéis e permissões',
    description: 'Defina papéis (RBAC) e as permissões associadas a cada um.',
    icon: 'fas fa-user-shield',
    permission: PERM_USUARIOS_GERENCIAR,
  },
  {
    to: '/admin/modulos',
    label: 'Módulos do órgão',
    description: 'Ative ou desative os módulos licenciados para este órgão.',
    icon: 'fas fa-toggle-on',
    permission: PERM_MODULOS_CONFIGURAR,
  },
  {
    to: '/admin/auditoria',
    label: 'Trilha de auditoria',
    description: 'Consulte o histórico imutável de alterações (quem, quando, o quê).',
    icon: 'fas fa-clipboard-list',
    permission: PERM_AUDITORIA_VER,
  },
];

export function AdminLandingPage() {
  return (
    <>
      <PageHeader
        title="Administração do Sistema"
        description="Gestão de usuários, papéis, módulos e auditoria do órgão."
      />
      <div className="row">
        {AREAS.map((area) => (
          <Can key={area.to} permission={area.permission}>
            <div className="col-sm-6 col-lg-4 mb-3">
              <Link to={area.to} className="d-block text-decoration-none">
                <Card>
                  <span className="text-up-01 text-semi-bold d-block mb-1">
                    <i className={`${area.icon} mr-2`} aria-hidden="true" />
                    {area.label}
                  </span>
                  <span className="text-base">{area.description}</span>
                </Card>
              </Link>
            </div>
          </Can>
        ))}
      </div>
    </>
  );
}
