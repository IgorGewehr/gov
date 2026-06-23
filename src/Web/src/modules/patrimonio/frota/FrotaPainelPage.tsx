// PAINEL DE FROTA (Onda 3a). Visão de GESTÃO sobre o agregado Veiculo:
//   - KPIs do período (combustível, manutenção, multas, total, consumo médio km/L)
//     em Metrica/MetricaGrade;
//   - custo/consumo POR VEÍCULO no período (CardSecao + DataTable);
//   - ALERTAS: CNH a vencer/vencida (janela de dias) + manutenções em aberto;
//   - REGISTRAR operação: multa direto pelo painel (modal scoped ao veículo);
//     abastecimento/manutenção exigem contexto de odômetro -> levam ao detalhe.
// Read models reais: GET /frota/painel, /frota/cnh-vencendo, /frota/manutencoes/abertas.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link } from 'react-router-dom';
import {
  Button,
  CardSecao,
  DataTable,
  EmptyState,
  errorMessage,
  FormField,
  FormRow,
  Input,
  Metrica,
  MetricaGrade,
  PageHeader,
  Select,
  Tag,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { formatarData, formatarMoeda } from '../../../i18n/format';
import { PatrimonioSubNav } from '../PatrimonioSubNav';
import { RegistrarMultaModal } from '../veiculo/VeiculoAcaoModais';
import {
  useCnhVencendo,
  useManutencoesAbertas,
  usePainelFrota,
} from './frota.api';
import type { CnhVencendoResumo, CustoVeiculoResumo, ManutencaoAbertaResumo } from './frota.api';

function inicioDoAnoIso(): string {
  return `${new Date().getFullYear()}-01-01`;
}

function hojeIso(): string {
  return new Date().toISOString().slice(0, 10);
}

function consumoLabel(consumo: number | null): string {
  return consumo == null ? '—' : `${consumo.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} km/L`;
}

const JANELA_CNH_OPCOES = [
  { value: '30', label: '30 dias' },
  { value: '60', label: '60 dias' },
  { value: '90', label: '90 dias' },
];

function PainelKpis({ de, ate }: { de: string; ate: string }) {
  const query = usePainelFrota(de, ate);
  const [veiculoMulta, setVeiculoMulta] = useState<CustoVeiculoResumo | null>(null);

  const colunas: Column<CustoVeiculoResumo>[] = [
    {
      key: 'placa',
      header: 'Placa',
      sortAccessor: (v) => v.placa,
      render: (v) => <Link to={`/patrimonio/frota/veiculos/${v.veiculoId}`}>{v.placa}</Link>,
    },
    { key: 'descricao', header: 'Descrição', sortAccessor: (v) => v.descricao, render: (v) => v.descricao },
    { key: 'combustivel', header: 'Combustível', align: 'end', sortAccessor: (v) => v.gastoCombustivel, render: (v) => formatarMoeda(v.gastoCombustivel) },
    { key: 'manutencao', header: 'Manutenção', align: 'end', sortAccessor: (v) => v.gastoManutencao, render: (v) => formatarMoeda(v.gastoManutencao) },
    { key: 'multas', header: 'Multas', align: 'end', sortAccessor: (v) => v.gastoMultas, render: (v) => formatarMoeda(v.gastoMultas) },
    { key: 'total', header: 'Custo total', align: 'end', sortAccessor: (v) => v.custoTotal, render: (v) => <strong>{formatarMoeda(v.custoTotal)}</strong> },
    { key: 'consumo', header: 'Consumo', align: 'end', sortAccessor: (v) => v.consumoMedioKmL ?? -1, render: (v) => consumoLabel(v.consumoMedioKmL) },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (v) => (
        <Can permission="patrimonio.gerenciar">
          <Button variant="secondary" size="sm" onClick={() => setVeiculoMulta(v)}>
            <i className="fas fa-triangle-exclamation" aria-hidden="true" /> Multa
          </Button>
        </Can>
      ),
    },
  ];

  const resumo = query.data;

  return (
    <>
      <MetricaGrade>
        <Metrica label="Gasto com combustível" valor={formatarMoeda(resumo?.gastoCombustivel ?? 0)} secundario={`${(resumo?.litrosTotais ?? 0).toLocaleString('pt-BR')} L abastecidos`} />
        <Metrica label="Gasto com manutenção" valor={formatarMoeda(resumo?.gastoManutencao ?? 0)} secundario={`${resumo?.manutencoesAbertas ?? 0} OS em aberto`} tom={resumo && resumo.manutencoesAbertas > 0 ? 'alerta' : 'neutro'} />
        <Metrica label="Gasto com multas" valor={formatarMoeda(resumo?.gastoMultas ?? 0)} tom={resumo && resumo.gastoMultas > 0 ? 'alerta' : 'neutro'} />
        <Metrica label="Custo total da frota" valor={formatarMoeda(resumo?.gastoTotal ?? 0)} secundario={`${resumo?.quantidadeVeiculos ?? 0} veículo(s) com custo`} />
        <Metrica label="Consumo médio da frota" valor={consumoLabel(resumo?.consumoMedioFrotaKmL ?? null)} />
      </MetricaGrade>

      <CardSecao
        className="mt-4"
        titulo="Custo por veículo"
        subtitulo="Combustível, manutenção, multas, custo total e consumo médio no período selecionado (ordenado por custo)."
      >
        <DataTable
          caption="Custo e consumo por veículo no período"
          columns={colunas}
          rows={resumo?.veiculos}
          rowKey={(v) => v.veiculoId}
          loading={query.isLoading}
          error={query.isError ? errorMessage(query.error) : null}
          empty={
            <EmptyState
              icon="fas fa-truck"
              title="Sem custos no período"
              description="Nenhum veículo teve combustível, manutenção ou multas no período informado."
            />
          }
        />
      </CardSecao>

      {veiculoMulta && (
        <RegistrarMultaModal veiculoId={veiculoMulta.veiculoId} open onClose={() => setVeiculoMulta(null)} />
      )}
    </>
  );
}

function AlertasCnh() {
  const [dias, setDias] = useState(30);
  const query = useCnhVencendo(dias);

  const colunas: Column<CnhVencendoResumo>[] = [
    {
      key: 'placa',
      header: 'Placa',
      sortAccessor: (c) => c.placa,
      render: (c) => <Link to={`/patrimonio/frota/veiculos/${c.veiculoId}`}>{c.placa}</Link>,
    },
    { key: 'nome', header: 'Condutor', sortAccessor: (c) => c.nome, render: (c) => c.nome },
    { key: 'cnh', header: 'CNH', sortAccessor: (c) => c.cnh, render: (c) => `${c.cnh} (${c.categoriaCnh})` },
    { key: 'validade', header: 'Validade', sortAccessor: (c) => c.validadeCnh, render: (c) => formatarData(c.validadeCnh) },
    {
      key: 'status',
      header: 'Situação',
      sortAccessor: (c) => c.diasParaVencer,
      render: (c) =>
        c.vencida ? (
          <Tag variant="danger">Vencida há {Math.abs(c.diasParaVencer)} dia(s)</Tag>
        ) : (
          <Tag variant="warning">Vence em {c.diasParaVencer} dia(s)</Tag>
        ),
    },
  ];

  return (
    <CardSecao
      className="mt-4"
      titulo="CNH a vencer"
      subtitulo="Condutores designados com CNH vencida ou a vencer na janela selecionada (CTB)."
      acao={
        <FormField label="Janela">
          {({ id }) => (
            <Select
              id={id}
              options={JANELA_CNH_OPCOES}
              value={String(dias)}
              onChange={(e) => setDias(Number(e.target.value))}
            />
          )}
        </FormField>
      }
    >
      <DataTable
        caption="Condutores com CNH a vencer"
        columns={colunas}
        rows={query.data}
        rowKey={(c) => c.motoristaId}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-id-card"
            title="Nenhuma CNH a vencer"
            description={`Nenhum condutor com CNH vencida ou a vencer nos próximos ${dias} dias.`}
          />
        }
      />
    </CardSecao>
  );
}

function ManutencoesAbertas() {
  const query = useManutencoesAbertas();

  const colunas: Column<ManutencaoAbertaResumo>[] = [
    {
      key: 'placa',
      header: 'Placa',
      sortAccessor: (m) => m.placa,
      render: (m) => <Link to={`/patrimonio/frota/veiculos/${m.veiculoId}`}>{m.placa}</Link>,
    },
    { key: 'descricao', header: 'Serviço', sortAccessor: (m) => m.descricao, render: (m) => m.descricao },
    { key: 'custo', header: 'Custo estimado', align: 'end', sortAccessor: (m) => m.custoEstimado, render: (m) => formatarMoeda(m.custoEstimado) },
    { key: 'odometro', header: 'Odômetro (km)', align: 'end', sortAccessor: (m) => m.odometro, render: (m) => m.odometro.toLocaleString('pt-BR') },
  ];

  return (
    <CardSecao
      className="mt-4"
      titulo="Manutenções em aberto"
      subtitulo="Ordens de serviço ainda não concluídas em toda a frota. Conclua-as na tela do veículo."
    >
      <DataTable
        caption="Ordens de serviço de manutenção em aberto"
        columns={colunas}
        rows={query.data}
        rowKey={(m) => m.ordemServicoId}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-wrench"
            title="Nenhuma manutenção em aberto"
            description="Não há ordens de serviço pendentes de conclusão na frota."
          />
        }
      />
    </CardSecao>
  );
}

export function FrotaPainelPage() {
  const [de, setDe] = useState(inicioDoAnoIso());
  const [ate, setAte] = useState(hojeIso());
  const [periodo, setPeriodo] = useState<{ de: string; ate: string }>({ de: inicioDoAnoIso(), ate: hojeIso() });

  function consultar(event: FormEvent): void {
    event.preventDefault();
    if (de !== '' && ate !== '') setPeriodo({ de, ate });
  }

  return (
    <>
      <PatrimonioSubNav />
      <PageHeader
        eyebrow="Patrimônio · Frota"
        title="Painel de frota"
        description="Indicadores de gestão da frota: custos, consumo, CNH a vencer e manutenções em aberto."
        actions={
          <Link className="br-button secondary" to="/patrimonio/frota">
            <i className="fas fa-list" aria-hidden="true" /> Veículos
          </Link>
        }
      />

      <form className="br-form mb-4" onSubmit={consultar}>
        <FormRow
          acao={
            <Button variant="primary" type="submit" disabled={de === '' || ate === ''}>
              Atualizar
            </Button>
          }
        >
          <div className="row align-items-end">
            <div className="col-sm-4">
              <FormField label="De">
                {({ id }) => <Input id={id} type="date" value={de} onChange={(e) => setDe(e.target.value)} />}
              </FormField>
            </div>
            <div className="col-sm-4">
              <FormField label="Até">
                {({ id }) => <Input id={id} type="date" value={ate} onChange={(e) => setAte(e.target.value)} />}
              </FormField>
            </div>
          </div>
        </FormRow>
      </form>

      <PainelKpis de={periodo.de} ate={periodo.ate} />
      <AlertasCnh />
      <ManutencoesAbertas />
    </>
  );
}
