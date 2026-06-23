// Tela de FILA das REQUISIÇÕES de almoxarifado (Onda 3b). Lista paginada de pedidos
// por situação/setor/UO (GET /patrimonio/requisicoes) com link para o detalhe, e a
// ação de abrir um novo pedido multi-item ([Command AbrirPedido] via RequisicaoAbrirModal).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  FormRow,
  Input,
  PageHeader,
  Select,
  Tag,
  Toolbar,
  errorMessage,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { formatarData } from '../../../i18n/format';
import { usePedidosLista } from './requisicao.api';
import type { PedidoItemLista, SituacaoPedidoValor } from './requisicao.api';
import { OPCOES_SITUACAO_PEDIDO, situacaoPedidoTagVariant } from './requisicao.helpers';
import { RequisicaoAbrirModal } from './RequisicaoAbrirModal';
import { PatrimonioSubNav } from '../PatrimonioSubNav';
import { Paginacao } from '../shared/Paginacao';
import { TAMANHO_PAGINA_PADRAO } from '../shared/paginacaoTipos';

export function RequisicaoListPage() {
  const navigate = useNavigate();
  const [abrirAberto, setAbrirAberto] = useState(false);

  const [setorInput, setSetorInput] = useState('');
  const [setor, setSetor] = useState('');
  const [status, setStatus] = useState('');
  const [pagina, setPagina] = useState(1);

  const query = usePedidosLista({
    status: status === '' ? undefined : (Number(status) as SituacaoPedidoValor),
    setor: setor || undefined,
    pagina,
    tamanho: TAMANHO_PAGINA_PADRAO,
  });

  function buscar(event: FormEvent): void {
    event.preventDefault();
    setSetor(setorInput.trim());
    setPagina(1);
  }

  const columns: Column<PedidoItemLista>[] = [
    {
      key: 'data',
      header: 'Data',
      sortAccessor: (p) => p.data,
      render: (p) => formatarData(p.data),
    },
    {
      key: 'setor',
      header: 'Setor solicitante',
      sortAccessor: (p) => p.setorSolicitante,
      render: (p) => p.setorSolicitante,
    },
    {
      key: 'totalItens',
      header: 'Itens',
      align: 'end',
      sortAccessor: (p) => p.totalItens,
      render: (p) => p.totalItens,
    },
    {
      key: 'situacao',
      header: 'Situação',
      render: (p) => <Tag variant={situacaoPedidoTagVariant(p.situacao)}>{p.situacao}</Tag>,
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (p) => (
        <Link className="br-button secondary small" to={`/patrimonio/requisicoes/${p.id}`}>
          Detalhes
        </Link>
      ),
    },
  ];

  return (
    <>
      <PatrimonioSubNav />
      <PageHeader
        eyebrow="Patrimônio"
        title="Requisições de almoxarifado"
        description="Fila de pedidos por setor: solicitação, aprovação e atendimento (baixa de estoque)."
        actions={
          <Can permission="patrimonio.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setAbrirAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Abrir requisição
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <Card className="mb-4" header={<strong>Filtrar fila</strong>}>
        <form className="br-form mb-3" onSubmit={buscar}>
          <FormRow
            acao={
              <Button variant="primary" type="submit" loading={query.isFetching}>
                <i className="fas fa-magnifying-glass" aria-hidden="true" /> Filtrar
              </Button>
            }
          >
            <div className="row">
              <div className="col-sm-8">
                <FormField label="Setor solicitante">
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      aria-describedby={describedBy}
                      value={setorInput}
                      onChange={(e) => setSetorInput(e.target.value)}
                      placeholder="Trecho do nome do setor"
                    />
                  )}
                </FormField>
              </div>
              <div className="col-sm-4">
                <FormField label="Situação">
                  {({ id, describedBy }) => (
                    <Select
                      id={id}
                      aria-describedby={describedBy}
                      options={OPCOES_SITUACAO_PEDIDO}
                      placeholder="Todas"
                      value={status}
                      onChange={(e) => {
                        setStatus(e.target.value);
                        setPagina(1);
                      }}
                    />
                  )}
                </FormField>
              </div>
            </div>
          </FormRow>
        </form>

        <DataTable
          caption="Pedidos de requisição de almoxarifado"
          columns={columns}
          rows={query.data?.itens}
          rowKey={(p) => p.id}
          loading={query.isLoading}
          error={query.isError ? errorMessage(query.error) : null}
          empty={
            <EmptyState
              icon="fas fa-clipboard-list"
              title="Nenhuma requisição encontrada"
              description="Ajuste os filtros ou abra uma nova requisição de almoxarifado."
            />
          }
        />

        {query.data && query.data.total > 0 && (
          <div className="mt-3">
            <Paginacao
              pagina={query.data.pagina}
              tamanho={query.data.tamanho}
              total={query.data.total}
              onPaginaChange={setPagina}
              carregando={query.isFetching}
            />
          </div>
        )}
      </Card>

      <RequisicaoAbrirModal
        open={abrirAberto}
        onClose={() => setAbrirAberto(false)}
        onAberto={(id) => navigate(`/patrimonio/requisicoes/${id}`)}
      />
    </>
  );
}
