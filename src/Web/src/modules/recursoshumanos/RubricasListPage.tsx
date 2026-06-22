// Tela de CONSULTA das rubricas (verbas) vigentes numa competência. Padrão-ouro:
// consulta sob demanda (enabled) + QueryState + DataTable, com abertura do formulário
// de criação (mutation). As rubricas alimentam as bases de cálculo da folha (S-1010).
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  Input,
  PageHeader,
  QueryState,
  Select,
  Tag,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { Can } from '../../auth/Can';
import { useRubricasVigentes } from './rubrica.api';
import type { RubricaResumo } from './rubrica.api';
import {
  MESES,
  naturezaRubricaTagVariant,
  PERM_RH_GERENCIAR,
} from './recursosHumanos.helpers';
import { CriarRubricaFormModal } from './CriarRubricaFormModal';
import { RhSubNav } from './RhSubNav';

const ANO_ATUAL = new Date().getFullYear();

function bases(r: RubricaResumo): string {
  const ativas = [
    r.incideInss && 'INSS',
    r.incideRpps && 'RPPS',
    r.incideIrrf && 'IRRF',
    r.incideFgts && 'FGTS',
  ].filter(Boolean);
  return ativas.length > 0 ? ativas.join(', ') : '—';
}

export function RubricasListPage() {
  const [ano, setAno] = useState(String(ANO_ATUAL));
  const [mes, setMes] = useState(String(new Date().getMonth() + 1));
  const [consulta, setConsulta] = useState<{ ano: number; mes: number } | null>(null);
  const [formAberto, setFormAberto] = useState(false);

  const query = useRubricasVigentes(consulta?.ano ?? 0, consulta?.mes ?? 0, consulta !== null);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    const anoNum = Number(ano);
    const mesNum = Number(mes);
    if (!Number.isInteger(anoNum) || !Number.isInteger(mesNum)) return;
    setConsulta({ ano: anoNum, mes: mesNum });
  }

  const columns: Column<RubricaResumo>[] = [
    { key: 'codigo', header: 'Código', render: (r) => r.codigo },
    { key: 'descricao', header: 'Descrição', render: (r) => r.descricao },
    {
      key: 'natureza',
      header: 'Natureza',
      render: (r) => <Tag variant={naturezaRubricaTagVariant(r.natureza)}>{r.natureza}</Tag>,
    },
    { key: 'bases', header: 'Bases', render: (r) => bases(r) },
  ];

  return (
    <>
      <RhSubNav />
      <PageHeader
        title="Rubricas"
        description="Verbas parametrizáveis da folha (S-1010), vigentes na competência consultada."
        actions={
          <Can permission={PERM_RH_GERENCIAR}>
            <Button variant="primary" onClick={() => setFormAberto(true)}>
              <i className="fas fa-plus" aria-hidden="true" /> Nova rubrica
            </Button>
          </Can>
        }
      />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <div className="row align-items-end">
            <div className="col-sm-4 col-md-3">
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
            <div className="col-sm-4 col-md-3">
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
            <div className="col-auto mb-3">
              <Button variant="primary" type="submit" loading={query.isFetching}>
                Consultar
              </Button>
            </div>
          </div>
        </form>
      </Card>

      {consulta === null ? (
        <EmptyState
          icon="fas fa-magnifying-glass"
          title="Selecione uma competência"
          description="Escolha mês e ano e clique em Consultar."
        />
      ) : (
        <QueryState<RubricaResumo[]>
          isLoading={query.isLoading}
          isError={query.isError}
          error={query.error}
          data={query.data}
          empty={
            <EmptyState
              icon="fas fa-list-check"
              title="Nenhuma rubrica vigente"
              description="Não há rubricas vigentes nesta competência. Use Nova rubrica."
            />
          }
        >
          {(rubricas) => (
            <Card>
              <DataTable
                caption={`Rubricas vigentes em ${String(consulta.mes).padStart(2, '0')}/${consulta.ano}`}
                columns={columns}
                rows={rubricas}
                rowKey={(r) => r.id}
              />
            </Card>
          )}
        </QueryState>
      )}

      <CriarRubricaFormModal
        open={formAberto}
        onClose={() => setFormAberto(false)}
        anoInicial={Number(ano)}
        mesInicial={Number(mes)}
      />
    </>
  );
}
