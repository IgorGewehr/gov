// Tela da FILA DE REGULAÇÃO (solicitações em análise). Padrão-ouro: filtros por
// procedimento (SIGTAP) e prioridade, DataTable com estados loading/vazio/erro +
// ordenação, link para detalhe e abertura do formulário de solicitação (mutation).
import { useState } from 'react';
import { Link } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  Input,
  PageHeader,
  Select,
  Tag,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarData } from '../../i18n/format';
import { useFilaDeRegulacao } from './api';
import type { Prioridade, SolicitacaoRegulacaoResumo } from './api';
import { opcoesPrioridade, paraPrioridade, prioridadeVariant, situacaoRegulacaoVariant } from './saude.helpers';
import { RegulacaoFormModal } from './RegulacaoFormModal';

export function RegulacaoListPage() {
  const [codigoSigtap, setCodigoSigtap] = useState('');
  const [prioridade, setPrioridade] = useState('');
  const [formAberto, setFormAberto] = useState(false);

  const prioridadeFiltro: Prioridade | undefined = prioridade ? paraPrioridade(prioridade) : undefined;
  const query = useFilaDeRegulacao(codigoSigtap, prioridadeFiltro);

  const columns: Column<SolicitacaoRegulacaoResumo>[] = [
    {
      key: 'prioridade',
      header: 'Prioridade',
      sortAccessor: (s) => s.prioridade,
      render: (s) => <Tag variant={prioridadeVariant(s.prioridade)}>{s.prioridade}</Tag>,
    },
    { key: 'sigtap', header: 'Procedimento (SIGTAP)', render: (s) => s.codigoSigtap },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (s) => s.situacao,
      render: (s) => <Tag variant={situacaoRegulacaoVariant(s.situacao)}>{s.situacao}</Tag>,
    },
    {
      key: 'data',
      header: 'Solicitada em',
      sortAccessor: (s) => s.dataSolicitacao,
      render: (s) => formatarData(s.dataSolicitacao),
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (s) => (
        <Link className="br-button tertiary small" to={`/saude/regulacao/${s.id}`}>
          Detalhes
        </Link>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Fila de regulação"
        description="Solicitações de procedimentos (SIGTAP) aguardando regulação, ordenadas por risco/urgência."
        actions={
          <Can permission="saude.gerenciar">
            <Button variant="primary" onClick={() => setFormAberto(true)}>
              <i className="fas fa-plus" aria-hidden="true" /> Nova solicitação
            </Button>
          </Can>
        }
      />

      <Card className="mb-4">
        <div className="br-form">
          <div className="row align-items-end">
            <div className="col-sm">
              <FormField label="Código SIGTAP" help="Opcional.">
                {({ id, describedBy }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    value={codigoSigtap}
                    onChange={(e) => setCodigoSigtap(e.target.value)}
                    placeholder="Filtrar por procedimento"
                  />
                )}
              </FormField>
            </div>
            <div className="col-sm">
              <FormField label="Prioridade">
                {({ id }) => (
                  <Select
                    id={id}
                    options={opcoesPrioridade}
                    placeholder="Todas"
                    value={prioridade}
                    onChange={(e) => setPrioridade(e.target.value)}
                  />
                )}
              </FormField>
            </div>
          </div>
        </div>
      </Card>

      <DataTable
        caption="Fila de regulação"
        columns={columns}
        rows={query.data}
        rowKey={(s) => s.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-circle-check"
            title="Fila vazia"
            description="Não há solicitações aguardando regulação com os filtros selecionados."
          />
        }
      />

      <RegulacaoFormModal open={formAberto} onClose={() => setFormAberto(false)} />
    </>
  );
}
