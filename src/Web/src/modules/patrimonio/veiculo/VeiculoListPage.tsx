// Tela principal da FROTA. Reúne as consultas de Veiculo que não dependem de um id
// específico, em abas:
//   - Veículo: busca por identificador -> link para a DetailPage (ObterVeiculo);
//   - Multas pendentes: ListarMultasPendentes (todos os veículos do tenant);
//   - Licenciamentos pendentes: ListarLicenciamentosPendentes por exercício.
// Botão "Incorporar veículo" abre o VeiculoFormModal (IncorporarVeiculo).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  errorMessage,
  FormField,
  FormRow,
  Input,
  PageHeader,
  Tag,
  Toolbar,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { formatarData, formatarMoeda } from '../../../i18n/format';
import { useLicenciamentosPendentes, useMultasPendentes } from './veiculo.api';
import type { LicenciamentoResumo, MultaResumo } from './veiculo.api';
import { VeiculoFormModal } from './VeiculoFormModal';
import { PatrimonioSubNav } from '../PatrimonioSubNav';

type Aba = 'veiculo' | 'multas' | 'licenciamentos';

function AbaVeiculo() {
  const navigate = useNavigate();
  const [veiculoId, setVeiculoId] = useState('');

  function abrir(event: FormEvent): void {
    event.preventDefault();
    const id = veiculoId.trim();
    if (id !== '') navigate(`/patrimonio/frota/veiculos/${id}`);
  }

  return (
    <Card>
      <form className="br-form" onSubmit={abrir}>
        <FormRow
          acao={
            <Button variant="primary" type="submit" disabled={veiculoId.trim() === ''}>
              Abrir veículo
            </Button>
          }
        >
          <FormField label="Identificador do veículo" required>
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                aria-describedby={describedBy}
                invalid={invalid}
                value={veiculoId}
                onChange={(e) => setVeiculoId(e.target.value)}
                placeholder="00000000-0000-0000-0000-000000000000"
              />
            )}
          </FormField>
        </FormRow>
      </form>
      <EmptyState
        icon="fas fa-truck"
        title="Consulte um veículo da frota"
        description="Informe o identificador do veículo e clique em Abrir veículo, ou incorpore um novo veículo."
      />
    </Card>
  );
}

function AbaMultas() {
  const query = useMultasPendentes();

  const columns: Column<MultaResumo>[] = [
    {
      key: 'placa',
      header: 'Placa',
      sortAccessor: (m) => m.placa,
      render: (m) => (
        <Link to={`/patrimonio/frota/veiculos/${m.veiculoId}`}>{m.placa}</Link>
      ),
    },
    { key: 'codigo', header: 'Código CTB', sortAccessor: (m) => m.codigoInfracaoCtb, render: (m) => m.codigoInfracaoCtb },
    { key: 'valor', header: 'Valor', align: 'end', sortAccessor: (m) => m.valor, render: (m) => formatarMoeda(m.valor) },
    {
      key: 'data',
      header: 'Data da infração',
      sortAccessor: (m) => m.dataInfracao,
      render: (m) => formatarData(m.dataInfracao),
    },
    {
      key: 'status',
      header: 'Situação',
      render: () => <Tag variant="warning">Pendente</Tag>,
    },
  ];

  return (
    <DataTable
      caption="Multas pendentes ou em recurso de todos os veículos"
      columns={columns}
      rows={query.data}
      rowKey={(m) => m.id}
      loading={query.isLoading}
      error={query.isError ? errorMessage(query.error) : null}
      empty={
        <EmptyState
          icon="fas fa-circle-check"
          title="Nenhuma multa pendente"
          description="Não há multas pendentes ou em recurso na frota."
        />
      }
    />
  );
}

function AbaLicenciamentos() {
  const anoCorrente = new Date().getFullYear();
  const [exercicio, setExercicio] = useState(anoCorrente);
  const [consultado, setConsultado] = useState(anoCorrente);

  const query = useLicenciamentosPendentes(consultado);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    setConsultado(exercicio);
  }

  const columns: Column<LicenciamentoResumo>[] = [
    {
      key: 'placa',
      header: 'Placa',
      sortAccessor: (l) => l.placa,
      render: (l) => <Link to={`/patrimonio/frota/veiculos/${l.veiculoId}`}>{l.placa}</Link>,
    },
    { key: 'exercicio', header: 'Exercício', sortAccessor: (l) => l.exercicio, render: (l) => l.exercicio },
    { key: 'ipva', header: 'IPVA', align: 'end', sortAccessor: (l) => l.valorIpva, render: (l) => formatarMoeda(l.valorIpva) },
    { key: 'situacao', header: 'Situação', render: (l) => <Tag variant="warning">{l.situacao}</Tag> },
  ];

  return (
    <>
      <form className="br-form mb-3" onSubmit={consultar}>
        <FormRow
          acao={
            <Button variant="primary" type="submit" loading={query.isFetching}>
              Consultar
            </Button>
          }
        >
          <FormField label="Exercício" required>
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                type="number"
                min="1900"
                step="1"
                inputMode="numeric"
                aria-describedby={describedBy}
                invalid={invalid}
                value={String(exercicio)}
                onChange={(e) => setExercicio(Number(e.target.value))}
              />
            )}
          </FormField>
        </FormRow>
      </form>
      <DataTable
        caption={`Veículos sem licenciamento Regular no exercício ${consultado}`}
        columns={columns}
        rows={query.data}
        rowKey={(l) => l.veiculoId}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-circle-check"
            title="Nenhum licenciamento pendente"
            description={`Todos os veículos estão regulares no exercício ${consultado}.`}
          />
        }
      />
    </>
  );
}

export function VeiculoListPage() {
  const [aba, setAba] = useState<Aba>('veiculo');
  const [formAberto, setFormAberto] = useState(false);
  const navigate = useNavigate();

  return (
    <>
      <PatrimonioSubNav />
      <PageHeader
        eyebrow="Patrimônio"
        title="Frota"
        description="Gestão operacional da frota: veículos, abastecimento, manutenção, multas e licenciamento."
        actions={
          <Can permission="patrimonio.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Incorporar veículo
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <nav className="br-tab mb-4" aria-label="Consultas da frota">
        <ul className="tab-nav" role="tablist">
          <li className={`tab-item${aba === 'veiculo' ? ' is-active' : ''}`} role="presentation">
            <button
              type="button"
              role="tab"
              id="tab-veiculo"
              aria-controls="painel-veiculo"
              aria-selected={aba === 'veiculo'}
              onClick={() => setAba('veiculo')}
            >
              <span className="name">Veículo</span>
            </button>
          </li>
          <li className={`tab-item${aba === 'multas' ? ' is-active' : ''}`} role="presentation">
            <button
              type="button"
              role="tab"
              id="tab-multas"
              aria-controls="painel-multas"
              aria-selected={aba === 'multas'}
              onClick={() => setAba('multas')}
            >
              <span className="name">Multas pendentes</span>
            </button>
          </li>
          <li className={`tab-item${aba === 'licenciamentos' ? ' is-active' : ''}`} role="presentation">
            <button
              type="button"
              role="tab"
              id="tab-licenciamentos"
              aria-controls="painel-licenciamentos"
              aria-selected={aba === 'licenciamentos'}
              onClick={() => setAba('licenciamentos')}
            >
              <span className="name">Licenciamentos pendentes</span>
            </button>
          </li>
        </ul>
      </nav>

      {aba === 'veiculo' && (
        <div role="tabpanel" id="painel-veiculo" aria-labelledby="tab-veiculo">
          <AbaVeiculo />
        </div>
      )}
      {aba === 'multas' && (
        <div role="tabpanel" id="painel-multas" aria-labelledby="tab-multas">
          <AbaMultas />
        </div>
      )}
      {aba === 'licenciamentos' && (
        <div role="tabpanel" id="painel-licenciamentos" aria-labelledby="tab-licenciamentos">
          <AbaLicenciamentos />
        </div>
      )}

      <VeiculoFormModal
        open={formAberto}
        onClose={() => setFormAberto(false)}
        onCriado={(veiculoId) => navigate(`/patrimonio/frota/veiculos/${veiculoId}`)}
      />
    </>
  );
}
