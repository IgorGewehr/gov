// Fluxo B — LISTA de parcerias OSC / MROSC (query ListarParcerias).
// Filtro por situacao, DataTable com semaforo de situacao e DESTAQUE do estado
// Inadimplente (repasses BLOQUEADOS — B-INV-9), com link para o detalhe.
import { useState } from 'react';
import { Link } from 'react-router-dom';
import {
  Alert,
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
import { useParcerias } from '../convenios.api';
import type { ParceriaListItem } from '../convenios.api';
import type { SituacaoParceria } from '../convenios.helpers';
import { parceriaBloqueada, situacaoParceriaLabel, situacaoParceriaTag } from '../convenios.helpers';
import { ConveniosSubNav } from '../ConveniosSubNav';

const SITUACOES: ReadonlyArray<SituacaoParceria> = [
  'EmSelecao',
  'Celebrada',
  'EmExecucao',
  'EmPrestacaoContas',
  'EmAnalise',
  'Aprovada',
  'AprovadaComRessalva',
  'Rejeitada',
  'Inadimplente',
];

const OPCOES: SelectOption[] = [
  { value: '', label: 'Todas as situações' },
  ...SITUACOES.map((s) => ({ value: s, label: situacaoParceriaLabel(s) })),
];

export function ParceriaListPage() {
  const [situacao, setSituacao] = useState<SituacaoParceria | ''>('');
  const query = useParcerias(situacao === '' ? undefined : situacao);

  const haInadimplente = (query.data ?? []).some((p) => parceriaBloqueada(p.situacao));

  const columns: Column<ParceriaListItem>[] = [
    {
      key: 'osc',
      header: 'OSC',
      sortAccessor: (p) => p.oscRazaoSocial,
      render: (p) => p.oscRazaoSocial,
    },
    {
      key: 'instrumento',
      header: 'Instrumento',
      sortAccessor: (p) => p.tipoInstrumento,
      render: (p) => p.tipoInstrumento,
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (p) => p.situacao,
      render: (p) => (
        <Tag variant={situacaoParceriaTag(p.situacao)}>{situacaoParceriaLabel(p.situacao)}</Tag>
      ),
    },
    {
      key: 'bloqueio',
      header: 'Repasses',
      render: (p) =>
        parceriaBloqueada(p.situacao) ? (
          <Tag variant="danger">
            <i className="fas fa-ban" aria-hidden="true" /> Bloqueados
          </Tag>
        ) : (
          'Liberáveis'
        ),
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (p) => (
        <Link className="br-button tertiary small" to={`/convenios/parcerias/${p.id}`}>
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
        title="Parcerias com OSC (MROSC)"
        description="Parcerias com Organizações da Sociedade Civil (Lei 13.019/2014): chamamento/dispensa → termo (Colaboração/Fomento/Cooperação) → plano de trabalho → repasses → prestação de contas → análise."
      />

      {haInadimplente && (
        <Alert variant="warning" className="mb-4">
          Há parceria(s) em situação <strong>Inadimplente</strong>: enquanto a inadimplência
          persistir, <strong>novos repasses ficam BLOQUEADOS</strong> (B-INV-9 — LRF art. 48).
        </Alert>
      )}

      <Card className="mb-4">
        <FormField label="Filtrar por situação">
          {({ id, describedBy }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              options={OPCOES}
              value={situacao}
              onChange={(e) => setSituacao(e.target.value as SituacaoParceria | '')}
            />
          )}
        </FormField>
      </Card>

      <DataTable
        caption="Parcerias com OSC (MROSC)"
        columns={columns}
        rows={query.data}
        rowKey={(p) => p.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-people-group"
            title="Nenhuma parceria encontrada"
            description="Não há parcerias com OSC para o filtro selecionado."
          />
        }
      />
    </>
  );
}
