// Tela principal da FROTA. Reúne as consultas de Veiculo que não dependem de um id
// específico, em abas:
//   - Veículos: LISTA NAVEGÁVEL (Onda 0) com busca por descrição/placa/RENAVAM +
//     filtro de situação, paginada (GET /patrimonio/veiculos) -> link para o detalhe;
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
  Select,
  Tag,
  Toolbar,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { formatarData, formatarMoeda } from '../../../i18n/format';
import { useLicenciamentosPendentes, useMultasPendentes, useVeiculosLista } from './veiculo.api';
import type {
  LicenciamentoResumo,
  MultaResumo,
  SituacaoVeiculo,
  VeiculoItemLista,
} from './veiculo.api';
import { SITUACAO_VEICULO_OPCOES, situacaoVeiculoLabel, situacaoVeiculoTagVariant } from './veiculo.helpers';
import { VeiculoFormModal } from './VeiculoFormModal';
import { PatrimonioSubNav } from '../PatrimonioSubNav';
import { Paginacao } from '../shared/Paginacao';
import { TAMANHO_PAGINA_PADRAO } from '../shared/paginacaoTipos';

type Aba = 'veiculo' | 'multas' | 'licenciamentos';

function AbaVeiculo() {
  const [termoInput, setTermoInput] = useState('');
  const [termo, setTermo] = useState('');
  const [situacao, setSituacao] = useState('');
  const [pagina, setPagina] = useState(1);

  const query = useVeiculosLista({
    termo: termo || undefined,
    situacao: situacao === '' ? undefined : (situacao as SituacaoVeiculo),
    pagina,
    tamanho: TAMANHO_PAGINA_PADRAO,
  });

  function buscar(event: FormEvent): void {
    event.preventDefault();
    setTermo(termoInput.trim());
    setPagina(1);
  }

  const columns: Column<VeiculoItemLista>[] = [
    {
      key: 'placa',
      header: 'Placa',
      sortAccessor: (v) => v.placa,
      render: (v) => <Link to={`/patrimonio/frota/veiculos/${v.id}`}>{v.placa}</Link>,
    },
    { key: 'renavam', header: 'RENAVAM', sortAccessor: (v) => v.renavam, render: (v) => v.renavam },
    {
      key: 'descricao',
      header: 'Descrição',
      sortAccessor: (v) => v.descricao,
      render: (v) => v.descricao,
    },
    {
      key: 'odometro',
      header: 'Odômetro',
      align: 'end',
      sortAccessor: (v) => v.odometro,
      render: (v) => v.odometro,
    },
    {
      key: 'valorContabil',
      header: 'Valor contábil',
      align: 'end',
      sortAccessor: (v) => v.valorContabil,
      render: (v) => formatarMoeda(v.valorContabil),
    },
    {
      key: 'situacao',
      header: 'Situação',
      render: (v) => (
        <Tag variant={situacaoVeiculoTagVariant(v.situacao)}>{situacaoVeiculoLabel(v.situacao)}</Tag>
      ),
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (v) => (
        <Link className="br-button secondary small" to={`/patrimonio/frota/veiculos/${v.id}`}>
          Detalhes
        </Link>
      ),
    },
  ];

  return (
    <Card>
      <form className="br-form mb-3" onSubmit={buscar}>
        <FormRow
          acao={
            <Button variant="primary" type="submit" loading={query.isFetching}>
              <i className="fas fa-magnifying-glass" aria-hidden="true" /> Buscar
            </Button>
          }
        >
          <div className="row">
            <div className="col-sm-8">
              <FormField label="Descrição, placa ou RENAVAM">
                {({ id, describedBy }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    value={termoInput}
                    onChange={(e) => setTermoInput(e.target.value)}
                    placeholder="Trecho da descrição, placa ou RENAVAM"
                  />
                )}
              </FormField>
            </div>
            <div className="col-sm-4">
              <FormField label="Situação">
                {({ id, describedBy }) => (
                  <Select
                    id={id}
                    aria-describedby={describedBy}
                    options={SITUACAO_VEICULO_OPCOES}
                    placeholder="Todas"
                    value={situacao}
                    onChange={(e) => {
                      setSituacao(e.target.value);
                      setPagina(1);
                    }}
                  />
                )}
              </FormField>
            </div>
          </div>
        </FormRow>
      </form>

      <DataTable
        caption="Veículos da frota"
        columns={columns}
        rows={query.data?.itens}
        rowKey={(v) => v.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-truck"
            title="Nenhum veículo encontrado"
            description="Ajuste o termo de busca ou o filtro de situação para localizar veículos da frota."
          />
        }
      />

      {query.data && query.data.total > 0 && (
        <div className="mt-3">
          <Paginacao
            pagina={query.data.pagina}
            tamanho={query.data.tamanho}
            total={query.data.total}
            onPaginaChange={setPagina}
            carregando={query.isFetching}
          />
        </div>
      )}
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
              <span className="name">Veículos</span>
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
