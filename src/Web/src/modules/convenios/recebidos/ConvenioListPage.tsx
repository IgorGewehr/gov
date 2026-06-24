// Fluxo A — LISTA de convenios federais RECEBIDOS (query ListarConvenios).
// Filtro por situacao, DataTable com semaforo de situacao (Tag) e link para o
// detalhe (ciclo: proposta/celebrar -> execucao -> PC parcial/final -> analise).
import { useState } from 'react';
import { Link } from 'react-router-dom';
import {
  Card,
  DataTable,
  EmptyState,
  FormField,
  PageHeader,
  Select,
  Tag,
  errorMessage,
} from '../../../components/ui';
import type { Column, SelectOption } from '../../../components/ui';
import { formatarMoeda } from '../../../i18n/format';
import { useConvenios } from '../convenios.api';
import type { ConvenioListItem } from '../convenios.api';
import type { SituacaoConvenio } from '../convenios.helpers';
import { situacaoConvenioLabel, situacaoConvenioTag } from '../convenios.helpers';
import { ConveniosSubNav } from '../ConveniosSubNav';

const SITUACOES: ReadonlyArray<SituacaoConvenio> = [
  'EmProposta',
  'Celebrado',
  'EmExecucao',
  'EmPrestacaoContas',
  'EmAnalise',
  'Aprovado',
  'AprovadoComRessalva',
  'Rejeitado',
  'Inadimplente',
];

const OPCOES: SelectOption[] = [
  { value: '', label: 'Todas as situações' },
  ...SITUACOES.map((s) => ({ value: s, label: situacaoConvenioLabel(s) })),
];

export function ConvenioListPage() {
  const [situacao, setSituacao] = useState<SituacaoConvenio | ''>('');
  const query = useConvenios(situacao === '' ? undefined : situacao);

  const columns: Column<ConvenioListItem>[] = [
    {
      key: 'concedente',
      header: 'Concedente',
      sortAccessor: (c) => c.concedenteNome,
      render: (c) => c.concedenteNome,
    },
    {
      key: 'transferegov',
      header: 'Nº Transferegov',
      render: (c) => c.numeroTransferegov ?? '—',
    },
    {
      key: 'objeto',
      header: 'Objeto',
      render: (c) => c.objeto,
    },
    {
      key: 'valor',
      header: 'Valor global',
      align: 'end',
      sortAccessor: (c) => c.valorGlobal,
      render: (c) => formatarMoeda(c.valorGlobal),
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (c) => c.situacao,
      render: (c) => (
        <Tag variant={situacaoConvenioTag(c.situacao)}>{situacaoConvenioLabel(c.situacao)}</Tag>
      ),
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (c) => (
        <Link className="br-button tertiary small" to={`/convenios/recebidos/${c.id}`}>
          Detalhes
        </Link>
      ),
    },
  ];

  return (
    <>
      <ConveniosSubNav />
      <PageHeader
        eyebrow="Convênios e Parcerias"
        title="Convênios federais recebidos"
        description="Transferências voluntárias recebidas (Dec. 11.531/2023): proposta → celebração → execução físico-financeira e contrapartida → prestação de contas → análise."
      />

      <Card className="mb-4">
        <FormField label="Filtrar por situação">
          {({ id, describedBy }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              options={OPCOES}
              value={situacao}
              onChange={(e) => setSituacao(e.target.value as SituacaoConvenio | '')}
            />
          )}
        </FormField>
      </Card>

      <DataTable
        caption="Convênios federais recebidos"
        columns={columns}
        rows={query.data}
        rowKey={(c) => c.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-hand-holding-dollar"
            title="Nenhum convênio encontrado"
            description="Não há convênios federais recebidos para o filtro selecionado."
          />
        }
      />
    </>
  );
}
