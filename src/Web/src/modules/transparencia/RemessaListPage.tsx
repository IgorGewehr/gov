// Tela de LISTA (consulta) das Remessas de PRESTAÇÃO DE CONTAS ao TCE-RS (SIAPC/PAD).
// Filtros (exercício/tipo/situação) -> DataTable com situação, nome do ZIP e protocolo,
// estados loading/vazio/erro, link para detalhe e abertura do formulário de geração.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link } from 'react-router-dom';
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
  Toolbar,
  errorMessage,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarData } from '../../i18n/format';
import { useRemessas } from './api';
import type { ListarRemessasParams, RemessaResumo, SituacaoRemessa, TipoPeriodo } from './api';
import {
  exercicioCorrente,
  situacaoRemessaLabel,
  situacaoRemessaOptions,
  situacaoRemessaTagVariant,
  tipoPeriodoOptions,
} from './transparencia.helpers';
import { RemessaFormModal } from './RemessaFormModal';
import { RemessaFolhaFormModal } from './RemessaFolhaFormModal';

export function RemessaListPage() {
  const [exercicio, setExercicio] = useState(String(exercicioCorrente()));
  const [tipo, setTipo] = useState<TipoPeriodo | ''>('');
  const [situacao, setSituacao] = useState<SituacaoRemessa | ''>('');
  const [filtros, setFiltros] = useState<ListarRemessasParams | null>(null);
  const [formAberto, setFormAberto] = useState(false);
  const [formFolhaAberto, setFormFolhaAberto] = useState(false);

  const query = useRemessas(filtros ?? { exercicio: 0 }, filtros !== null);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    const ano = Number(exercicio);
    if (!Number.isInteger(ano) || ano <= 0) return;
    setFiltros({ exercicio: ano, tipo: tipo || null, situacao: situacao || null });
  }

  const columns: Column<RemessaResumo>[] = [
    { key: 'periodo', header: 'Período', sortAccessor: (r) => r.periodo, render: (r) => r.periodo },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (r) => r.situacao,
      render: (r) => (
        <Tag variant={situacaoRemessaTagVariant(r.situacao)}>{situacaoRemessaLabel[r.situacao]}</Tag>
      ),
    },
    {
      key: 'nomeArquivoZip',
      header: 'Arquivo (ZIP)',
      sortAccessor: (r) => r.nomeArquivoZip ?? '',
      render: (r) => r.nomeArquivoZip ?? '—',
    },
    {
      key: 'protocolo',
      header: 'Protocolo',
      sortAccessor: (r) => r.protocolo ?? '',
      render: (r) => r.protocolo ?? '—',
    },
    { key: 'dataLimite', header: 'Prazo', sortAccessor: (r) => r.dataLimite, render: (r) => formatarData(r.dataLimite) },
    { key: 'dataEnvio', header: 'Envio', sortAccessor: (r) => r.dataEnvio ?? '', render: (r) => formatarData(r.dataEnvio) },
    {
      key: 'acoes',
      header: 'Ações',
      render: (r) => (
        <Link className="br-button tertiary small" to={`/transparencia/remessas-tce/${r.id}`}>
          Detalhes
        </Link>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        eyebrow="Transparência"
        title="Prestação de contas (TCE-RS)"
        description="Gere, valide e empacote as remessas do SIAPC/PAD e registre o protocolo da transmissão."
        actions={
          <Can permission="transparencia.gerenciar">
            <Toolbar>
              <Button variant="secondary" onClick={() => setFormFolhaAberto(true)}>
                <i className="fas fa-users" aria-hidden="true" /> Gerar remessa de folha
              </Button>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Gerar remessa contábil
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <FormRow
            acao={
              <Button variant="primary" type="submit" loading={query.isFetching}>
                Consultar
              </Button>
            }
          >
            <div className="row">
              <div className="col-12 col-sm-4">
                <FormField label="Exercício" required>
                  {({ id, describedBy, invalid }) => (
                    <Input
                      id={id}
                      type="number"
                      min="1900"
                      inputMode="numeric"
                      aria-describedby={describedBy}
                      invalid={invalid}
                      value={exercicio}
                      onChange={(e) => setExercicio(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-12 col-sm-4">
                <FormField label="Tipo de período">
                  {({ id, describedBy }) => (
                    <Select
                      id={id}
                      aria-describedby={describedBy}
                      placeholder="Todos"
                      options={tipoPeriodoOptions}
                      value={tipo}
                      onChange={(e) => setTipo(e.target.value as TipoPeriodo | '')}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-12 col-sm-4">
                <FormField label="Situação">
                  {({ id, describedBy }) => (
                    <Select
                      id={id}
                      aria-describedby={describedBy}
                      placeholder="Todas"
                      options={situacaoRemessaOptions}
                      value={situacao}
                      onChange={(e) => setSituacao(e.target.value as SituacaoRemessa | '')}
                    />
                  )}
                </FormField>
              </div>
            </div>
          </FormRow>
        </form>
      </Card>

      {filtros === null ? (
        <EmptyState
          icon="fas fa-magnifying-glass"
          title="Faça uma consulta"
          description="Informe o exercício e clique em Consultar para listar as remessas."
        />
      ) : (
        <DataTable
          caption={`Remessas ao TCE-RS do exercício ${filtros.exercicio}`}
          columns={columns}
          rows={query.data}
          rowKey={(r) => r.id}
          loading={query.isLoading}
          error={query.isError ? errorMessage(query.error) : null}
          empty={
            <EmptyState
              icon="fas fa-folder-open"
              title="Nenhuma remessa encontrada"
              description="Não há remessas para o exercício e filtros informados."
            />
          }
        />
      )}

      <RemessaFormModal
        open={formAberto}
        onClose={() => setFormAberto(false)}
        exercicioInicial={Number(exercicio) || exercicioCorrente()}
      />

      <RemessaFolhaFormModal
        open={formFolhaAberto}
        onClose={() => setFormFolhaAberto(false)}
        exercicioInicial={Number(exercicio) || exercicioCorrente()}
      />
    </>
  );
}
