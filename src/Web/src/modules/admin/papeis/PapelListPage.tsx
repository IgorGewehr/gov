// Tela de gestão de PAPÉIS (RBAC) do módulo Administração do Sistema.
// LISTA os papéis (nome + nº de permissões), permite criar um novo papel
// (PapelFormModal) e editar as permissões de um papel existente
// (PapelPermissoesModal). Todas as ações são gated por "identidade.usuarios.gerenciar".
import { useState } from 'react';
import { Button, DataTable, EmptyState, PageHeader, Tag } from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { errorMessage } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { PERM_USUARIOS_GERENCIAR } from '../admin.permissoes';
import { usePapeis } from './papel.api';
import type { Papel } from './papel.api';
import { rotuloQuantidadePermissoes } from './papel.helpers';
import { PapelFormModal } from './PapelFormModal';
import { PapelPermissoesModal } from './PapelPermissoesModal';

export function PapelListPage() {
  const query = usePapeis();
  const [formAberto, setFormAberto] = useState(false);
  const [papelEmEdicao, setPapelEmEdicao] = useState<Papel | null>(null);

  const columns: Column<Papel>[] = [
    {
      key: 'nome',
      header: 'Papel',
      sortAccessor: (p) => p.nome,
      render: (p) => p.nome,
    },
    {
      key: 'permissoes',
      header: 'Permissões',
      sortAccessor: (p) => p.permissoes.length,
      render: (p) => (
        <Tag variant={p.permissoes.length > 0 ? 'info' : 'default'}>
          {rotuloQuantidadePermissoes(p.permissoes.length)}
        </Tag>
      ),
    },
    {
      key: 'acoes',
      header: 'Ações',
      align: 'end',
      render: (p) => (
        <Can permission={PERM_USUARIOS_GERENCIAR}>
          <Button variant="tertiary" onClick={() => setPapelEmEdicao(p)}>
            <i className="fas fa-key" aria-hidden="true" /> Editar permissões
          </Button>
        </Can>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Papéis e permissões"
        description="Defina papéis (RBAC) e as permissões que cada um concede no órgão."
        actions={
          <Can permission={PERM_USUARIOS_GERENCIAR}>
            <Button variant="primary" onClick={() => setFormAberto(true)}>
              <i className="fas fa-plus" aria-hidden="true" /> Criar papel
            </Button>
          </Can>
        }
      />

      <DataTable
        caption="Papéis cadastrados no órgão"
        columns={columns}
        rows={query.data}
        rowKey={(p) => p.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-user-shield"
            title="Nenhum papel cadastrado"
            description="Crie o primeiro papel para definir as permissões dos usuários."
          />
        }
      />

      <PapelFormModal open={formAberto} onClose={() => setFormAberto(false)} />

      <PapelPermissoesModal
        open={papelEmEdicao !== null}
        onClose={() => setPapelEmEdicao(null)}
        papel={papelEmEdicao}
      />
    </>
  );
}
