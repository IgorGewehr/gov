// Visão de DADOS ABERTOS / PORTAL PÚBLICO (LAI). Deixa EXPLÍCITO o que é PÚBLICO:
//  - lista o catálogo (dicionário) de datasets publicados e suas colunas;
//  - oferece o link público do portal e o download CSV de cada dataset (links anônimos,
//    fora de /api — qualquer cidadão acessa sem login);
//  - permite ao operador configurar o slug público do ente (gated transparencia.gerenciar).
// O download/portal só funcionam APÓS o slug estar configurado.
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Alert,
  Button,
  Card,
  CardSecao,
  DataTable,
  FormField,
  FormRow,
  Input,
  PageHeader,
  Tag,
  useToast,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { Can } from '../../auth/Can';
import { ApiError } from '../../api/problemDetails';
import { exercicioCorrente } from './transparencia.helpers';
import { CATALOGO_DADOS_ABERTOS } from './dados-abertos.helpers';
import type { DatasetCatalogo } from './dados-abertos.helpers';
import { useConfigurarPortal, urlDownloadCsv, urlPortalPublico } from './dados-abertos.api';
import { TransparenciaSubNav } from './TransparenciaSubNav';

export function DadosAbertosPage() {
  const [slug, setSlug] = useState('');
  const [exercicio, setExercicio] = useState(String(exercicioCorrente()));

  const slugValido = slug.trim().length > 0;
  const ano = Number(exercicio);
  const filtroExercicio = Number.isInteger(ano) && ano > 0 ? ano : null;

  const columns: Column<DatasetCatalogo>[] = [
    {
      key: 'titulo',
      header: 'Conjunto de dados',
      sortAccessor: (d) => d.titulo,
      render: (d) => (
        <div className="d-flex flex-column">
          <strong>{d.titulo}</strong>
          <span className="text-muted">{d.descricao}</span>
        </div>
      ),
    },
    {
      key: 'colunas',
      header: 'Colunas (dicionário)',
      render: (d) => d.colunas.join(', '),
    },
    {
      key: 'download',
      header: 'CSV',
      render: (d) =>
        slugValido ? (
          <a
            className="br-button tertiary small"
            href={urlDownloadCsv(slug.trim(), d.dataset, filtroExercicio)}
            target="_blank"
            rel="noopener noreferrer"
            download
          >
            <i className="fas fa-download" aria-hidden="true" /> Baixar
          </a>
        ) : (
          <span className="text-muted">Configure o slug</span>
        ),
    },
  ];

  return (
    <>
      <PageHeader
        eyebrow="Transparência · Dados abertos"
        title="Portal de dados abertos (LAI)"
        description="Conteúdo PÚBLICO do portal de transparência: catálogo de conjuntos de dados e download em CSV, disponíveis a qualquer cidadão sem autenticação."
      />

      <TransparenciaSubNav />

      <Alert variant="info" title="Tudo nesta seção é PÚBLICO">
        Os conjuntos de dados e os arquivos CSV abaixo são servidos na superfície ANÔNIMA do portal
        (<code>/publico/transparencia/&#123;slug&#125;</code>), fora da área autenticada. Os dados são
        minimizados na origem (CPF mascarado; folha sem CPF/matrícula — Dec. 7.724/2012).
      </Alert>

      <Can permission="transparencia.gerenciar">
        <ConfiguracaoPortal onSlugAtualizado={setSlug} />
      </Can>

      <Card className="mb-4">
        <form
          className="br-form"
          onSubmit={(e) => {
            e.preventDefault();
          }}
        >
          <FormRow>
            <div className="row">
              <div className="col-12 col-sm-6">
                <FormField
                  label="Slug público do ente"
                  required
                  help="Identificador do ente na URL pública (ex.: maximiliano-de-almeida)."
                >
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      aria-describedby={describedBy}
                      placeholder="ex.: maximiliano-de-almeida"
                      value={slug}
                      onChange={(e) => setSlug(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-12 col-sm-6">
                <FormField label="Exercício (filtro do CSV)" help="Em branco baixa todos os anos.">
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      type="number"
                      min="1900"
                      inputMode="numeric"
                      aria-describedby={describedBy}
                      value={exercicio}
                      onChange={(e) => setExercicio(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
            </div>
          </FormRow>
        </form>

        {slugValido && (
          <p className="mb-0">
            <Tag variant="success">Portal público</Tag>{' '}
            <a
              href={urlPortalPublico(slug.trim())}
              target="_blank"
              rel="noopener noreferrer"
            >
              {urlPortalPublico(slug.trim())}
            </a>
          </p>
        )}
      </Card>

      <CardSecao
        titulo="Conjuntos de dados publicados"
        subtitulo="Dicionário de dados (colunas) e download CSV de cada conjunto."
        nota="Catálogo fixo do portal (LAI). O download exige o slug público configurado; o filtro de exercício é opcional."
      >
        <DataTable
          caption="Catálogo de conjuntos de dados abertos do portal"
          columns={columns}
          rows={CATALOGO_DADOS_ABERTOS as DatasetCatalogo[]}
          rowKey={(d) => d.dataset}
        />
      </CardSecao>
    </>
  );
}

/** Bloco INTERNO (gated) de configuração do slug + nome do portal público. */
function ConfiguracaoPortal({ onSlugAtualizado }: { onSlugAtualizado: (slug: string) => void }) {
  const toast = useToast();
  const mutation = useConfigurarPortal();
  const [slug, setSlug] = useState('');
  const [nomeEnte, setNomeEnte] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (slug.trim() === '' || nomeEnte.trim() === '') {
      setErro('Informe o slug e o nome do ente.');
      return;
    }
    setErro(undefined);
    mutation.mutate(
      { slug: slug.trim(), nomeEnte: nomeEnte.trim() },
      {
        onSuccess: (resposta) => {
          setSlug(resposta.slug);
          onSlugAtualizado(resposta.slug);
          toast.success(`Portal público configurado: ${resposta.slug}`, 'Sucesso');
        },
        onError: (error) => {
          toast.error(
            error instanceof ApiError
              ? error.userMessage
              : 'Não foi possível configurar o portal público.',
          );
        },
      },
    );
  }

  return (
    <Card className="mb-4">
      <form className="br-form" onSubmit={submeter} noValidate>
        <FormRow
          acao={
            <Button variant="primary" type="submit" loading={mutation.isPending}>
              Salvar portal
            </Button>
          }
        >
          <div className="row">
            <div className="col-12 col-sm-6">
              <FormField
                label="Slug público"
                required
                help="Será normalizado para kebab-case (único globalmente)."
                error={erro}
              >
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    placeholder="ex.: maximiliano-de-almeida"
                    value={slug}
                    onChange={(e) => setSlug(e.target.value)}
                  />
                )}
              </FormField>
            </div>
            <div className="col-12 col-sm-6">
              <FormField label="Nome de exibição do ente" required>
                {({ id, describedBy }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    placeholder="ex.: Município de Maximiliano de Almeida/RS"
                    value={nomeEnte}
                    onChange={(e) => setNomeEnte(e.target.value)}
                  />
                )}
              </FormField>
            </div>
          </div>
        </FormRow>
      </form>
    </Card>
  );
}
