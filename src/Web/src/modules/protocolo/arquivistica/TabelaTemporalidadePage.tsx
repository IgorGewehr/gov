// TABELA DE TEMPORALIDADE E DESTINACAO (TTD — CONARQ / Res. 40/2014): por classe,
// prazo de guarda corrente + intermediaria (anos) e destinacao final (Eliminacao OU
// Guarda Permanente), com o evento-base da contagem e a norma-fonte (auditavel ao TCE).
// I-T6: prazos e destinacao vem SEMPRE da TTD (zero hardcode). Cadastro via POST.
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Alert,
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
  Textarea,
  Toolbar,
  useToast,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { ProtocoloSubNav } from '../ProtocoloSubNav';
import {
  DESTINACAO_VALOR,
  EVENTO_CONTAGEM_VALOR,
  useCadastrarTabelaTemporalidade,
  type Destinacao,
  type EventoContagem,
  type ItemTabelaTemporalidadeInput,
} from './arquivistica.api';
import {
  chaveDestinacao,
  chaveEventoContagem,
  DESTINACAO_LABEL,
  DESTINACAO_OPCOES,
  destinacaoTagVariant,
  EVENTO_CONTAGEM_LABEL,
  EVENTO_CONTAGEM_OPCOES,
} from './arquivistica.helpers';

const CODIGO_MAX = 20;
const OBSERVACAO_MAX = 1000;

export function TabelaTemporalidadePage() {
  const toast = useToast();
  const mutation = useCadastrarTabelaTemporalidade();

  const [nome, setNome] = useState('');
  const [regras, setRegras] = useState<ItemTabelaTemporalidadeInput[]>([]);

  const [codigo, setCodigo] = useState('');
  const [corrente, setCorrente] = useState('');
  const [intermediaria, setIntermediaria] = useState('');
  const [destinacao, setDestinacao] = useState<Destinacao>('Eliminacao');
  const [evento, setEvento] = useState<EventoContagem>('DataArquivamento');
  const [observacao, setObservacao] = useState('');
  const [erroItem, setErroItem] = useState<string | null>(null);

  function adicionarRegra(event: FormEvent): void {
    event.preventDefault();
    const cod = codigo.trim();
    const pc = Number(corrente);
    const pi = Number(intermediaria);
    if (cod === '') {
      setErroItem('Informe o código da classe a que a regra se aplica.');
      return;
    }
    if (cod.length > CODIGO_MAX) {
      setErroItem(`O código deve ter no máximo ${CODIGO_MAX} caracteres.`);
      return;
    }
    if (!Number.isInteger(pc) || pc < 0 || !Number.isInteger(pi) || pi < 0) {
      setErroItem('Os prazos de guarda devem ser números inteiros maiores ou iguais a zero.');
      return;
    }
    if (regras.some((r) => r.codigoClassificacao === cod)) {
      setErroItem('Já existe uma regra de temporalidade para esta classe.');
      return;
    }
    setRegras((atual) => [
      ...atual,
      {
        codigoClassificacao: cod,
        prazoGuardaCorrenteAnos: pc,
        prazoGuardaIntermediariaAnos: pi,
        destinacao: DESTINACAO_VALOR[destinacao],
        eventoContagem: EVENTO_CONTAGEM_VALOR[evento],
        observacao: observacao.trim() === '' ? null : observacao.trim(),
      },
    ]);
    setCodigo('');
    setCorrente('');
    setIntermediaria('');
    setObservacao('');
    setErroItem(null);
  }

  function removerRegra(cod: string): void {
    setRegras((atual) => atual.filter((r) => r.codigoClassificacao !== cod));
  }

  function cadastrar(): void {
    if (nome.trim() === '') {
      setErroItem('Informe o nome da tabela de temporalidade.');
      return;
    }
    if (regras.length === 0) {
      setErroItem('Adicione ao menos uma regra de temporalidade.');
      return;
    }
    mutation.mutate(
      { nome: nome.trim(), regras },
      {
        onSuccess: () => {
          toast.success('Tabela de temporalidade cadastrada.');
          setNome('');
          setRegras([]);
          setErroItem(null);
        },
        onError: (erro) => {
          const msg =
            erro instanceof ApiError ? erro.message : 'Falha ao cadastrar a tabela de temporalidade.';
          toast.error(msg);
        },
      },
    );
  }

  const columns: Column<ItemTabelaTemporalidadeInput>[] = [
    { key: 'classe', header: 'Classe', render: (r) => <strong>{r.codigoClassificacao}</strong> },
    {
      key: 'corrente',
      header: 'Corrente (anos)',
      render: (r) => r.prazoGuardaCorrenteAnos,
    },
    {
      key: 'intermediaria',
      header: 'Intermediária (anos)',
      render: (r) => r.prazoGuardaIntermediariaAnos,
    },
    {
      key: 'destinacao',
      header: 'Destinação',
      render: (r) => {
        const chave = chaveDestinacao(r.destinacao);
        return <Tag variant={destinacaoTagVariant(chave)}>{DESTINACAO_LABEL[chave]}</Tag>;
      },
    },
    {
      key: 'evento',
      header: 'Contagem a partir de',
      render: (r) => EVENTO_CONTAGEM_LABEL[chaveEventoContagem(r.eventoContagem)],
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (r) => (
        <Button variant="tertiary" size="sm" onClick={() => removerRegra(r.codigoClassificacao)}>
          <i className="fas fa-trash" aria-hidden="true" /> Remover
        </Button>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        eyebrow="Protocolo · Arquivística"
        title="Tabela de temporalidade e destinação (TTD)"
        description="Defina, por classe, os prazos de guarda (corrente e intermediária) e a destinação final. Os prazos e a destinação vêm sempre da TTD — nunca codificados (I-T6)."
      />
      <ProtocoloSubNav />

      <Alert variant="info" className="mb-4" title="Guarda permanente nunca elimina.">
        Classes com destinação <strong>Guarda permanente</strong> jamais transitam para eliminação
        (valor probatório/histórico). A eliminação só ocorre após o decurso integral dos prazos, com
        edital e termo carimbado (Res. CONARQ 40/2014).
      </Alert>

      <Card className="mb-4">
        <FormField label="Nome da TTD" required>
          {({ id, describedBy }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              value={nome}
              maxLength={200}
              onChange={(e) => setNome(e.target.value)}
              placeholder="Tabela de Temporalidade — Município XYZ"
            />
          )}
        </FormField>
      </Card>

      <Card className="mb-4">
        <h3 className="text-up-01 mb-3">Adicionar regra</h3>
        <form className="br-form" onSubmit={adicionarRegra}>
          <div className="row">
            <div className="col-12 col-md-3">
              <FormField label="Código da classe" required>
                {({ id, describedBy }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    value={codigo}
                    maxLength={CODIGO_MAX}
                    onChange={(e) => setCodigo(e.target.value)}
                    placeholder="040.1"
                  />
                )}
              </FormField>
            </div>
            <div className="col-6 col-md-2">
              <FormField label="Corrente (anos)" required>
                {({ id, describedBy }) => (
                  <Input
                    id={id}
                    type="number"
                    min={0}
                    aria-describedby={describedBy}
                    value={corrente}
                    onChange={(e) => setCorrente(e.target.value)}
                  />
                )}
              </FormField>
            </div>
            <div className="col-6 col-md-2">
              <FormField label="Intermediária (anos)" required>
                {({ id, describedBy }) => (
                  <Input
                    id={id}
                    type="number"
                    min={0}
                    aria-describedby={describedBy}
                    value={intermediaria}
                    onChange={(e) => setIntermediaria(e.target.value)}
                  />
                )}
              </FormField>
            </div>
            <div className="col-12 col-md">
              <FormField label="Destinação final" required>
                {({ id, describedBy }) => (
                  <Select
                    id={id}
                    aria-describedby={describedBy}
                    options={DESTINACAO_OPCOES}
                    value={destinacao}
                    onChange={(e) => setDestinacao(e.target.value as Destinacao)}
                  />
                )}
              </FormField>
            </div>
          </div>
          <FormRow
            acao={
              <Button variant="secondary" type="submit">
                <i className="fas fa-plus" aria-hidden="true" /> Adicionar
              </Button>
            }
          >
            <div className="row">
              <div className="col-12 col-md-4">
                <FormField label="Contagem a partir de" required>
                  {({ id, describedBy }) => (
                    <Select
                      id={id}
                      aria-describedby={describedBy}
                      options={EVENTO_CONTAGEM_OPCOES}
                      value={evento}
                      onChange={(e) => setEvento(e.target.value as EventoContagem)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-12 col-md">
                <FormField label="Norma-fonte (observação)">
                  {({ id, describedBy }) => (
                    <Textarea
                      id={id}
                      aria-describedby={describedBy}
                      rows={2}
                      maxLength={OBSERVACAO_MAX}
                      value={observacao}
                      onChange={(e) => setObservacao(e.target.value)}
                      placeholder="Ex.: TTD CONARQ classe 040.1 — auditável ao TCE."
                    />
                  )}
                </FormField>
              </div>
            </div>
          </FormRow>
        </form>
        {erroItem && (
          <Alert variant="warning" className="mt-3">
            {erroItem}
          </Alert>
        )}
      </Card>

      <Card>
        <Toolbar align="between" className="mb-3 align-items-center">
          <h3 className="text-up-01 mb-0">Regras da TTD ({regras.length})</h3>
          <Button
            variant="primary"
            onClick={cadastrar}
            loading={mutation.isPending}
            disabled={regras.length === 0 || nome.trim() === ''}
          >
            <i className="fas fa-floppy-disk" aria-hidden="true" /> Cadastrar TTD
          </Button>
        </Toolbar>
        <DataTable
          caption="Regras de temporalidade em composição"
          columns={columns}
          rows={regras}
          rowKey={(r) => r.codigoClassificacao}
          empty={
            <EmptyState
              icon="fas fa-hourglass-half"
              title="Nenhuma regra adicionada"
              description="Adicione as regras de temporalidade por classe documental."
            />
          }
        />
      </Card>
    </>
  );
}
