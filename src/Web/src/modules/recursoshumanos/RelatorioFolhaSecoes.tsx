// Seções dos relatórios por COMPETÊNCIA: folha por secretaria/fonte e demonstrativo TCE.
// Padrão-ouro: consulta sob demanda (enabled), QueryState, totais em MetricaGrade e
// quebras em DataTable. Somente leitura — sem mutations.
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Button,
  Card,
  CardSecao,
  DataTable,
  EmptyState,
  FormField,
  FormRow,
  Input,
  Metrica,
  MetricaGrade,
  QueryState,
  Select,
  Tag,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { formatarMoeda } from '../../i18n/format';
import { MESES } from './recursosHumanos.helpers';
import {
  useDemonstrativoTce,
  useFolhaPorSecretaria,
} from './relatorio.api';
import type {
  DemonstrativoTceFonte,
  DemonstrativoTceUnidade,
  FolhaPorSecretariaView,
  DemonstrativoTceView,
  LinhaFolhaFonte,
  LinhaFolhaSecretaria,
} from './relatorio.api';
import { formatarCompetencia, formatarFonte, situacaoFolhaRelTagVariant } from './relatorio.helpers';

const ANO_ATUAL = new Date().getFullYear();
const MES_ATUAL = new Date().getMonth() + 1;

/** Formulário de competência (mês + ano) reutilizado pelas seções. */
function CompetenciaForm({
  ano,
  mes,
  setAno,
  setMes,
  fetching,
  onConsultar,
}: {
  ano: string;
  mes: string;
  setAno: (v: string) => void;
  setMes: (v: string) => void;
  fetching: boolean;
  onConsultar: (event: FormEvent) => void;
}) {
  return (
    <Card className="mb-4">
      <form className="br-form" onSubmit={onConsultar}>
        <FormRow
          acao={
            <Button variant="primary" type="submit" loading={fetching}>
              Consultar
            </Button>
          }
        >
          <div className="row">
            <div className="col-sm-6">
              <FormField label="Mês" required>
                {({ id, describedBy }) => (
                  <Select
                    id={id}
                    aria-describedby={describedBy}
                    value={mes}
                    onChange={(e) => setMes(e.target.value)}
                    options={MESES}
                  />
                )}
              </FormField>
            </div>
            <div className="col-sm-6">
              <FormField label="Ano" required>
                {({ id, describedBy }) => (
                  <Input
                    id={id}
                    type="number"
                    min="2000"
                    max="2100"
                    step="1"
                    inputMode="numeric"
                    aria-describedby={describedBy}
                    value={ano}
                    onChange={(e) => setAno(e.target.value)}
                  />
                )}
              </FormField>
            </div>
          </div>
        </FormRow>
      </form>
    </Card>
  );
}

const COLS_SECRETARIA: Column<LinhaFolhaSecretaria>[] = [
  { key: 'unidade', header: 'Secretaria / UO', render: (r) => r.unidade, sortAccessor: (r) => r.unidade },
  {
    key: 'qtd',
    header: 'Servidores',
    align: 'end',
    render: (r) => r.quantidadeServidores,
    sortAccessor: (r) => r.quantidadeServidores,
  },
  {
    key: 'proventos',
    header: 'Proventos',
    align: 'end',
    render: (r) => formatarMoeda(r.totalProventos),
    sortAccessor: (r) => r.totalProventos,
  },
  {
    key: 'descontos',
    header: 'Descontos',
    align: 'end',
    render: (r) => formatarMoeda(r.totalDescontos),
    sortAccessor: (r) => r.totalDescontos,
  },
  {
    key: 'liquido',
    header: 'Líquido',
    align: 'end',
    render: (r) => formatarMoeda(r.totalLiquido),
    sortAccessor: (r) => r.totalLiquido,
  },
];

const COLS_FONTE: Column<LinhaFolhaFonte>[] = [
  { key: 'fonte', header: 'Fonte (regime)', render: (r) => formatarFonte(r.fonte) },
  { key: 'qtd', header: 'Servidores', align: 'end', render: (r) => r.quantidadeServidores },
  { key: 'proventos', header: 'Proventos', align: 'end', render: (r) => formatarMoeda(r.totalProventos) },
  { key: 'descontos', header: 'Descontos', align: 'end', render: (r) => formatarMoeda(r.totalDescontos) },
  { key: 'liquido', header: 'Líquido', align: 'end', render: (r) => formatarMoeda(r.totalLiquido) },
];

const EMPTY_COMPETENCIA = (
  <EmptyState
    icon="fas fa-file-circle-question"
    title="Nenhuma folha para a competência"
    description="Não há folha mensal apurada nessa competência. Selecione outra."
  />
);

export function FolhaPorSecretariaSecao() {
  const [ano, setAno] = useState(String(ANO_ATUAL));
  const [mes, setMes] = useState(String(MES_ATUAL));
  const [consulta, setConsulta] = useState<{ ano: number; mes: number } | null>(null);
  const query = useFolhaPorSecretaria(consulta?.ano ?? 0, consulta?.mes ?? 0, consulta !== null);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    const a = Number(ano);
    const m = Number(mes);
    if (!Number.isInteger(a) || !Number.isInteger(m)) return;
    setConsulta({ ano: a, mes: m });
  }

  return (
    <>
      <CompetenciaForm
        ano={ano}
        mes={mes}
        setAno={setAno}
        setMes={setMes}
        fetching={query.isFetching}
        onConsultar={consultar}
      />
      {consulta === null ? (
        <EmptyState
          icon="fas fa-magnifying-glass"
          title="Selecione uma competência"
          description="Escolha mês e ano e clique em Consultar."
        />
      ) : (
        <QueryState<FolhaPorSecretariaView>
          isLoading={query.isLoading}
          isError={query.isError}
          error={query.error}
          data={query.data ?? undefined}
          empty={EMPTY_COMPETENCIA}
        >
          {(folha) => (
            <>
              <MetricaGrade>
                <Metrica label="Competência" valor={formatarCompetencia(folha.competencia)} />
                <Metrica
                  label="Situação"
                  valor={
                    <Tag variant={situacaoFolhaRelTagVariant(folha.situacaoFolha)}>
                      {folha.situacaoFolha}
                    </Tag>
                  }
                />
                <Metrica label="Servidores" valor={folha.quantidadeServidores} />
                <Metrica label="Total de proventos" valor={formatarMoeda(folha.totalProventos)} />
                <Metrica label="Total de descontos" valor={formatarMoeda(folha.totalDescontos)} />
                <Metrica
                  label="Total líquido"
                  valor={formatarMoeda(folha.totalLiquido)}
                  tom="sucesso"
                />
              </MetricaGrade>

              <CardSecao
                titulo="Por secretaria / unidade de lotação"
                subtitulo="Folha consolidada por UO na competência (decrescente por proventos)."
                className="mt-4"
              >
                <DataTable<LinhaFolhaSecretaria>
                  caption="Folha por secretaria/UO"
                  columns={COLS_SECRETARIA}
                  rows={folha.porSecretaria}
                  rowKey={(r) => r.unidade}
                />
              </CardSecao>

              <CardSecao
                titulo="Por fonte (regime previdenciário)"
                subtitulo="RPPS (regime próprio) x RGPS (regime geral) — eixo da despesa previdenciária patronal."
                className="mt-4"
                nota="Fonte no contexto RH = regime previdenciário (EC 103/2019)."
              >
                <DataTable<LinhaFolhaFonte>
                  caption="Folha por fonte/regime"
                  columns={COLS_FONTE}
                  rows={folha.porFonte}
                  rowKey={(r) => r.fonte}
                />
              </CardSecao>
            </>
          )}
        </QueryState>
      )}
    </>
  );
}

const COLS_TCE_FONTE: Column<DemonstrativoTceFonte>[] = COLS_FONTE;

const COLS_TCE_UNIDADE: Column<DemonstrativoTceUnidade>[] = [
  { key: 'unidade', header: 'Secretaria / UO', render: (r) => r.unidade, sortAccessor: (r) => r.unidade },
  { key: 'qtd', header: 'Servidores', align: 'end', render: (r) => r.quantidadeServidores },
  { key: 'proventos', header: 'Proventos', align: 'end', render: (r) => formatarMoeda(r.totalProventos) },
  { key: 'descontos', header: 'Descontos', align: 'end', render: (r) => formatarMoeda(r.totalDescontos) },
  { key: 'liquido', header: 'Líquido', align: 'end', render: (r) => formatarMoeda(r.totalLiquido) },
];

export function DemonstrativoTceSecao() {
  const [ano, setAno] = useState(String(ANO_ATUAL));
  const [mes, setMes] = useState(String(MES_ATUAL));
  const [consulta, setConsulta] = useState<{ ano: number; mes: number } | null>(null);
  const query = useDemonstrativoTce(consulta?.ano ?? 0, consulta?.mes ?? 0, consulta !== null);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    const a = Number(ano);
    const m = Number(mes);
    if (!Number.isInteger(a) || !Number.isInteger(m)) return;
    setConsulta({ ano: a, mes: m });
  }

  return (
    <>
      <CompetenciaForm
        ano={ano}
        mes={mes}
        setAno={setAno}
        setMes={setMes}
        fetching={query.isFetching}
        onConsultar={consultar}
      />
      {consulta === null ? (
        <EmptyState
          icon="fas fa-magnifying-glass"
          title="Selecione uma competência"
          description="Escolha mês e ano e clique em Consultar."
        />
      ) : (
        <QueryState<DemonstrativoTceView>
          isLoading={query.isLoading}
          isError={query.isError}
          error={query.error}
          data={query.data ?? undefined}
          empty={EMPTY_COMPETENCIA}
        >
          {(d) => (
            <>
              <MetricaGrade>
                <Metrica label="Competência" valor={formatarCompetencia(d.competencia)} />
                <Metrica label="Servidores" valor={d.quantidadeServidores} />
                <Metrica label="Total bruto (proventos)" valor={formatarMoeda(d.totalProventos)} />
                <Metrica label="Total de descontos" valor={formatarMoeda(d.totalDescontos)} />
                <Metrica label="Total líquido" valor={formatarMoeda(d.totalLiquido)} tom="sucesso" />
                <Metrica
                  label="Contribuição prev. do segurado"
                  valor={formatarMoeda(d.contribuicaoPrevidenciariaSegurado)}
                />
              </MetricaGrade>

              <CardSecao
                titulo="Por fonte (regime previdenciário)"
                className="mt-4"
              >
                <DataTable<DemonstrativoTceFonte>
                  caption="Demonstrativo TCE por fonte"
                  columns={COLS_TCE_FONTE}
                  rows={d.porFonte}
                  rowKey={(r) => r.fonte}
                />
              </CardSecao>

              <CardSecao
                titulo="Por secretaria / unidade de lotação"
                className="mt-4"
                nota="Visão gerencial. A remessa formatada SIAPC/PAD é do módulo Transparência."
              >
                <DataTable<DemonstrativoTceUnidade>
                  caption="Demonstrativo TCE por secretaria/UO"
                  columns={COLS_TCE_UNIDADE}
                  rows={d.porSecretaria}
                  rowKey={(r) => r.unidade}
                />
              </CardSecao>
            </>
          )}
        </QueryState>
      )}
    </>
  );
}
