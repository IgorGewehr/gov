// Tela de CONSULTA das concessoes por competencia (query ObterConcessoesPorCompetencia).
// Filtros ano/mes -> busca sob demanda (enabled); DataTable com colunas + ordenacao + estados.
// Retorna apenas beneficios Concedida (tenant-scoped) — relatorio de provisao por competencia.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  FormRow,
  Input,
  PageHeader,
  Select,
  Tag,
  errorMessage,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { formatarData, formatarMoeda } from '../../../i18n/format';
import { useConcessoesPorCompetencia } from './beneficio.api';
import type { BeneficioResumo } from './beneficio.api';
import { MES_OPTIONS, situacaoLabel, situacaoTagVariant, tipoLabel } from './beneficio.helpers';
import { AssistenciaSocialSubNav } from '../AssistenciaSocialSubNav';

const hoje = new Date();

export function ConcessoesPorCompetenciaPage() {
  const [ano, setAno] = useState(String(hoje.getFullYear()));
  const [mes, setMes] = useState(String(hoje.getMonth() + 1));
  const [consulta, setConsulta] = useState<{ ano: number; mes: number } | null>(null);

  const query = useConcessoesPorCompetencia(
    consulta?.ano ?? 0,
    consulta?.mes ?? 0,
    consulta !== null,
  );

  function consultar(event: FormEvent): void {
    event.preventDefault();
    const anoNum = Number(ano);
    const mesNum = Number(mes);
    if (Number.isNaN(anoNum) || anoNum < 1900 || Number.isNaN(mesNum) || mesNum < 1 || mesNum > 12)
      return;
    setConsulta({ ano: anoNum, mes: mesNum });
  }

  const columns: Column<BeneficioResumo>[] = [
    {
      key: 'tipo',
      header: 'Tipo',
      sortAccessor: (b) => b.tipo,
      render: (b) => tipoLabel(b.tipo),
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (b) => b.situacao,
      render: (b) => <Tag variant={situacaoTagVariant(b.situacao)}>{situacaoLabel(b.situacao)}</Tag>,
    },
    {
      key: 'familia',
      header: 'Família',
      sortAccessor: (b) => b.familiaId,
      render: (b) => b.familiaId,
    },
    {
      key: 'valor',
      header: 'Valor',
      align: 'end',
      sortAccessor: (b) => b.valor ?? -1,
      render: (b) => (b.valor != null ? formatarMoeda(b.valor) : '—'),
    },
    {
      key: 'decisao',
      header: 'Data da decisão',
      sortAccessor: (b) => b.dataDecisao ?? '',
      render: (b) => formatarData(b.dataDecisao),
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (b) => (
        <Link
          className="br-button tertiary small"
          to={`/assistenciasocial/familias/${b.familiaId}/beneficios/${b.id}`}
        >
          Detalhes
        </Link>
      ),
    },
  ];

  return (
    <>
      <AssistenciaSocialSubNav />
      <PageHeader
        eyebrow="Assistência Social"
        title="Concessões por competência"
        description="Consulte os benefícios concedidos em uma competência (ano/mês)."
      />

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
              <div className="col-sm-6">
                <FormField label="Ano" required>
                  {({ id, describedBy, invalid }) => (
                    <Input
                      id={id}
                      type="number"
                      min="1900"
                      max="9999"
                      step="1"
                      inputMode="numeric"
                      aria-describedby={describedBy}
                      invalid={invalid}
                      value={ano}
                      onChange={(e) => setAno(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-sm-6">
                <FormField label="Mês" required>
                  {({ id, describedBy, invalid }) => (
                    <Select
                      id={id}
                      aria-describedby={describedBy}
                      invalid={invalid}
                      options={MES_OPTIONS}
                      value={mes}
                      onChange={(e) => setMes(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
            </div>
          </FormRow>
        </form>
      </Card>

      {consulta === null ? (
        <EmptyState
          icon="fas fa-calendar-day"
          title="Selecione uma competência"
          description="Informe o ano e o mês e clique em Consultar."
        />
      ) : (
        <DataTable
          caption={`Concessões da competência ${String(consulta.mes).padStart(2, '0')}/${consulta.ano}`}
          columns={columns}
          rows={query.data}
          rowKey={(b) => b.id}
          loading={query.isLoading}
          error={query.isError ? errorMessage(query.error) : null}
          empty={
            <EmptyState
              icon="fas fa-folder-open"
              title="Nenhuma concessão na competência"
              description="Não há benefícios concedidos para o período informado."
            />
          }
        />
      )}
    </>
  );
}
