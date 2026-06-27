// Retenções/consignações — guias de recolhimento (extra-orçamentário). Lista as guias por situação,
// permite registrar o recolhimento (baixa do passivo) e exibe a tabela de IRRF/PJ vigente (IN RFB
// 1234/2012) como referência. Reúne o ciclo extra-orçamentário das retenções apuradas na liquidação.
import { useState } from 'react';
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
  errorMessage,
  useToast,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarData, formatarMoeda } from '../../i18n/format';
import { FinancasSubNav } from './FinancasSubNav';
import { mensagemErro } from './financas.helpers';
import {
  NATUREZA_RETENCAO_LABEL,
  SITUACAO_GUIA_LABEL,
  useGuiasRecolhimento,
  useRecolherGuia,
  useSemearTabelaIrrf,
  useTabelaIrrf,
  type GuiaRecolhimento,
} from './retencoes.api';

const SITUACOES = [
  { value: '', label: 'Todas as situações' },
  { value: '1', label: 'Emitidas' },
  { value: '2', label: 'Recolhidas' },
  { value: '3', label: 'Canceladas' },
];

function hoje(): string {
  return new Date().toISOString().slice(0, 10);
}

export function GuiasRecolhimentoPage() {
  const toast = useToast();
  const [situacao, setSituacao] = useState('');
  const filtro = situacao === '' ? undefined : Number(situacao);
  const query = useGuiasRecolhimento(filtro);
  const tabela = useTabelaIrrf();
  const semear = useSemearTabelaIrrf();
  const recolher = useRecolherGuia();

  function aoSemear(): void {
    semear.mutate(undefined, {
      onSuccess: (r) => toast.success(`Tabela de IRRF semeada (${r.faixas} faixas).`, 'Sucesso'),
      onError: (e) => toast.error(mensagemErro(e, 'Não foi possível semear a tabela de IRRF.')),
    });
  }

  function aoRecolher(guia: GuiaRecolhimento): void {
    recolher.mutate(
      { guiaId: guia.guiaRecolhimentoId, dataRecolhimento: hoje() },
      {
        onSuccess: () => toast.success('Recolhimento registrado.', 'Sucesso'),
        onError: (e) => toast.error(mensagemErro(e, 'Não foi possível registrar o recolhimento.')),
      },
    );
  }

  const columns: Column<GuiaRecolhimento>[] = [
    {
      key: 'natureza',
      header: 'Natureza',
      sortAccessor: (g) => g.natureza,
      render: (g) => NATUREZA_RETENCAO_LABEL[g.natureza] ?? String(g.natureza),
    },
    { key: 'codigoReceita', header: 'Cód. receita', render: (g) => g.codigoReceita ?? '—' },
    {
      key: 'competencia',
      header: 'Competência',
      sortAccessor: (g) => g.competencia,
      render: (g) => formatarData(g.competencia),
    },
    {
      key: 'vencimento',
      header: 'Vencimento',
      sortAccessor: (g) => g.dataVencimento,
      render: (g) => formatarData(g.dataVencimento),
    },
    {
      key: 'valor',
      header: 'Valor total',
      align: 'end',
      sortAccessor: (g) => g.valorTotal,
      render: (g) => formatarMoeda(g.valorTotal),
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (g) => g.situacao,
      render: (g) => (
        <Tag variant={g.situacao === 2 ? 'success' : g.situacao === 3 ? 'danger' : 'warning'}>
          {SITUACAO_GUIA_LABEL[g.situacao] ?? String(g.situacao)}
        </Tag>
      ),
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (g) =>
        g.situacao === 1 ? (
          <Can permission="financas.gerenciar">
            <Button variant="primary" size="sm" onClick={() => aoRecolher(g)} loading={recolher.isPending}>
              Recolher
            </Button>
          </Can>
        ) : (
          <span className="text-gray-60">—</span>
        ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Retenções e recolhimentos"
        description="Consignações extra-orçamentárias (IRRF IN RFB 1234/2012, INSS, ISS, caução) e suas guias de recolhimento."
        actions={
          <Can permission="financas.gerenciar">
            <Toolbar>
              <Button variant="secondary" onClick={aoSemear} loading={semear.isPending}>
                <i className="fas fa-table" aria-hidden="true" /> Semear tabela IRRF
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <FinancasSubNav />

      {tabela.data ? (
        <Card className="mb-4" header={<strong>Tabela de IRRF/PJ vigente (IN RFB 1234/2012)</strong>}>
          <p className="text-down-01 text-gray-70 mb-2">
            Vigência desde {formatarData(tabela.data.vigenciaInicio)} · dispensa de retenção abaixo de{' '}
            {formatarMoeda(tabela.data.valorMinimoRetencao)} por DARF.
          </p>
          <DataTable
            caption="Faixas de IRRF de serviços/PJ"
            columns={[
              { key: 'codigo', header: 'Enquadramento', render: (f) => f.codigo },
              { key: 'descricao', header: 'Natureza', render: (f) => f.descricao },
              {
                key: 'aliquota',
                header: 'Alíquota',
                align: 'end',
                render: (f) => `${(f.aliquota * 100).toFixed(2).replace('.', ',')}%`,
              },
              { key: 'darf', header: 'Cód. DARF', render: (f) => f.codigoReceitaDarf },
            ]}
            rows={tabela.data.faixas}
            rowKey={(f) => f.codigo}
          />
        </Card>
      ) : null}

      <Card className="mb-3">
        <div style={{ maxWidth: 320 }}>
          <FormField label="Situação">
            {({ id, describedBy }) => (
              <Select
                id={id}
                aria-describedby={describedBy}
                value={situacao}
                onChange={(e) => setSituacao(e.target.value)}
                options={SITUACOES}
              />
            )}
          </FormField>
        </div>
      </Card>

      <DataTable
        caption="Guias de recolhimento de consignações"
        columns={columns}
        rows={query.data ?? []}
        rowKey={(g) => g.guiaRecolhimentoId}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-receipt"
            title="Nenhuma guia de recolhimento"
            description="As guias são emitidas a partir das retenções pendentes apuradas nas liquidações."
          />
        }
      />
    </>
  );
}
