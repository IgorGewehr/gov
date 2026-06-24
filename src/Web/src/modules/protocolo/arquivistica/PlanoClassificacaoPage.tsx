// PLANO DE CLASSIFICACAO (CONARQ / e-ARQ v2): arvore codigo x assunto montada em
// memoria (codigo-pai define a hierarquia) e cadastrada de uma vez via
// CadastrarPlanoClassificacao. Atividade-MEIO = codigo nacional; FIM = proprio do ente.
// Sem GET no contrato: a tela e de composicao + envio (POST). Gated por protocolo.gerenciar.
import { useMemo, useState } from 'react';
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
  Tag,
  Toolbar,
  useToast,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { ProtocoloSubNav } from '../ProtocoloSubNav';
import {
  useCadastrarPlanoClassificacao,
  type ItemPlanoClassificacaoInput,
} from './arquivistica.api';

const CODIGO_MAX = 20;
const ASSUNTO_MAX = 200;

/** Linha exibida: classe + profundidade calculada pela cadeia de codigo-pai (recuo visual). */
interface LinhaClasse extends ItemPlanoClassificacaoInput {
  nivel: number;
}

/** Calcula a profundidade de cada classe seguindo a cadeia de codigoPai (arvore). */
function ordenarComoArvore(classes: ItemPlanoClassificacaoInput[]): LinhaClasse[] {
  const porCodigo = new Map(classes.map((c) => [c.codigo, c]));
  function nivelDe(codigo: string, visitados = new Set<string>()): number {
    const classe = porCodigo.get(codigo);
    const pai = classe?.codigoPai?.trim();
    if (!pai || !porCodigo.has(pai) || visitados.has(codigo)) return 0;
    visitados.add(codigo);
    return 1 + nivelDe(pai, visitados);
  }
  return classes.map((c) => ({ ...c, nivel: nivelDe(c.codigo) }));
}

export function PlanoClassificacaoPage() {
  const toast = useToast();
  const mutation = useCadastrarPlanoClassificacao();

  const [nome, setNome] = useState('');
  const [classes, setClasses] = useState<ItemPlanoClassificacaoInput[]>([]);

  const [codigo, setCodigo] = useState('');
  const [assunto, setAssunto] = useState('');
  const [codigoPai, setCodigoPai] = useState('');
  const [atividadeFim, setAtividadeFim] = useState(false);
  const [erroItem, setErroItem] = useState<string | null>(null);

  const linhas = useMemo(() => ordenarComoArvore(classes), [classes]);

  function adicionarClasse(event: FormEvent): void {
    event.preventDefault();
    const cod = codigo.trim();
    const ass = assunto.trim();
    const pai = codigoPai.trim();
    if (cod === '' || ass === '') {
      setErroItem('Informe o código e o assunto da classe.');
      return;
    }
    if (cod.length > CODIGO_MAX) {
      setErroItem(`O código deve ter no máximo ${CODIGO_MAX} caracteres.`);
      return;
    }
    if (classes.some((c) => c.codigo === cod)) {
      setErroItem('Já existe uma classe com este código no plano.');
      return;
    }
    if (pai !== '' && !classes.some((c) => c.codigo === pai)) {
      setErroItem('A classe-pai informada ainda não foi adicionada ao plano.');
      return;
    }
    setClasses((atual) => [
      ...atual,
      { codigo: cod, assunto: ass, atividadeFim, codigoPai: pai === '' ? null : pai },
    ]);
    setCodigo('');
    setAssunto('');
    setCodigoPai('');
    setAtividadeFim(false);
    setErroItem(null);
  }

  function removerClasse(cod: string): void {
    // Remove a classe e qualquer descendente que ficaria orfao (mantem a arvore consistente).
    setClasses((atual) => {
      const restantes = atual.filter((c) => c.codigo !== cod);
      return restantes.filter((c) => !c.codigoPai || restantes.some((r) => r.codigo === c.codigoPai));
    });
  }

  function cadastrar(): void {
    if (nome.trim() === '') {
      setErroItem('Informe o nome do plano de classificação.');
      return;
    }
    if (classes.length === 0) {
      setErroItem('Adicione ao menos uma classe ao plano.');
      return;
    }
    mutation.mutate(
      { nome: nome.trim(), classes },
      {
        onSuccess: () => {
          toast.success('Plano de classificação cadastrado.');
          setNome('');
          setClasses([]);
          setErroItem(null);
        },
        onError: (erro) => {
          const msg =
            erro instanceof ApiError ? erro.message : 'Falha ao cadastrar o plano de classificação.';
          toast.error(msg);
        },
      },
    );
  }

  const columns: Column<LinhaClasse>[] = [
    {
      key: 'codigo',
      header: 'Código',
      render: (c) => (
        <span style={{ paddingLeft: `${c.nivel * 1.5}rem` }}>
          {c.nivel > 0 && <i className="fas fa-turn-up fa-rotate-90 mr-1" aria-hidden="true" />}
          <strong>{c.codigo}</strong>
        </span>
      ),
    },
    { key: 'assunto', header: 'Assunto', render: (c) => c.assunto },
    {
      key: 'atividade',
      header: 'Atividade',
      render: (c) => (
        <Tag variant={c.atividadeFim ? 'info' : 'default'}>{c.atividadeFim ? 'Fim' : 'Meio'}</Tag>
      ),
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (c) => (
        <Button variant="tertiary" size="sm" onClick={() => removerClasse(c.codigo)}>
          <i className="fas fa-trash" aria-hidden="true" /> Remover
        </Button>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        eyebrow="Protocolo · Arquivística"
        title="Plano de classificação documental"
        description="Monte a árvore de classes (código × assunto) conforme o CONARQ / e-ARQ v2. Atividade-meio usa código nacional; atividade-fim é própria do ente."
      />
      <ProtocoloSubNav />

      <Card className="mb-4">
        <FormField label="Nome do plano" required>
          {({ id, describedBy }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              value={nome}
              maxLength={ASSUNTO_MAX}
              onChange={(e) => setNome(e.target.value)}
              placeholder="Plano de Classificação — Município XYZ"
            />
          )}
        </FormField>
      </Card>

      <Card className="mb-4">
        <h3 className="text-up-01 mb-3">Adicionar classe</h3>
        <form className="br-form" onSubmit={adicionarClasse}>
          <FormRow
            acao={
              <Button variant="secondary" type="submit">
                <i className="fas fa-plus" aria-hidden="true" /> Adicionar
              </Button>
            }
          >
            <div className="row">
              <div className="col-12 col-md-3">
                <FormField label="Código" required>
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
              <div className="col-12 col-md">
                <FormField label="Assunto" required>
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      aria-describedby={describedBy}
                      value={assunto}
                      maxLength={ASSUNTO_MAX}
                      onChange={(e) => setAssunto(e.target.value)}
                      placeholder="Pessoal — Registros funcionais"
                    />
                  )}
                </FormField>
              </div>
              <div className="col-12 col-md-3">
                <FormField label="Código da classe-pai">
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      aria-describedby={describedBy}
                      value={codigoPai}
                      maxLength={CODIGO_MAX}
                      onChange={(e) => setCodigoPai(e.target.value)}
                      placeholder="040 (opcional)"
                    />
                  )}
                </FormField>
              </div>
            </div>
            <div className="br-checkbox mt-2">
              <input
                id="plano-atividade-fim"
                type="checkbox"
                checked={atividadeFim}
                onChange={(e) => setAtividadeFim(e.target.checked)}
              />
              <label htmlFor="plano-atividade-fim">Atividade-fim (finalística do ente)</label>
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
          <h3 className="text-up-01 mb-0">Classes do plano ({classes.length})</h3>
          <Button
            variant="primary"
            onClick={cadastrar}
            loading={mutation.isPending}
            disabled={classes.length === 0 || nome.trim() === ''}
          >
            <i className="fas fa-floppy-disk" aria-hidden="true" /> Cadastrar plano
          </Button>
        </Toolbar>
        <DataTable
          caption="Classes documentais do plano em composição"
          columns={columns}
          rows={linhas}
          rowKey={(c) => c.codigo}
          empty={
            <EmptyState
              icon="fas fa-sitemap"
              title="Nenhuma classe adicionada"
              description="Adicione as classes documentais (código × assunto) para compor o plano."
            />
          }
        />
      </Card>
    </>
  );
}
