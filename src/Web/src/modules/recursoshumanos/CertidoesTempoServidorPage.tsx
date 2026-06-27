// CERTIDOES DE TEMPO DE SERVICO/CONTRIBUICAO (CTC) de um servidor: apura o tempo (efetivo exercicio do
// vinculo + periodos averbados de outros orgaos/regimes), emite o documento numerado e autenticado e
// permite a validacao publica por codigo e a anulacao. O efetivo exercicio e' apurado automaticamente do
// vinculo no backend; aqui o operador informa a finalidade, o orgao emissor e os periodos averbados.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useParams, useLocation } from 'react-router-dom';
import {
  Alert,
  Button,
  CardSecao,
  DataTable,
  FormField,
  Input,
  Modal,
  PageHeader,
  QueryState,
  Select,
  Tag,
  Textarea,
  Toolbar,
  errorMessage,
  useToast,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarData } from '../../i18n/format';
import {
  FINALIDADES,
  REGIMES_ORIGEM,
  useCertidoesDoServidor,
  useEmitirCertidao,
  useAnularCertidao,
  useValidarCertidao,
} from './certidaoTempo.api';
import type {
  CertidaoResumoView,
  FinalidadeCertidao,
  PeriodoAverbadoInput,
  RegimeOrigemPeriodo,
} from './certidaoTempo.api';
import { PERM_RH_GERENCIAR } from './recursosHumanos.helpers';

function rotuloFinalidade(valor: FinalidadeCertidao): string {
  return FINALIDADES.find((f) => f.valor === valor)?.rotulo ?? valor;
}

// ---------------------------------------------------------------------------
// Modal de emissao
// ---------------------------------------------------------------------------

function EmitirCertidaoModal({
  open,
  onClose,
  servidorId,
}: {
  open: boolean;
  onClose: () => void;
  servidorId: string;
}) {
  const toast = useToast();
  const mutation = useEmitirCertidao();
  const [finalidade, setFinalidade] = useState<FinalidadeCertidao>('Aposentadoria');
  const [orgaoEmissor, setOrgaoEmissor] = useState('Departamento de Recursos Humanos');
  const [dataBase, setDataBase] = useState('');
  const [incluirEfetivo, setIncluirEfetivo] = useState(true);
  const [observacao, setObservacao] = useState('');
  const [averbados, setAverbados] = useState<PeriodoAverbadoInput[]>([]);
  const [erro, setErro] = useState<string | undefined>();

  function adicionarAverbado(): void {
    setAverbados((atual) => [
      ...atual,
      { inicio: '', fim: '', regimeOrigem: 'Rgps', origem: '', fator: 1, diasNaoComputaveis: 0 },
    ]);
  }

  function alterarAverbado(indice: number, campo: keyof PeriodoAverbadoInput, valor: string): void {
    setAverbados((atual) =>
      atual.map((p, i) => {
        if (i !== indice) return p;
        if (campo === 'fator') return { ...p, fator: Number(valor) || 1 };
        if (campo === 'diasNaoComputaveis') return { ...p, diasNaoComputaveis: Number(valor) || 0 };
        return { ...p, [campo]: valor };
      }),
    );
  }

  function removerAverbado(indice: number): void {
    setAverbados((atual) => atual.filter((_, i) => i !== indice));
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (!orgaoEmissor.trim()) {
      setErro('Informe o órgão emissor.');
      return;
    }
    if (!incluirEfetivo && averbados.length === 0) {
      setErro('Inclua o efetivo exercício ou ao menos um período averbado.');
      return;
    }
    for (const p of averbados) {
      if (!p.inicio || !p.fim || !p.origem.trim()) {
        setErro('Cada período averbado precisa de início, fim e origem.');
        return;
      }
    }
    setErro(undefined);
    mutation.mutate(
      {
        servidorId,
        finalidade,
        orgaoEmissor: orgaoEmissor.trim(),
        dataBase: dataBase || null,
        incluirEfetivoExercicio: incluirEfetivo,
        averbados: averbados.length > 0 ? averbados : undefined,
        observacao: observacao.trim() || null,
      },
      {
        onSuccess: () => {
          toast.success('Certidão emitida e autenticada.', 'Sucesso');
          setAverbados([]);
          setObservacao('');
          setDataBase('');
          onClose();
        },
        onError: (e) => setErro(errorMessage(e)),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Emitir certidão de tempo"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-emitir-certidao" loading={mutation.isPending}>
            Emitir
          </Button>
        </>
      }
    >
      <form id="form-emitir-certidao" className="br-form" onSubmit={submeter} noValidate>
        <FormField
          label="Finalidade"
          required
          help="Aposentadoria/disponibilidade contam tempo de contribuição; demais contam tempo de serviço."
        >
          {({ id, describedBy }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              value={finalidade}
              onChange={(e) => setFinalidade(e.target.value as FinalidadeCertidao)}
              options={FINALIDADES.map((f) => ({ value: f.valor, label: f.rotulo }))}
            />
          )}
        </FormField>

        <FormField label="Órgão emissor" required>
          {({ id, describedBy }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              value={orgaoEmissor}
              onChange={(e) => setOrgaoEmissor(e.target.value)}
              maxLength={200}
            />
          )}
        </FormField>

        <FormField
          label="Data-base da apuração"
          help="Opcional; em branco usa o desligamento (se desligado) ou a data de hoje."
        >
          {({ id, describedBy }) => (
            <Input
              id={id}
              type="date"
              aria-describedby={describedBy}
              value={dataBase}
              onChange={(e) => setDataBase(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Tempo do vínculo">
          {({ id }) => (
            <label htmlFor={id} className="d-flex align-items-center">
              <input
                id={id}
                type="checkbox"
                checked={incluirEfetivo}
                onChange={(e) => setIncluirEfetivo(e.target.checked)}
              />
              <span className="ml-2">Incluir o tempo de efetivo exercício (apurado do vínculo)</span>
            </label>
          )}
        </FormField>

        <fieldset className="br-fieldset">
          <legend>Períodos averbados (outros órgãos/regimes)</legend>
          {averbados.length === 0 && <p className="text-secondary">Nenhum período averbado.</p>}
          {averbados.map((p, i) => (
            <div key={i} className="d-flex flex-wrap gap-1 mb-2">
              <FormField label="Início">
                {({ id }) => (
                  <Input
                    id={id}
                    type="date"
                    value={p.inicio}
                    onChange={(e) => alterarAverbado(i, 'inicio', e.target.value)}
                  />
                )}
              </FormField>
              <FormField label="Fim">
                {({ id }) => (
                  <Input
                    id={id}
                    type="date"
                    value={p.fim}
                    onChange={(e) => alterarAverbado(i, 'fim', e.target.value)}
                  />
                )}
              </FormField>
              <FormField label="Regime">
                {({ id }) => (
                  <Select
                    id={id}
                    value={p.regimeOrigem}
                    onChange={(e) =>
                      alterarAverbado(i, 'regimeOrigem', e.target.value as RegimeOrigemPeriodo)
                    }
                    options={REGIMES_ORIGEM.map((r) => ({ value: r.valor, label: r.rotulo }))}
                  />
                )}
              </FormField>
              <FormField label="Origem">
                {({ id }) => (
                  <Input
                    id={id}
                    value={p.origem}
                    onChange={(e) => alterarAverbado(i, 'origem', e.target.value)}
                    placeholder="Órgão / certidão"
                  />
                )}
              </FormField>
              <FormField label="Fator">
                {({ id }) => (
                  <Input
                    id={id}
                    type="number"
                    step="0.01"
                    min="1"
                    value={String(p.fator ?? 1)}
                    onChange={(e) => alterarAverbado(i, 'fator', e.target.value)}
                  />
                )}
              </FormField>
              <Button type="button" variant="secondary" onClick={() => removerAverbado(i)}>
                Remover
              </Button>
            </div>
          ))}
          <Button type="button" variant="secondary" onClick={adicionarAverbado}>
            <i className="fas fa-plus" aria-hidden="true" /> Adicionar período averbado
          </Button>
        </fieldset>

        <FormField label="Observação">
          {({ id, describedBy }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              value={observacao}
              onChange={(e) => setObservacao(e.target.value)}
              maxLength={2000}
              rows={3}
            />
          )}
        </FormField>

        {erro && (
          <Alert variant="danger" title="Não foi possível emitir">
            {erro}
          </Alert>
        )}
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------
// Painel de validacao publica
// ---------------------------------------------------------------------------

function ValidacaoPanel() {
  const [codigo, setCodigo] = useState('');
  const [busca, setBusca] = useState('');
  const query = useValidarCertidao(busca, busca.length > 0);

  return (
    <CardSecao
      titulo="Validar autenticidade por código"
      subtitulo="Serviço de balcão: confirma se há certidão vigente com o código informado."
    >
      <Toolbar>
        <FormField label="Código de autenticação">
          {({ id }) => (
            <Input
              id={id}
              value={codigo}
              onChange={(e) => setCodigo(e.target.value.toUpperCase())}
              placeholder="16 caracteres hexadecimais"
            />
          )}
        </FormField>
        <Button
          variant="primary"
          onClick={() => setBusca(codigo.trim())}
          disabled={codigo.trim().length === 0}
        >
          Validar
        </Button>
      </Toolbar>
      {busca.length > 0 && (
        <QueryState
          isLoading={query.isLoading}
          isError={query.isError}
          error={query.error}
          data={query.data}
        >
          {(resultado) =>
            resultado.valida ? (
              <Alert variant="success" title="Certidão autêntica">
                <strong>{resultado.numero}</strong> — {resultado.servidorNome}
                <br />
                {resultado.finalidade && rotuloFinalidade(resultado.finalidade)} · emitida em{' '}
                {formatarData(resultado.dataEmissao)} · {resultado.tempoFormatado}
              </Alert>
            ) : (
              <Alert variant="warning" title="Código não confere">
                Nenhuma certidão vigente com este código.
              </Alert>
            )
          }
        </QueryState>
      )}
    </CardSecao>
  );
}

// ---------------------------------------------------------------------------
// Pagina
// ---------------------------------------------------------------------------

export function CertidoesTempoServidorPage() {
  const { servidorId = '' } = useParams<{ servidorId: string }>();
  const location = useLocation() as { state?: { nome?: string } };
  const nome = location.state?.nome;
  const toast = useToast();
  const query = useCertidoesDoServidor(servidorId);
  const anular = useAnularCertidao(servidorId);
  const [emitirAberto, setEmitirAberto] = useState(false);

  function onAnular(certidao: CertidaoResumoView): void {
    const motivo = window.prompt(`Motivo da anulação da certidão ${certidao.numero}:`);
    if (!motivo || !motivo.trim()) return;
    anular.mutate(
      { certidaoId: certidao.id, motivo: motivo.trim() },
      {
        onSuccess: () => toast.success('Certidão anulada.', 'Sucesso'),
        onError: (e) => toast.error(errorMessage(e)),
      },
    );
  }

  const colunas: Column<CertidaoResumoView>[] = [
    {
      key: 'numero',
      header: 'Número',
      render: (c) => (
        <Link to={`/recursoshumanos/certidoes-tempo/${c.id}`} state={{ nome }}>
          {c.numero}
        </Link>
      ),
    },
    { key: 'finalidade', header: 'Finalidade', render: (c) => rotuloFinalidade(c.finalidade) },
    { key: 'emissao', header: 'Emissão', render: (c) => formatarData(c.dataEmissao) },
    { key: 'tempo', header: 'Tempo', render: (c) => c.tempoFormatado },
    {
      key: 'situacao',
      header: 'Situação',
      render: (c) =>
        c.situacao === 'Emitida' ? (
          <Tag variant="success">Emitida</Tag>
        ) : (
          <Tag variant="danger">Anulada</Tag>
        ),
    },
    { key: 'codigo', header: 'Autenticação', render: (c) => <code>{c.codigoAutenticacao}</code> },
    {
      key: 'acoes',
      header: 'Ações',
      render: (c) =>
        c.situacao === 'Emitida' ? (
          <Can permission={PERM_RH_GERENCIAR}>
            <Button variant="secondary" onClick={() => onAnular(c)}>
              Anular
            </Button>
          </Can>
        ) : (
          '—'
        ),
    },
  ];

  return (
    <>
      <PageHeader
        eyebrow="Recursos Humanos"
        title="Certidões de tempo de serviço/contribuição"
        actions={
          <Link className="br-button secondary" to={`/recursoshumanos/servidores/${servidorId}/ficha`}>
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar à ficha
          </Link>
        }
      />

      {nome && (
        <Alert variant="info" className="mb-4">
          Servidor: <strong>{nome}</strong>
        </Alert>
      )}

      <CardSecao
        titulo="Certidões emitidas"
        subtitulo="O tempo de efetivo exercício é apurado do vínculo; os períodos averbados são informados na emissão."
        acao={
          <Can permission={PERM_RH_GERENCIAR}>
            <Button variant="primary" onClick={() => setEmitirAberto(true)}>
              <i className="fas fa-plus" aria-hidden="true" /> Emitir certidão
            </Button>
          </Can>
        }
      >
        <DataTable
          caption="Certidões de tempo do servidor"
          columns={colunas}
          rows={query.data}
          rowKey={(c) => c.id}
          loading={query.isLoading}
          error={query.isError ? errorMessage(query.error) : null}
          empty="Nenhuma certidão emitida para este servidor."
        />
      </CardSecao>

      <ValidacaoPanel />

      <EmitirCertidaoModal
        open={emitirAberto}
        onClose={() => setEmitirAberto(false)}
        servidorId={servidorId}
      />
    </>
  );
}
