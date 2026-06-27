// Detalhe de uma remessa SICAP-AP: cabeçalho (órgão/lote/versão), atos de admissão (CPF mascarado) e
// ações conforme a situação — incluir ato a partir de um servidor (Aberta), gerar/baixar o arquivo de
// importação (Aberta → Gerada) e marcar a transmissão com o protocolo (Gerada → Transmitida).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  Input,
  Modal,
  PageHeader,
  QueryState,
  Tag,
  Toolbar,
  useToast,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { Can } from '../../auth/Can';
import { ApiError } from '../../api/problemDetails';
import { formatarData } from '../../i18n/format';
import {
  useRemessaSicap,
  useAdicionarAtoSicap,
  useGerarRemessaSicap,
  useTransmitirRemessaSicap,
} from './api';
import type { AtoAdmissaoItem, RemessaSicapDetalhe } from './api';
import { PERM_RH_GERENCIAR, situacaoRemessaSicapTagVariant } from './recursosHumanos.helpers';
import { RhSubNav } from './RhSubNav';

function AdicionarAtoModal({
  remessaId,
  open,
  onClose,
}: {
  remessaId: string;
  open: boolean;
  onClose: () => void;
}) {
  const toast = useToast();
  const mutation = useAdicionarAtoSicap(remessaId);
  const [servidorId, setServidorId] = useState('');
  const [classificacao, setClassificacao] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (servidorId.trim() === '') {
      setErro('Informe o identificador do servidor.');
      return;
    }
    setErro(undefined);

    mutation.mutate(
      {
        servidorId: servidorId.trim(),
        classificacaoConcurso: classificacao.trim() ? Number(classificacao) : null,
      },
      {
        onSuccess: () => {
          toast.success('Ato incluído na remessa.', 'Sucesso');
          setServidorId('');
          setClassificacao('');
          onClose();
        },
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível incluir o ato.',
          ),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Incluir ato de admissão"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-ato" loading={mutation.isPending}>
            Incluir
          </Button>
        </>
      }
    >
      <form id="form-ato" className="br-form" onSubmit={submeter} noValidate>
        <FormField
          label="Identificador do servidor"
          required
          error={erro}
          help="O título, regime, datas e cargo são derivados do cadastro do servidor."
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={servidorId}
              onChange={(e) => setServidorId(e.target.value)}
              placeholder="GUID do servidor"
            />
          )}
        </FormField>
        <FormField
          label="Classificação no concurso"
          help="Obrigatória quando a admissão for por concurso público (título 01)."
        >
          {({ id }) => (
            <Input
              id={id}
              type="number"
              min="1"
              inputMode="numeric"
              value={classificacao}
              onChange={(e) => setClassificacao(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

function TransmitirModal({
  remessaId,
  open,
  onClose,
}: {
  remessaId: string;
  open: boolean;
  onClose: () => void;
}) {
  const toast = useToast();
  const mutation = useTransmitirRemessaSicap(remessaId);
  const [protocolo, setProtocolo] = useState('');

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (protocolo.trim() === '') return;
    mutation.mutate(protocolo.trim(), {
      onSuccess: () => {
        toast.success('Remessa marcada como transmitida.', 'Sucesso');
        setProtocolo('');
        onClose();
      },
      onError: (error) =>
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível registrar a transmissão.',
        ),
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Registrar transmissão"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-transmissao" loading={mutation.isPending}>
            Confirmar
          </Button>
        </>
      }
    >
      <form id="form-transmissao" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Protocolo do TCE-RS" required help="Transmissão real ao SIAPESweb na fase de integrações.">
          {({ id }) => (
            <Input id={id} value={protocolo} onChange={(e) => setProtocolo(e.target.value)} />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

export function RemessaSicapDetailPage() {
  const { remessaId = '' } = useParams<{ remessaId: string }>();
  const query = useRemessaSicap(remessaId);
  const gerar = useGerarRemessaSicap(remessaId);
  const toast = useToast();
  const [adicionando, setAdicionando] = useState(false);
  const [transmitindo, setTransmitindo] = useState(false);

  function gerarArquivo(): void {
    gerar.mutate(undefined, {
      onSuccess: () => toast.success('Arquivo de importação gerado e baixado.', 'Sucesso'),
      onError: (error) =>
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível gerar a remessa.',
        ),
    });
  }

  const columns: Column<AtoAdmissaoItem>[] = [
    { key: 'identificador', header: 'Identificador', render: (a) => a.identificadorAto },
    { key: 'nome', header: 'Servidor', render: (a) => a.nome },
    { key: 'cpf', header: 'CPF', render: (a) => a.cpfMascarado },
    { key: 'titulo', header: 'Título', render: (a) => a.tipoAto },
    { key: 'regime', header: 'Regime', render: (a) => a.regime },
    { key: 'cargo', header: 'Cargo', render: (a) => a.descricaoCargo },
    { key: 'dataAto', header: 'Data do ato', render: (a) => formatarData(a.dataAto) },
  ];

  return (
    <>
      <RhSubNav />
      <PageHeader
        eyebrow="Recursos Humanos"
        title="Remessa SICAP-AP"
        description="Atos de admissão do lote (leiaute estadual 57 posições). Inclua atos, gere o arquivo e registre a transmissão ao TCE-RS."
        actions={
          <Link className="br-button secondary" to="/recursoshumanos/sicap-pessoal">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<RemessaSicapDetalhe>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
      >
        {(remessa) => {
          const aberta = remessa.resumo.situacao === 'Aberta';
          const gerada = remessa.resumo.situacao === 'Gerada';
          return (
            <>
              <Card className="mb-4">
                <dl className="row mb-0">
                  <dt className="col-sm-3">Órgão / lote</dt>
                  <dd className="col-sm-9">
                    {String(remessa.resumo.codigoOrgao).padStart(6, '0')} /{' '}
                    {String(remessa.resumo.sequencialLote).padStart(4, '0')}
                  </dd>
                  <dt className="col-sm-3">Situação</dt>
                  <dd className="col-sm-9">
                    <Tag variant={situacaoRemessaSicapTagVariant(remessa.resumo.situacao)}>
                      {remessa.resumo.situacao}
                    </Tag>
                  </dd>
                  <dt className="col-sm-3">Versão do leiaute</dt>
                  <dd className="col-sm-9">{remessa.versaoLeiaute} posições</dd>
                  <dt className="col-sm-3">Geração do lote</dt>
                  <dd className="col-sm-9">{formatarData(remessa.resumo.dataGeracaoLote)}</dd>
                  {remessa.resumo.protocolo && (
                    <>
                      <dt className="col-sm-3">Protocolo</dt>
                      <dd className="col-sm-9">{remessa.resumo.protocolo}</dd>
                    </>
                  )}
                </dl>
              </Card>

              <Can permission={PERM_RH_GERENCIAR}>
                <Toolbar className="mb-4">
                  {aberta && (
                    <Button variant="secondary" onClick={() => setAdicionando(true)}>
                      <i className="fas fa-user-plus" aria-hidden="true" /> Incluir ato
                    </Button>
                  )}
                  {aberta && (
                    <Button
                      variant="primary"
                      onClick={gerarArquivo}
                      loading={gerar.isPending}
                      disabled={remessa.atos.length === 0}
                    >
                      <i className="fas fa-file-download" aria-hidden="true" /> Gerar arquivo
                    </Button>
                  )}
                  {gerada && (
                    <Button variant="primary" onClick={() => setTransmitindo(true)}>
                      <i className="fas fa-paper-plane" aria-hidden="true" /> Registrar transmissão
                    </Button>
                  )}
                </Toolbar>
              </Can>

              <DataTable
                caption="Atos de admissão da remessa"
                columns={columns}
                rows={remessa.atos}
                rowKey={(a) => a.id}
                empty={
                  <EmptyState
                    icon="fas fa-user-plus"
                    title="Nenhum ato na remessa"
                    description="Inclua atos de admissão a partir dos servidores para gerar o arquivo."
                  />
                }
              />

              <AdicionarAtoModal
                remessaId={remessaId}
                open={adicionando}
                onClose={() => setAdicionando(false)}
              />
              <TransmitirModal
                remessaId={remessaId}
                open={transmitindo}
                onClose={() => setTransmitindo(false)}
              />
            </>
          );
        }}
      </QueryState>
    </>
  );
}
