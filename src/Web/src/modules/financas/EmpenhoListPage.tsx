// Lista de Empenhos de uma Dotação (GET /dotacoes/{dotacaoId}/empenhos). Filtro pelo
// identificador da dotação, DataTable com estados, link para detalhe e abertura do
// formulário de emissão de empenho (POST /empenhos).
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
import { formatarMoeda } from '../../i18n/format';
import { useEmpenhosPorDotacao } from './financas.api';
import type { EmpenhoResumo } from './financas.api';
import { situacaoTagVariant } from './financas.helpers';
import { FinancasSubNav } from './FinancasSubNav';
import { EmpenhoFormModal } from './EmpenhoFormModal';

export function EmpenhoListPage() {
  const [dotacaoId, setDotacaoId] = useState('');
  const [consultaAtiva, setConsultaAtiva] = useState('');
  const [formAberto, setFormAberto] = useState(false);

  const query = useEmpenhosPorDotacao(consultaAtiva, consultaAtiva.length > 0);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    setConsultaAtiva(dotacaoId.trim());
  }

  const columns: Column<EmpenhoResumo>[] = [
    {
      key: 'numero',
      header: 'Número',
      sortAccessor: (e) => e.numero,
      render: (e) => <Link to={`/financas/empenhos/${e.id}`}>{e.numero}</Link>,
    },
    {
      key: 'credor',
      header: 'Credor',
      sortAccessor: (e) => e.credorNome,
      render: (e) => e.credorNome,
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (e) => e.situacao,
      render: (e) => <Tag variant={situacaoTagVariant(e.situacao)}>{e.situacao}</Tag>,
    },
    {
      key: 'valor',
      header: 'Valor empenhado',
      align: 'end',
      sortAccessor: (e) => e.valorEmpenhado,
      render: (e) => formatarMoeda(e.valorEmpenhado),
    },
    {
      key: 'saldo',
      header: 'Saldo a liquidar',
      align: 'end',
      sortAccessor: (e) => e.saldoALiquidar,
      render: (e) => formatarMoeda(e.saldoALiquidar),
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (e) => (
        <Link className="br-button tertiary small" to={`/financas/empenhos/${e.id}`}>
          Detalhes
        </Link>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Empenhos"
        description="Empenhos de uma dotação (1º estágio da despesa, Lei 4.320/64)."
        actions={
          <Can permission="financas.gerenciar">
            <Button variant="primary" onClick={() => setFormAberto(true)}>
              <i className="fas fa-plus" aria-hidden="true" /> Emitir empenho
            </Button>
          </Can>
        }
      />

      <FinancasSubNav />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <div className="row align-items-end">
            <div className="col">
              <FormField label="Identificador da dotação" required>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={dotacaoId}
                    onChange={(e) => setDotacaoId(e.target.value)}
                    placeholder="00000000-0000-0000-0000-000000000000"
                  />
                )}
              </FormField>
            </div>
            <div className="col-auto mb-3">
              <Button variant="primary" type="submit" disabled={dotacaoId.trim() === ''} loading={query.isFetching}>
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
          description="Informe o identificador da dotação e clique em Consultar."
        />
      ) : (
        <DataTable
          caption={`Empenhos da dotação ${consultaAtiva}`}
          columns={columns}
          rows={query.data}
          rowKey={(e) => e.id}
          loading={query.isLoading}
          error={query.isError ? errorMessage(query.error) : null}
          empty={
            <EmptyState
              icon="fas fa-folder-open"
              title="Nenhum empenho encontrado"
              description="Esta dotação não possui empenhos emitidos."
            />
          }
        />
      )}

      <EmpenhoFormModal
        open={formAberto}
        onClose={() => setFormAberto(false)}
        dotacaoIdInicial={consultaAtiva}
      />
    </>
  );
}
