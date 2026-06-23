// Tela de LISTA/CONSULTA de bens patrimoniais. Cobre as queries de "entrada" do agregado:
//   - ListarBensDepreciaveis (lista por competência, com colunas + ordenação via DataTable);
//   - ObterBemPatrimonial (consulta direta por identificador -> navega ao detalhe).
// Inclui a ação de criação (IncorporarBem) via FormModal.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  FormRow,
  Input,
  PageHeader,
  Toolbar,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { errorMessage } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { formatarMoeda } from '../../../i18n/format';
import { useBensDepreciaveis } from './bempatrimonial.api';
import type { BemDepreciavelResumo } from './bempatrimonial.api';
import { competenciaAtual, guidInvalido } from './bemPatrimonial.helpers';
import { BemPatrimonialFormModal } from './BemPatrimonialFormModal';
import { PatrimonioSubNav } from '../PatrimonioSubNav';

export function BemPatrimonialListPage() {
  const navigate = useNavigate();
  const inicial = competenciaAtual();

  // Consulta direta por identificador (ObterBemPatrimonial).
  const [bemId, setBemId] = useState('');
  const [idErro, setIdErro] = useState<string | undefined>();

  // Filtros da lista de depreciáveis (competência).
  const [ano, setAno] = useState(String(inicial.ano));
  const [mes, setMes] = useState(String(inicial.mes));
  const [competencia, setCompetencia] = useState<{ ano: number; mes: number } | null>(null);

  const [formAberto, setFormAberto] = useState(false);

  const query = useBensDepreciaveis(
    competencia?.ano ?? 0,
    competencia?.mes ?? 0,
    competencia !== null,
  );

  function abrirDetalhe(event: FormEvent): void {
    event.preventDefault();
    if (guidInvalido(bemId)) {
      setIdErro('Informe um identificador de bem válido.');
      return;
    }
    setIdErro(undefined);
    navigate(`/patrimonio/bens/${bemId.trim()}`);
  }

  function consultarDepreciaveis(event: FormEvent): void {
    event.preventDefault();
    const anoN = Number(ano);
    const mesN = Number(mes);
    if (!Number.isInteger(anoN) || anoN < 2000 || anoN > 2100) return;
    if (!Number.isInteger(mesN) || mesN < 1 || mesN > 12) return;
    setCompetencia({ ano: anoN, mes: mesN });
  }

  const columns: Column<BemDepreciavelResumo>[] = [
    {
      key: 'tombamento',
      header: 'Tombo',
      sortAccessor: (b) => b.numeroTombamento,
      render: (b) => b.numeroTombamento,
    },
    {
      key: 'valorContabil',
      header: 'Valor contábil',
      align: 'end',
      sortAccessor: (b) => b.valorContabil,
      render: (b) => formatarMoeda(b.valorContabil),
    },
    {
      key: 'valorResidual',
      header: 'Valor residual',
      align: 'end',
      sortAccessor: (b) => b.valorResidual,
      render: (b) => formatarMoeda(b.valorResidual),
    },
    {
      key: 'parcelaMensal',
      header: 'Parcela mensal',
      align: 'end',
      sortAccessor: (b) => b.parcelaMensal,
      render: (b) => formatarMoeda(b.parcelaMensal),
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (b) => (
        <Link className="br-button secondary small" to={`/patrimonio/bens/${b.id}`}>
          Detalhes
        </Link>
      ),
    },
  ];

  return (
    <>
      <PatrimonioSubNav />
      <PageHeader
        eyebrow="Patrimônio"
        title="Bens patrimoniais"
        description="Consulte bens do acervo, acompanhe os depreciáveis por competência e incorpore novos bens."
        actions={
          <Can permission="patrimonio.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Incorporar bem
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <Card className="mb-4" header={<strong>Consultar bem por identificador</strong>}>
        <form className="br-form" onSubmit={abrirDetalhe}>
          <FormRow
            acao={
              <Button variant="secondary" type="submit" disabled={bemId.trim() === ''}>
                Abrir detalhe
              </Button>
            }
          >
            <FormField label="Identificador do bem" required error={idErro}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={bemId}
                  onChange={(e) => setBemId(e.target.value)}
                  placeholder="00000000-0000-0000-0000-000000000000"
                />
              )}
            </FormField>
          </FormRow>
        </form>
      </Card>

      <Card className="mb-4" header={<strong>Bens depreciáveis por competência</strong>}>
        <form className="br-form" onSubmit={consultarDepreciaveis}>
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
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      aria-describedby={describedBy}
                      type="number"
                      min="2000"
                      max="2100"
                      step="1"
                      inputMode="numeric"
                      value={ano}
                      onChange={(e) => setAno(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-sm-6">
                <FormField label="Mês" required>
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      aria-describedby={describedBy}
                      type="number"
                      min="1"
                      max="12"
                      step="1"
                      inputMode="numeric"
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

      {competencia === null ? (
        <EmptyState
          icon="fas fa-magnifying-glass"
          title="Selecione uma competência"
          description="Informe ano e mês e clique em Consultar para listar os bens depreciáveis."
        />
      ) : (
        <DataTable
          caption={`Bens depreciáveis — competência ${String(competencia.mes).padStart(2, '0')}/${competencia.ano}`}
          columns={columns}
          rows={query.data}
          rowKey={(b) => b.id}
          loading={query.isLoading}
          error={query.isError ? errorMessage(query.error) : null}
          empty={
            <EmptyState
              icon="fas fa-circle-check"
              title="Nenhum bem depreciável"
              description="Não há bens depreciáveis para a competência selecionada."
            />
          }
        />
      )}

      <BemPatrimonialFormModal open={formAberto} onClose={() => setFormAberto(false)} />
    </>
  );
}
