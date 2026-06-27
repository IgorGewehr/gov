// SAUDE E SEGURANCA DO TRABALHO (SST) de um servidor: ASO (S-2220/PCMSO), exposicao a agentes nocivos
// (S-2240/PPP) e CAT (S-2210). Cada registro gera, sob demanda, o evento eSocial nao-periodico. A pagina
// exibe a ficha de saude ocupacional, os registros ambientais e as CAT, com geracao do evento por linha.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { useParams, useLocation } from 'react-router-dom';
import {
  Button,
  CardSecao,
  DataTable,
  EmptyState,
  FormField,
  FormRow,
  Input,
  Modal,
  PageHeader,
  Select,
  Tag,
  Toolbar,
  useToast,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { Can } from '../../auth/Can';
import { ApiError } from '../../api/problemDetails';
import { formatarData, formatarDataHora } from '../../i18n/format';
import {
  useExamesOcupacionais,
  useExposicoes,
  useComunicacoesAcidente,
  useRegistrarExame,
  useComunicarAcidente,
  useGerarS2210,
  useGerarS2220,
  useGerarS2240,
} from './api';
import type {
  ExameOcupacionalView,
  ExposicaoAgenteNocivoView,
  ComunicacaoAcidenteView,
} from './api';
import { PERM_RH_GERENCIAR } from './recursosHumanos.helpers';

const TIPOS_EXAME = [
  { value: '0', label: 'Admissional' },
  { value: '1', label: 'Periodico' },
  { value: '2', label: 'Retorno ao trabalho' },
  { value: '3', label: 'Mudanca de risco' },
  { value: '4', label: 'Monitoracao' },
  { value: '9', label: 'Demissional' },
];

const TIPOS_CAT = [
  { value: '1', label: 'Inicial' },
  { value: '2', label: 'Reabertura' },
  { value: '3', label: 'Comunicacao de obito' },
];

const TIPOS_ACIDENTE = [
  { value: '1', label: 'Tipico' },
  { value: '2', label: 'Doenca ocupacional' },
  { value: '3', label: 'Trajeto' },
];

function RegistrarExameModal({
  open,
  onClose,
  servidorId,
}: {
  open: boolean;
  onClose: () => void;
  servidorId: string;
}) {
  const toast = useToast();
  const mutation = useRegistrarExame();
  const [tipo, setTipo] = useState('1');
  const [dataExame, setDataExame] = useState('');
  const [resultado, setResultado] = useState('1');
  const [medicoNome, setMedicoNome] = useState('');
  const [medicoCrm, setMedicoCrm] = useState('');
  const [medicoUf, setMedicoUf] = useState('');
  const [proximo, setProximo] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (!dataExame || !medicoNome.trim() || !medicoCrm.trim() || medicoUf.trim().length !== 2) {
      setErro('Preencha a data, o medico (nome/CRM) e a UF (2 letras).');
      return;
    }
    setErro(undefined);
    mutation.mutate(
      {
        servidorId,
        tipo: Number(tipo),
        dataExame,
        resultado: Number(resultado),
        medicoNome: medicoNome.trim(),
        medicoNrCrm: medicoCrm.trim(),
        medicoUfCrm: medicoUf.trim().toUpperCase(),
        dataProximoExame: proximo || null,
      },
      {
        onSuccess: () => {
          toast.success('ASO registrado.', 'Sucesso');
          onClose();
        },
        onError: (e) =>
          toast.error(e instanceof ApiError ? e.userMessage : 'Nao foi possivel registrar o ASO.'),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Registrar ASO"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-aso" loading={mutation.isPending}>
            Registrar
          </Button>
        </>
      }
    >
      <form id="form-aso" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Tipo de exame" required error={erro}>
          {({ id }) => (
            <Select id={id} value={tipo} onChange={(e) => setTipo(e.target.value)} options={TIPOS_EXAME} />
          )}
        </FormField>
        <FormField label="Data do exame" required>
          {({ id }) => (
            <Input id={id} type="date" value={dataExame} onChange={(e) => setDataExame(e.target.value)} />
          )}
        </FormField>
        <FormField label="Resultado" required>
          {({ id }) => (
            <Select
              id={id}
              value={resultado}
              onChange={(e) => setResultado(e.target.value)}
              options={[
                { value: '1', label: 'Apto' },
                { value: '2', label: 'Inapto' },
              ]}
            />
          )}
        </FormField>
        <FormField label="Medico responsavel" required>
          {({ id }) => (
            <Input id={id} value={medicoNome} onChange={(e) => setMedicoNome(e.target.value)} />
          )}
        </FormField>
        <FormRow>
          <FormField label="CRM" required>
            {({ id }) => <Input id={id} value={medicoCrm} onChange={(e) => setMedicoCrm(e.target.value)} />}
          </FormField>
          <FormField label="UF do CRM" required>
            {({ id }) => (
              <Input id={id} maxLength={2} value={medicoUf} onChange={(e) => setMedicoUf(e.target.value)} />
            )}
          </FormField>
        </FormRow>
        <FormField label="Proximo exame (PCMSO)" help="Data prevista do proximo exame periodico.">
          {({ id }) => <Input id={id} type="date" value={proximo} onChange={(e) => setProximo(e.target.value)} />}
        </FormField>
      </form>
    </Modal>
  );
}

function ComunicarCatModal({
  open,
  onClose,
  servidorId,
}: {
  open: boolean;
  onClose: () => void;
  servidorId: string;
}) {
  const toast = useToast();
  const mutation = useComunicarAcidente();
  const [tipoCat, setTipoCat] = useState('1');
  const [tipoAcidente, setTipoAcidente] = useState('1');
  const [dataHora, setDataHora] = useState('');
  const [descricao, setDescricao] = useState('');
  const [cid, setCid] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (!dataHora || !descricao.trim()) {
      setErro('Informe a data/hora do acidente e a descricao da situacao.');
      return;
    }
    setErro(undefined);
    mutation.mutate(
      {
        servidorId,
        tipoCat: Number(tipoCat),
        tipoAcidente: Number(tipoAcidente),
        dataHoraAcidente: new Date(dataHora).toISOString(),
        descricaoSituacao: descricao.trim(),
        cid: cid.trim() || null,
      },
      {
        onSuccess: () => {
          toast.success('CAT comunicada.', 'Sucesso');
          onClose();
        },
        onError: (e) =>
          toast.error(e instanceof ApiError ? e.userMessage : 'Nao foi possivel comunicar a CAT.'),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Comunicar acidente (CAT)"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-cat" loading={mutation.isPending}>
            Comunicar
          </Button>
        </>
      }
    >
      <form id="form-cat" className="br-form" onSubmit={submeter} noValidate>
        <FormRow>
          <FormField label="Tipo de CAT" required error={erro}>
            {({ id }) => (
              <Select id={id} value={tipoCat} onChange={(e) => setTipoCat(e.target.value)} options={TIPOS_CAT} />
            )}
          </FormField>
          <FormField label="Tipo de acidente" required>
            {({ id }) => (
              <Select
                id={id}
                value={tipoAcidente}
                onChange={(e) => setTipoAcidente(e.target.value)}
                options={TIPOS_ACIDENTE}
              />
            )}
          </FormField>
        </FormRow>
        <FormField label="Data/hora do acidente" required>
          {({ id }) => (
            <Input id={id} type="datetime-local" value={dataHora} onChange={(e) => setDataHora(e.target.value)} />
          )}
        </FormField>
        <FormField label="Descricao da situacao" required>
          {({ id }) => <Input id={id} value={descricao} onChange={(e) => setDescricao(e.target.value)} />}
        </FormField>
        <FormField label="CID-10" help="Opcional.">
          {({ id }) => <Input id={id} maxLength={4} value={cid} onChange={(e) => setCid(e.target.value)} />}
        </FormField>
      </form>
    </Modal>
  );
}

export function SstServidorPage() {
  const { servidorId = '' } = useParams<{ servidorId: string }>();
  const location = useLocation();
  const nome = (location.state as { nome?: string } | null)?.nome ?? 'Servidor';
  const toast = useToast();

  const exames = useExamesOcupacionais(servidorId);
  const exposicoes = useExposicoes(servidorId);
  const cats = useComunicacoesAcidente(servidorId);
  const gerar2220 = useGerarS2220();
  const gerar2240 = useGerarS2240();
  const gerar2210 = useGerarS2210();

  const [exameAberto, setExameAberto] = useState(false);
  const [catAberta, setCatAberta] = useState(false);

  function gerarEvento(mutate: { mutate: (id: string, opts: object) => void }, id: string, rotulo: string): void {
    mutate.mutate(id, {
      onSuccess: () => toast.success(`Evento ${rotulo} gerado.`, 'Sucesso'),
      onError: (e: unknown) =>
        toast.error(e instanceof ApiError ? e.userMessage : `Nao foi possivel gerar o ${rotulo}.`),
    });
  }

  const colExames: Column<ExameOcupacionalView>[] = [
    { key: 'tipo', header: 'Tipo', render: (e) => e.tipo },
    { key: 'data', header: 'Data', render: (e) => formatarData(e.dataExame) },
    {
      key: 'resultado',
      header: 'Resultado',
      render: (e) => <Tag variant={e.resultado === 'Apto' ? 'success' : 'danger'}>{e.resultado}</Tag>,
    },
    { key: 'medico', header: 'Medico', render: (e) => `${e.medicoNome} (${e.medicoCrm})` },
    { key: 'proximo', header: 'Proximo exame', render: (e) => (e.dataProximoExame ? formatarData(e.dataProximoExame) : '—') },
    {
      key: 'acoes',
      header: 'eSocial',
      render: (e) => (
        <Can permission={PERM_RH_GERENCIAR}>
          <Button variant="secondary" size="sm" onClick={() => gerarEvento(gerar2220, e.id, 'S-2220')}>
            Gerar S-2220
          </Button>
        </Can>
      ),
    },
  ];

  const colExposicoes: Column<ExposicaoAgenteNocivoView>[] = [
    { key: 'setor', header: 'Setor/atividade', render: (e) => e.setorAtividade },
    { key: 'inicio', header: 'Inicio', render: (e) => formatarData(e.inicioExposicao) },
    { key: 'fim', header: 'Fim', render: (e) => (e.fimExposicao ? formatarData(e.fimExposicao) : 'Vigente') },
    { key: 'agentes', header: 'Agentes', render: (e) => e.agentes.map((a) => a.codigo).join(', ') },
    {
      key: 'acoes',
      header: 'eSocial',
      render: (e) => (
        <Can permission={PERM_RH_GERENCIAR}>
          <Button variant="secondary" size="sm" onClick={() => gerarEvento(gerar2240, e.id, 'S-2240')}>
            Gerar S-2240
          </Button>
        </Can>
      ),
    },
  ];

  const colCats: Column<ComunicacaoAcidenteView>[] = [
    { key: 'tipo', header: 'Tipo', render: (c) => c.tipoCat },
    { key: 'acidente', header: 'Acidente', render: (c) => c.tipoAcidente },
    { key: 'dataHora', header: 'Data/hora', render: (c) => formatarDataHora(c.dataHoraAcidente) },
    { key: 'descricao', header: 'Situacao', render: (c) => c.descricaoSituacao },
    {
      key: 'acoes',
      header: 'eSocial',
      render: (c) => (
        <Can permission={PERM_RH_GERENCIAR}>
          <Button variant="secondary" size="sm" onClick={() => gerarEvento(gerar2210, c.id, 'S-2210')}>
            Gerar S-2210
          </Button>
        </Can>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        eyebrow="Recursos Humanos"
        title={`SST — ${nome}`}
        description="Saude e seguranca do trabalho: ASO (monitoramento da saude/PCMSO), condicoes ambientais (agentes nocivos/PPP) e CAT. Cada registro gera o evento eSocial correspondente (S-2220/S-2240/S-2210)."
      />

      <CardSecao
        className="mb-4"
        titulo="Monitoramento da saude (ASO)"
        subtitulo="Atestados de Saude Ocupacional do servidor (PCMSO/NR-07). Cada ASO gera o evento eSocial S-2220."
        acao={
          <Can permission={PERM_RH_GERENCIAR}>
            <Toolbar>
              <Button variant="primary" onClick={() => setExameAberto(true)}>
                <i className="fas fa-notes-medical" aria-hidden="true" /> Registrar ASO
              </Button>
            </Toolbar>
          </Can>
        }
      >
        <DataTable
          caption="ASO do servidor"
          columns={colExames}
          rows={exames.data}
          rowKey={(e) => e.id}
          loading={exames.isLoading}
          error={exames.isError ? errorMessage(exames.error) : null}
          empty={<EmptyState icon="fas fa-notes-medical" title="Nenhum ASO" description="Registre o ASO do servidor." />}
        />
      </CardSecao>

      <CardSecao
        className="mb-4"
        titulo="Condicoes ambientais (agentes nocivos / PPP)"
        subtitulo="Periodos de exposicao a agentes nocivos. Fonte do PPP e do evento eSocial S-2240."
      >
        <DataTable
          caption="Exposicoes do servidor"
          columns={colExposicoes}
          rows={exposicoes.data}
          rowKey={(e) => e.id}
          loading={exposicoes.isLoading}
          error={exposicoes.isError ? errorMessage(exposicoes.error) : null}
          empty={<EmptyState icon="fas fa-mask-face" title="Nenhuma exposicao" description="Sem registros ambientais." />}
        />
      </CardSecao>

      <CardSecao
        titulo="Comunicacoes de acidente (CAT)"
        subtitulo="Acidentes/doencas ocupacionais comunicados. Cada CAT gera o evento eSocial S-2210."
        acao={
          <Can permission={PERM_RH_GERENCIAR}>
            <Toolbar>
              <Button variant="primary" onClick={() => setCatAberta(true)}>
                <i className="fas fa-triangle-exclamation" aria-hidden="true" /> Comunicar CAT
              </Button>
            </Toolbar>
          </Can>
        }
      >
        <DataTable
          caption="CAT do servidor"
          columns={colCats}
          rows={cats.data}
          rowKey={(c) => c.id}
          loading={cats.isLoading}
          error={cats.isError ? errorMessage(cats.error) : null}
          empty={<EmptyState icon="fas fa-triangle-exclamation" title="Nenhuma CAT" description="Sem acidentes comunicados." />}
        />
      </CardSecao>

      <RegistrarExameModal open={exameAberto} onClose={() => setExameAberto(false)} servidorId={servidorId} />
      <ComunicarCatModal open={catAberta} onClose={() => setCatAberta(false)} servidorId={servidorId} />
    </>
  );
}
