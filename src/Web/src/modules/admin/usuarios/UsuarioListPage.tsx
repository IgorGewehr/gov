// Tela de LISTA/GESTÃO de usuários (Identidade / Administração do Sistema).
// DataTable com nome, e-mail, status e papéis; ações de criar/editar/ativar/
// desativar, definir papéis e redefinir senha — TODAS escondidas pelo gating de
// permissão "identidade.usuarios.gerenciar" (<Can>). Segue o PADRÃO-OURO de
// ProcessoListPage (estados loading/erro/vazio, modais controlados).
import { useState } from 'react';
import {
  Button,
  DataTable,
  EmptyState,
  PageHeader,
  Tag,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { errorMessage } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { PERM_USUARIOS_GERENCIAR } from '../admin.permissoes';
import {
  useAtivarUsuario,
  useDesativarUsuario,
  useUsuarios,
} from './usuario.api';
import type { UsuarioResumo } from './usuario.api';
import { ativoLabel, ativoTagVariant } from './usuario.helpers';
import { UsuarioFormModal } from './UsuarioFormModal';
import { UsuarioPapeisModal, UsuarioSenhaModal } from './UsuarioPapeisModal';
import { UsuarioAtribuicoesModal } from './UsuarioAtribuicoesModal';

export function UsuarioListPage() {
  const query = useUsuarios();
  const ativar = useAtivarUsuario();
  const desativar = useDesativarUsuario();

  const [formAberto, setFormAberto] = useState(false);
  const [emEdicao, setEmEdicao] = useState<UsuarioResumo | null>(null);
  const [papeisDe, setPapeisDe] = useState<UsuarioResumo | null>(null);
  const [senhaDe, setSenhaDe] = useState<UsuarioResumo | null>(null);
  const [atribuicoesDe, setAtribuicoesDe] = useState<UsuarioResumo | null>(null);

  function abrirCriacao(): void {
    setEmEdicao(null);
    setFormAberto(true);
  }

  function abrirEdicao(usuario: UsuarioResumo): void {
    setEmEdicao(usuario);
    setFormAberto(true);
  }

  const columns: Column<UsuarioResumo>[] = [
    { key: 'nome', header: 'Nome', sortAccessor: (u) => u.nome, render: (u) => u.nome },
    { key: 'email', header: 'E-mail', sortAccessor: (u) => u.email, render: (u) => u.email },
    {
      key: 'ativo',
      header: 'Status',
      sortAccessor: (u) => (u.ativo ? 1 : 0),
      render: (u) => <Tag variant={ativoTagVariant(u.ativo)}>{ativoLabel(u.ativo)}</Tag>,
    },
    {
      key: 'papeis',
      header: 'Papéis',
      render: (u) =>
        u.papeis.length > 0 ? u.papeis.join(', ') : <span className="text-down-01">—</span>,
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (u) => (
        <Can permission={PERM_USUARIOS_GERENCIAR}>
          <div className="d-flex" style={{ gap: '0.5rem', flexWrap: 'wrap' }}>
            <Button variant="tertiary" className="small" onClick={() => abrirEdicao(u)}>
              Editar
            </Button>
            <Button variant="tertiary" className="small" onClick={() => setPapeisDe(u)}>
              Papéis
            </Button>
            <Button variant="tertiary" className="small" onClick={() => setAtribuicoesDe(u)}>
              Atribuições
            </Button>
            <Button variant="tertiary" className="small" onClick={() => setSenhaDe(u)}>
              Senha
            </Button>
            {u.ativo ? (
              <Button
                variant="tertiary"
                className="small"
                loading={desativar.isPending && desativar.variables === u.id}
                onClick={() => desativar.mutate(u.id)}
              >
                Desativar
              </Button>
            ) : (
              <Button
                variant="tertiary"
                className="small"
                loading={ativar.isPending && ativar.variables === u.id}
                onClick={() => ativar.mutate(u.id)}
              >
                Ativar
              </Button>
            )}
          </div>
        </Can>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Usuários"
        description="Gestão de servidores e contas do órgão (RBAC)."
        actions={
          <Can permission={PERM_USUARIOS_GERENCIAR}>
            <Button variant="primary" onClick={abrirCriacao}>
              <i className="fas fa-plus" aria-hidden="true" /> Novo usuário
            </Button>
          </Can>
        }
      />

      <DataTable
        caption="Usuários do órgão"
        columns={columns}
        rows={query.data}
        rowKey={(u) => u.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-users"
            title="Nenhum usuário cadastrado"
            description="Cadastre o primeiro usuário do órgão para começar."
          />
        }
      />

      <UsuarioFormModal
        open={formAberto}
        onClose={() => setFormAberto(false)}
        usuario={emEdicao}
      />
      <UsuarioPapeisModal
        open={papeisDe !== null}
        onClose={() => setPapeisDe(null)}
        usuario={papeisDe}
      />
      <UsuarioSenhaModal
        open={senhaDe !== null}
        onClose={() => setSenhaDe(null)}
        usuario={senhaDe}
      />
      <UsuarioAtribuicoesModal
        open={atribuicoesDe !== null}
        onClose={() => setAtribuicoesDe(null)}
        usuario={atribuicoesDe}
      />
    </>
  );
}
