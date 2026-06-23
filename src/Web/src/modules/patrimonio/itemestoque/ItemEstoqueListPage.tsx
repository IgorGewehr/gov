// Tela de LISTA/CONSULTA do Almoxarifado (ItemEstoque). Reúne as consultas de listagem:
//   - [Query ListarItensAbaixoDoPontoPedido §6.2] itens a repor (DataTable, ordenação);
//   - [Query ObterPosicaoCurvaAbc §6.4] posição agregada por classe A/B/C (DataTable);
//   - acesso ao detalhe por identificador ([Query ObterItemEstoque §6.1] na DetailPage);
//   - ação de cadastro ([Command CadastrarItemEstoque §5.1] via FormModal).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { Link } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  FormRow,
  Input,
  PageHeader,
  Tag,
  Toolbar,
  errorMessage,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { formatarMoeda } from '../../../i18n/format';
import {
  useItensReposicao,
  usePosicaoCurvaAbc,
} from './itemestoque.api';
import type { ItemReposicaoResumo, PosicaoAbcResumo } from './itemestoque.api';
import { classificacaoAbcLabel, classificacaoAbcTagVariant } from './itemEstoque.helpers';
import { ItemEstoqueFormModal } from './ItemEstoqueFormModal';
import { PatrimonioSubNav } from '../PatrimonioSubNav';

export function ItemEstoqueListPage() {
  const navigate = useNavigate();
  const [formAberto, setFormAberto] = useState(false);
  const [buscaId, setBuscaId] = useState('');

  const reposicao = useItensReposicao();
  const curvaAbc = usePosicaoCurvaAbc();

  function consultarItem(event: FormEvent): void {
    event.preventDefault();
    const id = buscaId.trim();
    if (id !== '') navigate(`/patrimonio/estoque/itens/${id}`);
  }

  const colunasReposicao: Column<ItemReposicaoResumo>[] = [
    { key: 'codigo', header: 'Código', sortAccessor: (i) => i.codigo, render: (i) => i.codigo },
    { key: 'descricao', header: 'Descrição', sortAccessor: (i) => i.descricao, render: (i) => i.descricao },
    {
      key: 'saldo',
      header: 'Saldo',
      align: 'end',
      sortAccessor: (i) => i.saldo,
      render: (i) => i.saldo,
    },
    {
      key: 'pontoPedido',
      header: 'Ponto de pedido',
      align: 'end',
      sortAccessor: (i) => i.pontoPedido,
      render: (i) => i.pontoPedido,
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (i) => (
        <Link className="br-button secondary small" to={`/patrimonio/estoque/itens/${i.id}`}>
          Detalhes
        </Link>
      ),
    },
  ];

  const colunasAbc: Column<PosicaoAbcResumo>[] = [
    {
      key: 'classe',
      header: 'Classe',
      sortAccessor: (p) => p.classe,
      render: (p) => (
        <Tag variant={classificacaoAbcTagVariant(p.classe)}>{classificacaoAbcLabel(p.classe)}</Tag>
      ),
    },
    {
      key: 'quantidadeItens',
      header: 'Itens',
      align: 'end',
      sortAccessor: (p) => p.quantidadeItens,
      render: (p) => p.quantidadeItens,
    },
    {
      key: 'valorTotal',
      header: 'Valor total estocado',
      align: 'end',
      sortAccessor: (p) => p.valorTotal,
      render: (p) => formatarMoeda(p.valorTotal),
    },
  ];

  return (
    <>
      <PatrimonioSubNav />
      <PageHeader
        eyebrow="Patrimônio"
        title="Almoxarifado"
        description="Controle de itens de consumo: reposição, curva ABC e movimentos."
        actions={
          <Can permission="patrimonio.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Cadastrar item
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <Card className="mb-4" header={<strong>Consultar item por identificador</strong>}>
        <form className="br-form" onSubmit={consultarItem}>
          <FormRow
            acao={
              <Button variant="secondary" type="submit" disabled={buscaId.trim() === ''}>
                <i className="fas fa-magnifying-glass" aria-hidden="true" /> Abrir detalhe
              </Button>
            }
          >
            <FormField label="Identificador do item">
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={buscaId}
                  onChange={(e) => setBuscaId(e.target.value)}
                  placeholder="00000000-0000-0000-0000-000000000000"
                />
              )}
            </FormField>
          </FormRow>
        </form>
      </Card>

      <Card
        className="mb-4"
        header={
          <div className="d-flex justify-content-between align-items-center">
            <strong>Itens em ponto de pedido (reposição)</strong>
            <Button
              variant="tertiary"
              onClick={() => reposicao.refetch()}
              loading={reposicao.isFetching}
              aria-label="Atualizar lista de reposição"
            >
              <i className="fas fa-rotate-right" aria-hidden="true" /> Atualizar
            </Button>
          </div>
        }
      >
        <DataTable
          caption="Itens ativos com saldo abaixo ou igual ao ponto de pedido"
          columns={colunasReposicao}
          rows={reposicao.data}
          rowKey={(i) => i.id}
          loading={reposicao.isLoading}
          error={reposicao.isError ? errorMessage(reposicao.error) : null}
          empty={
            <EmptyState
              icon="fas fa-circle-check"
              title="Nenhum item para repor"
              description="Todos os itens ativos estão acima do ponto de pedido."
            />
          }
        />
      </Card>

      <Card
        header={
          <div className="d-flex justify-content-between align-items-center">
            <strong>Posição da Curva ABC</strong>
            <Button
              variant="tertiary"
              onClick={() => curvaAbc.refetch()}
              loading={curvaAbc.isFetching}
              aria-label="Atualizar posição da curva ABC"
            >
              <i className="fas fa-rotate-right" aria-hidden="true" /> Atualizar
            </Button>
          </div>
        }
      >
        <DataTable
          caption="Posição agregada dos itens ativos por classe da Curva ABC"
          columns={colunasAbc}
          rows={curvaAbc.data}
          rowKey={(p) => p.classe}
          loading={curvaAbc.isLoading}
          error={curvaAbc.isError ? errorMessage(curvaAbc.error) : null}
          empty={
            <EmptyState
              icon="fas fa-chart-pie"
              title="Sem itens classificados"
              description="Cadastre itens para visualizar a posição da Curva ABC."
            />
          }
        />
      </Card>

      <ItemEstoqueFormModal
        open={formAberto}
        onClose={() => setFormAberto(false)}
        onCadastrado={(id) => navigate(`/patrimonio/estoque/itens/${id}`)}
      />
    </>
  );
}
