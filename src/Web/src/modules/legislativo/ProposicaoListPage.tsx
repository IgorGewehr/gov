// Tela de LISTA de Proposicoes por situacao. Padrao-ouro: filtro por situacao,
// DataTable com estados loading/vazio/erro + ordenacao, link para detalhe e
// abertura do formulario de apresentacao (mutation).
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { LegislativoSecoesNav } from './LegislativoSecoesNav';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  PageHeader,
  Select,
  Tag,
  Toolbar,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { formatarData } from '../../i18n/format';
import { Can } from '../../auth/Can';
import { SITUACOES_PROPOSICAO, useProposicoesPorSituacao } from './api';
import type { ProposicaoResumo } from './api';
import { situacaoProposicaoTagVariant } from './legislativo.helpers';
import { ProposicaoFormModal } from './ProposicaoFormModal';

export function ProposicaoListPage() {
  const [situacao, setSituacao] = useState<number>(SITUACOES_PROPOSICAO[0].value);
  const [formAberto, setFormAberto] = useState(false);

  const query = useProposicoesPorSituacao(situacao);

  const columns: Column<ProposicaoResumo>[] = [
    {
      key: 'tipo',
      header: 'Tipo',
      sortAccessor: (p) => p.tipo,
      render: (p) => p.tipo,
    },
    {
      key: 'ementa',
      header: 'Ementa',
      render: (p) => p.ementa,
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (p) => p.situacao,
      render: (p) => <Tag variant={situacaoProposicaoTagVariant(p.situacao)}>{p.situacao}</Tag>,
    },
    {
      key: 'apresentacao',
      header: 'Apresentação',
      sortAccessor: (p) => p.dataApresentacao,
      render: (p) => formatarData(p.dataApresentacao),
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (p) => (
        <Link className="br-button tertiary small" to={`/legislativo/proposicoes/${p.id}`}>
          Detalhes
        </Link>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        eyebrow="Legislativo"
        title="Proposições"
        description="Acompanhe as matérias submetidas à apreciação do Plenário por situação."
        actions={
          <Can permission="legislativo.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Apresentar proposição
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <LegislativoSecoesNav />

      <Card className="mb-4">
        <form className="br-form" onSubmit={(e) => e.preventDefault()}>
          <div className="row align-items-end">
            <div className="col-sm-6 col-md-4">
              <FormField label="Situação">
                {({ id, describedBy }) => (
                  <Select
                    id={id}
                    aria-describedby={describedBy}
                    value={String(situacao)}
                    onChange={(e) => setSituacao(Number(e.target.value))}
                    options={SITUACOES_PROPOSICAO.map((s) => ({ value: String(s.value), label: s.label }))}
                  />
                )}
              </FormField>
            </div>
          </div>
        </form>
      </Card>

      <DataTable
        caption={`Proposições na situação ${SITUACOES_PROPOSICAO.find((s) => s.value === situacao)?.label ?? ''}`}
        columns={columns}
        rows={query.data}
        rowKey={(p) => p.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-folder-open"
            title="Nenhuma proposição encontrada"
            description="Não há matérias nesta situação para o tenant atual."
          />
        }
      />

      <ProposicaoFormModal open={formAberto} onClose={() => setFormAberto(false)} />
    </>
  );
}
