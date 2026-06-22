// Tela de DETALHE de uma Comissao: cabecalho (tipo/finalidade) + COMPOSICAO
// (membros vereadores e seus cargos), com acao de adicionar membro gated por
// `legislativo.comissoes.gerenciar`.
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Button, Card, DataTable, EmptyState, PageHeader, QueryState, Tag } from '../../components/ui';
import type { Column } from '../../components/ui';
import { Can } from '../../auth/Can';
import { useComissao } from './comissoes.api';
import type { ComissaoDetalhe, MembroComissaoResumo } from './comissoes.api';
import { MembroComissaoModal } from './MembroComissaoModal';

const COLUNAS_MEMBRO: Column<MembroComissaoResumo>[] = [
  { key: 'nome', header: 'Vereador', sortAccessor: (m) => m.vereadorNome, render: (m) => m.vereadorNome },
  {
    key: 'cargo',
    header: 'Cargo',
    sortAccessor: (m) => m.cargo,
    render: (m) => <Tag variant="info">{m.cargo}</Tag>,
  },
];

export function ComissaoDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const query = useComissao(id);
  const [membroAberto, setMembroAberto] = useState(false);

  return (
    <>
      <PageHeader
        title="Composição da Comissão"
        actions={
          <Link className="br-button secondary" to="/legislativo/comissoes">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<ComissaoDetalhe>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
      >
        {(comissao) => (
          <>
            <Card className="mb-4" header={<strong>{comissao.nome}</strong>}>
              <dl className="row">
                <div className="col-sm-4 mb-3">
                  <dt className="text-gray-60 text-down-01">Tipo</dt>
                  <dd className="mb-0">
                    <Tag variant="info">{comissao.tipo}</Tag>
                  </dd>
                </div>
                <div className="col-12">
                  <dt className="text-gray-60 text-down-01">Finalidade</dt>
                  <dd className="mb-0">{comissao.finalidade}</dd>
                </div>
              </dl>
            </Card>

            <div className="d-flex justify-content-between align-items-center mb-3">
              <h2 className="text-up-01 mb-0">Membros</h2>
              <Can permission="legislativo.comissoes.gerenciar">
                <Button variant="secondary" onClick={() => setMembroAberto(true)}>
                  <i className="fas fa-user-plus" aria-hidden="true" /> Adicionar membro
                </Button>
              </Can>
            </div>

            <DataTable
              caption={`Composição da comissão ${comissao.nome}`}
              columns={COLUNAS_MEMBRO}
              rows={comissao.membros}
              rowKey={(m) => m.vereadorId}
              empty={
                <EmptyState
                  icon="fas fa-user-group"
                  title="Sem membros"
                  description="Adicione vereadores à composição desta comissão."
                />
              }
            />

            <Can permission="legislativo.comissoes.gerenciar">
              <MembroComissaoModal
                open={membroAberto}
                onClose={() => setMembroAberto(false)}
                comissaoId={comissao.id}
              />
            </Can>
          </>
        )}
      </QueryState>
    </>
  );
}
