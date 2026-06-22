// Tela de OPERACAO do ponto de um servidor (Portaria MTP 671/2021): define a jornada,
// registra/lista as marcacoes da sessao (cada batida devolve o NSR sequencial) e apura a
// competencia exibindo o ESPELHO (trabalhado/extras/faltas/banco de horas) com fechamento.
//
// O contrato M5 nao expoe GET de marcacoes/apuracao: a lista de marcacoes reflete o que foi
// registrado nesta sessao e o espelho reflete o retorno da apuracao. A navegacao traz o nome
// do servidor via router state (a partir da PontoListPage).
import { useState } from 'react';
import { Link, useLocation, useParams } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  Input,
  PageHeader,
  Select,
  Tag,
  useToast,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { Can } from '../../auth/Can';
import { ApiError } from '../../api/problemDetails';
import { formatarDataHora } from '../../i18n/format';
import { useApurarJornada, useFecharApuracao } from './api';
import type { ApuracaoResultado } from './api';
import { RegistrarMarcacaoFormModal } from './RegistrarMarcacaoFormModal';
import type { MarcacaoSessao } from './RegistrarMarcacaoFormModal';
import { DefinirJornadaFormModal } from './DefinirJornadaFormModal';
import { EspelhoPonto } from './EspelhoPonto';
import {
  MESES,
  ORIGENS_REP,
  PERM_RH_GERENCIAR,
  SENTIDOS_MARCACAO,
} from './recursosHumanos.helpers';

const ANO_ATUAL = new Date().getFullYear();

interface PontoLocationState {
  nome?: string;
  matricula?: string;
}

function rotulo(opcoes: { value: string; label: string }[], valor: number): string {
  return opcoes.find((o) => o.value === String(valor))?.label ?? String(valor);
}


export function PontoServidorPage() {
  const { servidorId = '' } = useParams<{ servidorId: string }>();
  const location = useLocation();
  const state = (location.state ?? {}) as PontoLocationState;
  const nome = state.nome ?? 'Servidor';
  const toast = useToast();

  const [jornadaAberta, setJornadaAberta] = useState(false);
  const [marcacaoAberta, setMarcacaoAberta] = useState(false);
  const [marcacoes, setMarcacoes] = useState<MarcacaoSessao[]>([]);

  const [ano, setAno] = useState(String(ANO_ATUAL));
  const [mes, setMes] = useState(String(new Date().getMonth() + 1));
  const [apuracao, setApuracao] = useState<ApuracaoResultado | null>(null);
  const [fechada, setFechada] = useState(false);

  const apurar = useApurarJornada();
  const fechar = useFecharApuracao();

  function executarApuracao(): void {
    apurar.mutate(
      { servidorId, ano: Number(ano), mes: Number(mes) },
      {
        onSuccess: (resultado) => {
          setApuracao(resultado);
          setFechada(false);
          toast.success('Competência apurada.', 'Sucesso');
        },
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível apurar a competência.',
          ),
      },
    );
  }

  function executarFechamento(): void {
    if (!apuracao) return;
    fechar.mutate(apuracao.id, {
      onSuccess: () => {
        setFechada(true);
        toast.success('Apuração fechada (espelho congelado).', 'Sucesso');
      },
      onError: (error) =>
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível fechar a apuração.',
        ),
    });
  }

  const colunasMarcacoes: Column<MarcacaoSessao>[] = [
    {
      key: 'nsr',
      header: 'NSR',
      sortAccessor: (m) => m.nsr,
      render: (m) => <span className="text-semi-bold">{m.nsr}</span>,
    },
    { key: 'dataHora', header: 'Data/hora', render: (m) => formatarDataHora(m.dataHora) },
    {
      key: 'sentido',
      header: 'Sentido',
      render: (m) => (
        <Tag variant={m.sentido === 1 ? 'success' : 'info'}>
          {rotulo(SENTIDOS_MARCACAO, m.sentido)}
        </Tag>
      ),
    },
    { key: 'origem', header: 'Origem', render: (m) => rotulo(ORIGENS_REP, m.origem) },
  ];

  return (
    <>
      <PageHeader
        title={`Ponto — ${nome}`}
        description={state.matricula ? `Matrícula ${state.matricula}` : undefined}
        actions={
          <Link className="br-button secondary" to="/recursoshumanos/ponto">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      {/* (1) JORNADA */}
      <Card className="mb-4" header={<strong>Jornada do servidor</strong>}>
        <p className="text-down-01 text-gray-60 mt-0">
          Defina a carga diária, o intervalo, a tolerância e o regime. A jornada vigente
          anterior, se houver, é substituída (o histórico é preservado).
        </p>
        <Can permission={PERM_RH_GERENCIAR}>
          <Button variant="primary" onClick={() => setJornadaAberta(true)}>
            <i className="fas fa-business-time" aria-hidden="true" /> Definir jornada
          </Button>
        </Can>
      </Card>

      {/* (2) MARCACOES */}
      <Card
        className="mb-4"
        header={<strong>Marcações (NSR · origem REP)</strong>}
      >
        <Can permission={PERM_RH_GERENCIAR}>
          <div className="mb-3">
            <Button variant="secondary" onClick={() => setMarcacaoAberta(true)}>
              <i className="fas fa-clock" aria-hidden="true" /> Registrar marcação
            </Button>
          </div>
        </Can>
        <DataTable
          caption={`Marcações registradas nesta sessão — ${nome}`}
          columns={colunasMarcacoes}
          rows={marcacoes}
          rowKey={(m) => String(m.nsr)}
          empty={
            <EmptyState
              icon="fas fa-clock"
              title="Nenhuma marcação registrada"
              description="Registre marcações para compor o AFD. O NSR é sequencial por órgão."
            />
          }
        />
      </Card>

      {/* (3) APURACAO + ESPELHO */}
      <Card header={<strong>Apuração da competência</strong>}>
        <div className="row align-items-end">
          <div className="col-sm-6 col-md-3">
            <FormField label="Mês">
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
          <div className="col-sm-6 col-md-3">
            <FormField label="Ano">
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
            <Can permission={PERM_RH_GERENCIAR}>
              <Button variant="primary" onClick={executarApuracao} loading={apurar.isPending}>
                <i className="fas fa-calculator" aria-hidden="true" /> Apurar competência
              </Button>
            </Can>
          </div>
        </div>

        {apuracao === null ? (
          <EmptyState
            icon="fas fa-clipboard-list"
            title="Sem apuração nesta sessão"
            description="Escolha a competência e clique em Apurar competência para ver o espelho."
          />
        ) : (
          <EspelhoPonto
            apuracao={apuracao}
            fechada={fechada}
            fechando={fechar.isPending}
            onFechar={executarFechamento}
          />
        )}
      </Card>

      <DefinirJornadaFormModal
        open={jornadaAberta}
        onClose={() => setJornadaAberta(false)}
        servidorId={servidorId}
        servidorNome={nome}
      />
      <RegistrarMarcacaoFormModal
        open={marcacaoAberta}
        onClose={() => setMarcacaoAberta(false)}
        servidorId={servidorId}
        servidorNome={nome}
        onRegistrada={(m) => setMarcacoes((atual) => [...atual, m].sort((a, b) => a.nsr - b.nsr))}
      />
    </>
  );
}

