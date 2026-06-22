// Lista de Liquidações de um Empenho (GET /empenhos/{empenhoId}/liquidacoes). Filtro
// pelo identificador do empenho, DataTable, link para detalhe e formulário de liquidação.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  Input,
  PageHeader,
  Tag,
  errorMessage,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarData, formatarMoeda } from '../../i18n/format';
import { useLiquidacoesPorEmpenho } from './financas.api';
import type { LiquidacaoResumo } from './financas.api';
import { situacaoTagVariant } from './financas.helpers';
import { FinancasSubNav } from './FinancasSubNav';
import { LiquidacaoFormModal } from './LiquidacaoFormModal';

export function LiquidacaoListPage() {
  const [empenhoId, setEmpenhoId] = useState('');
  const [consultaAtiva, setConsultaAtiva] = useState('');
  const [formAberto, setFormAberto] = useState(false);

  const query = useLiquidacoesPorEmpenho(consultaAtiva, consultaAtiva.length > 0);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    setConsultaAtiva(empenhoId.trim());
  }

  const columns: Column<LiquidacaoResumo>[] = [
    {
      key: 'documento',
      header: 'Documento',
      sortAccessor: (l) => l.documento,
      render: (l) => <Link to={`/financas/liquidacoes/${l.id}`}>{l.documento}</Link>,
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (l) => l.situacao,
      render: (l) => <Tag variant={situacaoTagVariant(l.situacao)}>{l.situacao}</Tag>,
    },
    {
      key: 'valor',
      header: 'Valor',
      align: 'end',
      sortAccessor: (l) => l.valor,
      render: (l) => formatarMoeda(l.valor),
    },
    {
      key: 'saldo',
      header: 'Saldo a pagar',
      align: 'end',
      sortAccessor: (l) => l.saldoAPagar,
      render: (l) => formatarMoeda(l.saldoAPagar),
    },
    {
      key: 'data',
      header: 'Data',
      sortAccessor: (l) => l.dataLiquidacao,
      render: (l) => formatarData(l.dataLiquidacao),
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (l) => (
        <Link className="br-button tertiary small" to={`/financas/liquidacoes/${l.id}`}>
          Detalhes
        </Link>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Liquidações"
        description="Liquidações de um empenho (2º estágio da despesa, Lei 4.320/64)."
        actions={
          <Can permission="financas.gerenciar">
            <Button variant="primary" onClick={() => setFormAberto(true)}>
              <i className="fas fa-plus" aria-hidden="true" /> Liquidar despesa
            </Button>
          </Can>
        }
      />

      <FinancasSubNav />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <div className="row align-items-end">
            <div className="col">
              <FormField label="Identificador do empenho" required>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={empenhoId}
                    onChange={(e) => setEmpenhoId(e.target.value)}
                    placeholder="00000000-0000-0000-0000-000000000000"
                  />
                )}
              </FormField>
            </div>
            <div className="col-auto mb-3">
              <Button variant="primary" type="submit" disabled={empenhoId.trim() === ''} loading={query.isFetching}>
                Consultar
              </Button>
            </div>
          </div>
        </form>
      </Card>

      {consultaAtiva === '' ? (
        <EmptyState
          icon="fas fa-magnifying-glass"
          title="Faça uma consulta"
          description="Informe o identificador do empenho e clique em Consultar."
        />
      ) : (
        <DataTable
          caption={`Liquidações do empenho ${consultaAtiva}`}
          columns={columns}
          rows={query.data}
          rowKey={(l) => l.id}
          loading={query.isLoading}
          error={query.isError ? errorMessage(query.error) : null}
          empty={
            <EmptyState
              icon="fas fa-folder-open"
              title="Nenhuma liquidação encontrada"
              description="Este empenho não possui liquidações registradas."
            />
          }
        />
      )}

      <LiquidacaoFormModal
        open={formAberto}
        onClose={() => setFormAberto(false)}
        empenhoIdInicial={consultaAtiva}
      />
    </>
  );
}
