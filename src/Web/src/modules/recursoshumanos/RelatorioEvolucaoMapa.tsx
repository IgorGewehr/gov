// Seções dos relatórios: EVOLUÇÃO mensal da despesa de pessoal (série + gráfico de
// barras CSS acessível) e MAPA DE CARGOS (ocupados x vagos). Somente leitura.
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
import { MESES, situacaoCargoTagVariant } from './recursosHumanos.helpers';
import { useEvolucaoDespesa, useMapaCargos } from './relatorio.api';
import type {
  EvolucaoDespesaPessoalView,
  LinhaMapaCargo,
  MapaCargosView,
  PontoEvolucaoDespesa,
  TotalMapaCargoPorTipo,
} from './relatorio.api';
import { formatarCompetencia } from './relatorio.helpers';

const ANO_ATUAL = new Date().getFullYear();
const MES_ATUAL = new Date().getMonth() + 1;

// ---------------------------------------------------------------------------
// Evolução da despesa de pessoal
// ---------------------------------------------------------------------------

const COLS_EVOLUCAO: Column<PontoEvolucaoDespesa>[] = [
  { key: 'comp', header: 'Competência', render: (r) => formatarCompetencia(r.competencia) },
  { key: 'situacao', header: 'Situação', render: (r) => r.situacaoFolha },
  { key: 'qtd', header: 'Servidores', align: 'end', render: (r) => r.quantidadeServidores },
  {
    key: 'bruta',
    header: 'Despesa bruta',
    align: 'end',
    render: (r) => formatarMoeda(r.despesaBruta),
    sortAccessor: (r) => r.despesaBruta,
  },
  { key: 'descontos', header: 'Descontos', align: 'end', render: (r) => formatarMoeda(r.totalDescontos) },
  { key: 'liquido', header: 'Líquido', align: 'end', render: (r) => formatarMoeda(r.totalLiquido) },
];

/** Gráfico de barras CSS acessível da despesa bruta por competência. */
function GraficoEvolucao({ serie }: { serie: PontoEvolucaoDespesa[] }) {
  const maximo = serie.reduce((m, p) => Math.max(m, p.despesaBruta), 0);
  if (maximo <= 0 || serie.length === 0) return null;
  return (
    <ul className="d-flex list-style-none p-0 m-0" style={{ gap: '0.5rem', alignItems: 'flex-end', height: '160px' }}>
      {serie.map((p) => {
        const pct = Math.round((p.despesaBruta / maximo) * 100);
        const titulo = `${formatarCompetencia(p.competencia)}: ${formatarMoeda(p.despesaBruta)}`;
        return (
          <li
            key={p.competencia}
            className="d-flex flex-column align-items-center text-center"
            style={{ flex: '1 1 0', minWidth: 0 }}
            title={titulo}
          >
            <div
              className="bg-blue-warm-vivid-50 w-100"
              style={{ height: `${Math.max(pct, 2)}%`, borderRadius: '4px 4px 0 0' }}
              role="img"
              aria-label={titulo}
            />
            <span className="text-down-02 text-gray-60 mt-1" style={{ wordBreak: 'break-all' }}>
              {p.mes.toString().padStart(2, '0')}/{p.ano}
            </span>
          </li>
        );
      })}
    </ul>
  );
}

export function EvolucaoDespesaSecao() {
  const [anoDe, setAnoDe] = useState(String(ANO_ATUAL));
  const [mesDe, setMesDe] = useState('1');
  const [anoAte, setAnoAte] = useState(String(ANO_ATUAL));
  const [mesAte, setMesAte] = useState(String(MES_ATUAL));
  const [consulta, setConsulta] = useState<{
    anoDe: number;
    mesDe: number;
    anoAte: number;
    mesAte: number;
  } | null>(null);

  const query = useEvolucaoDespesa(
    consulta?.anoDe ?? 0,
    consulta?.mesDe ?? 0,
    consulta?.anoAte ?? 0,
    consulta?.mesAte ?? 0,
    consulta !== null,
  );

  function consultar(event: FormEvent): void {
    event.preventDefault();
    const valores = [anoDe, mesDe, anoAte, mesAte].map(Number);
    if (valores.some((v) => !Number.isInteger(v))) return;
    setConsulta({ anoDe: valores[0], mesDe: valores[1], anoAte: valores[2], mesAte: valores[3] });
  }

  return (
    <>
      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <FormRow
            acao={
              <Button variant="primary" type="submit" loading={query.isFetching}>
                Consultar
              </Button>
            }
          >
            <div className="row">
              <div className="col-sm-3">
                <FormField label="Mês inicial" required>
                  {({ id, describedBy }) => (
                    <Select id={id} aria-describedby={describedBy} value={mesDe} onChange={(e) => setMesDe(e.target.value)} options={MESES} />
                  )}
                </FormField>
              </div>
              <div className="col-sm-3">
                <FormField label="Ano inicial" required>
                  {({ id, describedBy }) => (
                    <Input id={id} type="number" min="2000" max="2100" step="1" inputMode="numeric" aria-describedby={describedBy} value={anoDe} onChange={(e) => setAnoDe(e.target.value)} />
                  )}
                </FormField>
              </div>
              <div className="col-sm-3">
                <FormField label="Mês final" required>
                  {({ id, describedBy }) => (
                    <Select id={id} aria-describedby={describedBy} value={mesAte} onChange={(e) => setMesAte(e.target.value)} options={MESES} />
                  )}
                </FormField>
              </div>
              <div className="col-sm-3">
                <FormField label="Ano final" required>
                  {({ id, describedBy }) => (
                    <Input id={id} type="number" min="2000" max="2100" step="1" inputMode="numeric" aria-describedby={describedBy} value={anoAte} onChange={(e) => setAnoAte(e.target.value)} />
                  )}
                </FormField>
              </div>
            </div>
          </FormRow>
        </form>
      </Card>

      {consulta === null ? (
        <EmptyState
          icon="fas fa-magnifying-glass"
          title="Selecione o intervalo de competências"
          description="Escolha as competências inicial e final e clique em Consultar."
        />
      ) : (
        <QueryState<EvolucaoDespesaPessoalView>
          isLoading={query.isLoading}
          isError={query.isError}
          error={query.error}
          data={query.data}
          empty={<EmptyState icon="fas fa-chart-line" title="Sem dados no intervalo" />}
        >
          {(ev) => (
            <>
              <MetricaGrade>
                <Metrica
                  label="Intervalo"
                  valor={`${formatarCompetencia(ev.competenciaInicial)} – ${formatarCompetencia(ev.competenciaFinal)}`}
                />
                <Metrica label="Meses com folha" valor={ev.mesesComFolha} />
                <Metrica label="Despesa bruta acumulada" valor={formatarMoeda(ev.despesaBrutaAcumulada)} />
                <Metrica label="Média mensal" valor={formatarMoeda(ev.despesaBrutaMediaMensal)} />
              </MetricaGrade>

              <CardSecao
                titulo="Evolução da despesa bruta"
                subtitulo="Despesa bruta de pessoal por competência (base gerencial para o limite da LRF, art. 19/20)."
                className="mt-4"
                nota="A Receita Corrente Líquida (RCL) para o cálculo do percentual da LRF é fornecida fora deste módulo."
              >
                <GraficoEvolucao serie={ev.serie} />
              </CardSecao>

              <CardSecao titulo="Série mensal" className="mt-4">
                <DataTable<PontoEvolucaoDespesa>
                  caption="Série mensal da despesa de pessoal"
                  columns={COLS_EVOLUCAO}
                  rows={ev.serie}
                  rowKey={(r) => r.competencia}
                />
              </CardSecao>
            </>
          )}
        </QueryState>
      )}
    </>
  );
}

// ---------------------------------------------------------------------------
// Mapa de cargos
// ---------------------------------------------------------------------------

const COLS_MAPA_TIPO: Column<TotalMapaCargoPorTipo>[] = [
  { key: 'tipo', header: 'Tipo', render: (r) => r.tipo },
  { key: 'aut', header: 'Autorizadas', align: 'end', render: (r) => r.vagasAutorizadas },
  { key: 'ocu', header: 'Ocupadas', align: 'end', render: (r) => r.vagasOcupadas },
  { key: 'disp', header: 'Vagas', align: 'end', render: (r) => r.vagasDisponiveis },
];

const COLS_MAPA_CARGO: Column<LinhaMapaCargo>[] = [
  { key: 'cargo', header: 'Cargo', render: (r) => r.denominacao, sortAccessor: (r) => r.denominacao },
  { key: 'tipo', header: 'Tipo', render: (r) => r.tipo },
  {
    key: 'situacao',
    header: 'Situação',
    render: (r) => <Tag variant={situacaoCargoTagVariant(r.situacao)}>{r.situacao}</Tag>,
  },
  { key: 'unidade', header: 'Unidade', render: (r) => r.unidade },
  { key: 'venc', header: 'Vencimento', align: 'end', render: (r) => formatarMoeda(r.vencimento) },
  {
    key: 'aut',
    header: 'Autorizadas',
    align: 'end',
    render: (r) => r.vagasAutorizadas,
    sortAccessor: (r) => r.vagasAutorizadas,
  },
  {
    key: 'ocu',
    header: 'Ocupadas',
    align: 'end',
    render: (r) => r.vagasOcupadas,
    sortAccessor: (r) => r.vagasOcupadas,
  },
  {
    key: 'disp',
    header: 'Vagas',
    align: 'end',
    render: (r) => r.vagasDisponiveis,
    sortAccessor: (r) => r.vagasDisponiveis,
  },
];

export function MapaCargosSecao() {
  const [ativo, setAtivo] = useState(false);
  const query = useMapaCargos(ativo);

  if (!ativo) {
    return (
      <EmptyState
        icon="fas fa-sitemap"
        title="Mapa de cargos do quadro"
        description="Vagas autorizadas x ocupadas x livres, por cargo e por tipo."
        action={
          <Button variant="primary" onClick={() => setAtivo(true)}>
            Gerar mapa de cargos
          </Button>
        }
      />
    );
  }

  return (
    <QueryState<MapaCargosView>
      isLoading={query.isLoading}
      isError={query.isError}
      error={query.error}
      data={query.data}
      empty={<EmptyState icon="fas fa-sitemap" title="Nenhum cargo no quadro" />}
    >
      {(mapa) => {
        const ocupacao =
          mapa.totalVagasAutorizadas > 0
            ? (mapa.totalVagasOcupadas / mapa.totalVagasAutorizadas).toLocaleString('pt-BR', {
                style: 'percent',
                maximumFractionDigits: 1,
              })
            : '—';
        return (
          <>
            <MetricaGrade>
              <Metrica label="Vagas autorizadas" valor={mapa.totalVagasAutorizadas} />
              <Metrica
                label="Vagas ocupadas"
                valor={mapa.totalVagasOcupadas}
                secundario={`${ocupacao} de ocupação`}
                tom="sucesso"
              />
              <Metrica label="Vagas livres" valor={mapa.totalVagasDisponiveis} tom="alerta" />
            </MetricaGrade>

            <CardSecao titulo="Totais por tipo de cargo" className="mt-4">
              <DataTable<TotalMapaCargoPorTipo>
                caption="Mapa de cargos — totais por tipo"
                columns={COLS_MAPA_TIPO}
                rows={mapa.porTipo}
                rowKey={(r) => r.tipo}
              />
            </CardSecao>

            <CardSecao
              titulo="Cargos do quadro"
              subtitulo="Cargos detalhados (decrescente por vagas ocupadas). Cargos extintos aparecem marcados."
              className="mt-4"
            >
              <DataTable<LinhaMapaCargo>
                caption="Mapa de cargos — detalhado"
                columns={COLS_MAPA_CARGO}
                rows={mapa.cargos}
                rowKey={(r) => r.cargoId}
              />
            </CardSecao>
          </>
        );
      }}
    </QueryState>
  );
}
