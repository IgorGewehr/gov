// MINHA DIVIDA ATIVA: posicao consolidada da Divida Ativa do PROPRIO cidadao (valor
// originario + encargos atualizados na data-base). Dado-proprio resolvido server-side.
// Endpoint: GET /api/cidadao/minha-divida-ativa.
import { useQuery } from '@tanstack/react-query';
import {
  Alert,
  Card,
  DataTable,
  EmptyState,
  Metrica,
  MetricaGrade,
  PageHeader,
  Tag,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { cidadaoApi } from './cidadaoApi';
import type { MinhaDividaAtiva } from './cidadaoApi';
import { formatarData, formatarMoeda, situacaoTagVariant } from './cidadao.helpers';

export function MinhaDividaAtivaPage() {
  const dividas = useQuery({
    queryKey: ['cidadao', 'minha-divida-ativa'],
    queryFn: () => cidadaoApi.minhaDividaAtiva(),
  });

  const total = (dividas.data ?? []).reduce((soma, d) => soma + d.valorAtualizado, 0);

  const colunas: Column<MinhaDividaAtiva>[] = [
    { key: 'tributo', header: 'Tributo', render: (d) => d.tributo, sortAccessor: (d) => d.tributo },
    {
      key: 'inscricao',
      header: 'Inscricao',
      render: (d) => d.numeroCda ?? `Insc. ${d.numeroInscricao}`,
    },
    {
      key: 'dataInscricao',
      header: 'Inscrita em',
      render: (d) => formatarData(d.dataInscricao),
      sortAccessor: (d) => d.dataInscricao,
    },
    {
      key: 'originario',
      header: 'Valor originario',
      align: 'end',
      render: (d) => formatarMoeda(d.valorOriginario),
      sortAccessor: (d) => d.valorOriginario,
    },
    {
      key: 'atualizado',
      header: 'Valor atualizado',
      align: 'end',
      render: (d) => <strong>{formatarMoeda(d.valorAtualizado)}</strong>,
      sortAccessor: (d) => d.valorAtualizado,
    },
    {
      key: 'situacao',
      header: 'Situacao',
      render: (d) => (
        <Tag variant={d.parcelada ? 'success' : situacaoTagVariant(d.situacao)}>
          {d.parcelada ? 'Parcelada' : d.situacao}
        </Tag>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Minha divida ativa"
        description="Inscricoes em Divida Ativa no seu nome, com o valor atualizado ate hoje."
      />

      {dividas.isError && <Alert variant="danger">{errorMessage(dividas.error)}</Alert>}

      {dividas.data && dividas.data.length > 0 && (
        <MetricaGrade>
          <Metrica
            label="Total atualizado"
            valor={formatarMoeda(total)}
            tom="alerta"
            secundario={`${dividas.data.length} inscricao(oes)`}
          />
        </MetricaGrade>
      )}

      <Card>
        <DataTable
          caption="Inscricoes em Divida Ativa"
          columns={colunas}
          rows={dividas.data}
          rowKey={(d) => d.dividaAtivaId}
          loading={dividas.isLoading}
          error={dividas.isError ? errorMessage(dividas.error) : null}
          empty={
            <EmptyState
              icon="fas fa-check-circle"
              title="Voce nao tem divida ativa."
              description="Nenhuma inscricao em Divida Ativa foi encontrada no seu nome."
            />
          }
        />
      </Card>

      <p className="text-down-01 text-gray-60 mt-2">
        O valor atualizado inclui os encargos legais apurados ate a data de hoje. Para o pagamento,
        emita a 2a via em &quot;Meus debitos&quot; ou procure o setor de tributos do municipio.
      </p>
    </>
  );
}
