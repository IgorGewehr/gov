// Tela de ENTRADA do Ponto Eletronico (Portaria MTP 671/2021). Lista os servidores ativos
// para operar a jornada/marcacoes/apuracao de cada um (PontoServidorPage) e concentra o
// download dos arquivos posicionais AFD (periodo) e AEJ (competencia) via Blob, como na
// remessa TCE-RS. Padrao-ouro: DataTable + estados + Card de operacoes + Toast.
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
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
  useToast,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useServidoresAtivos, baixarAfd, baixarAej } from './api';
import type { ServidorResumo } from './api';
import { MESES, situacaoServidorTagVariant } from './recursosHumanos.helpers';
import { RhSubNav } from './RhSubNav';

const ANO_ATUAL = new Date().getFullYear();
const HOJE = new Date().toISOString().slice(0, 10);
const PRIMEIRO_DIA_MES = `${HOJE.slice(0, 7)}-01`;

export function PontoListPage() {
  const navigate = useNavigate();
  const toast = useToast();
  const query = useServidoresAtivos();

  // Estado dos downloads de arquivos posicionais.
  const [inicio, setInicio] = useState(PRIMEIRO_DIA_MES);
  const [fim, setFim] = useState(HOJE);
  const [ano, setAno] = useState(String(ANO_ATUAL));
  const [mes, setMes] = useState(String(new Date().getMonth() + 1));
  const [baixandoAfd, setBaixandoAfd] = useState(false);
  const [baixandoAej, setBaixandoAej] = useState(false);

  async function gerarAfd(): Promise<void> {
    setBaixandoAfd(true);
    try {
      await baixarAfd(inicio, fim);
      toast.success('AFD gerado e baixado.', 'Sucesso');
    } catch (error) {
      toast.error(
        error instanceof ApiError ? error.userMessage : 'Não foi possível gerar o AFD.',
      );
    } finally {
      setBaixandoAfd(false);
    }
  }

  async function gerarAej(): Promise<void> {
    setBaixandoAej(true);
    try {
      await baixarAej(Number(ano), Number(mes));
      toast.success('AEJ gerado e baixado.', 'Sucesso');
    } catch (error) {
      toast.error(
        error instanceof ApiError ? error.userMessage : 'Não foi possível gerar o AEJ.',
      );
    } finally {
      setBaixandoAej(false);
    }
  }

  const columns: Column<ServidorResumo>[] = [
    {
      key: 'matricula',
      header: 'Matrícula',
      sortAccessor: (s) => s.matricula,
      render: (s) => <span className="text-semi-bold">{s.matricula}</span>,
    },
    {
      key: 'nome',
      header: 'Servidor',
      sortAccessor: (s) => s.nomeServidor,
      render: (s) => s.nomeServidor,
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (s) => s.situacao,
      render: (s) => <Tag variant={situacaoServidorTagVariant(s.situacao)}>{s.situacao}</Tag>,
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (s) => (
        <Button
          variant="secondary"
          size="sm"
          onClick={() =>
            navigate(`/recursoshumanos/ponto/${s.id}`, {
              state: { nome: s.nomeServidor, matricula: s.matricula },
            })
          }
        >
          Operar ponto
        </Button>
      ),
    },
  ];

  return (
    <>
      <RhSubNav />
      <PageHeader
        eyebrow="Recursos Humanos"
        title="Ponto Eletrônico"
        description="Jornada, marcações e apuração por servidor (Portaria MTP 671/2021)."
      />

      <Card className="mb-4" header={<strong>Arquivos do REP (Portaria 671/2021)</strong>}>
        <p className="text-down-01 text-gray-60 mt-0">
          Gere e baixe os arquivos posicionais assinados: o AFD (Arquivo Fonte de Dados) por
          período e o AEJ (Arquivo Eletrônico de Jornada) por competência.
        </p>
        <FormRow
          acao={
            <Button variant="secondary" onClick={gerarAfd} loading={baixandoAfd}>
              <i className="fas fa-download" aria-hidden="true" /> Gerar e baixar AFD
            </Button>
          }
        >
          <div className="row">
            <div className="col-sm-6">
              <FormField label="AFD — início">
                {({ id, describedBy }) => (
                  <Input
                    id={id}
                    type="date"
                    aria-describedby={describedBy}
                    value={inicio}
                    onChange={(e) => setInicio(e.target.value)}
                  />
                )}
              </FormField>
            </div>
            <div className="col-sm-6">
              <FormField label="AFD — fim">
                {({ id, describedBy }) => (
                  <Input
                    id={id}
                    type="date"
                    aria-describedby={describedBy}
                    value={fim}
                    onChange={(e) => setFim(e.target.value)}
                  />
                )}
              </FormField>
            </div>
          </div>
        </FormRow>
        <FormRow
          acao={
            <Button variant="secondary" onClick={gerarAej} loading={baixandoAej}>
              <i className="fas fa-download" aria-hidden="true" /> Gerar e baixar AEJ
            </Button>
          }
        >
          <div className="row">
            <div className="col-sm-6">
              <FormField label="AEJ — mês">
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
              <FormField label="AEJ — ano">
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
      </Card>

      <DataTable
        caption="Servidores ativos para operação de ponto"
        columns={columns}
        rows={query.data}
        rowKey={(s) => s.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-users"
            title="Nenhum servidor ativo"
            description="Admita servidores no quadro de pessoal para operar o ponto."
          />
        }
      />
    </>
  );
}
