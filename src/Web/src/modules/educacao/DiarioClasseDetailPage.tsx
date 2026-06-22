// Tela de DETALHE do Diário de Classe de uma matrícula (param de rota = matriculaId).
// Quando a matrícula ainda não possui diário (resposta nula), oferece a abertura do
// diário (mutation via modal). Quando há diário aberto, expõe as ações de registro
// (frequência, nota, aula) e a apuração, além da frequência consolidada (LDB 75%).
// Ações gated por permissão (educacao.gerenciar).
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Button, Card, EmptyState, PageHeader, QueryState, Tag } from '../../components/ui';
import { Can } from '../../auth/Can';
import { useDiarioDaMatricula, useFrequenciaDoDiario } from './api';
import type { DiarioClasseResumo } from './api';
import {
  formatarPercentual,
  resultadoAlunoTagVariant,
  situacaoDiarioTagVariant,
} from './educacao.helpers';
import { DiarioClasseFormModal } from './DiarioClasseFormModal';
import {
  ApurarResultadoModal,
  LancarNotaModal,
  RegistrarAulaModal,
  RegistrarFrequenciaModal,
} from './DiarioClasseAcaoModais';

type AcaoDiario = 'frequencia' | 'nota' | 'aula' | 'apurar' | null;

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

function FrequenciaCard({ diarioId }: { diarioId: string }) {
  const query = useFrequenciaDoDiario(diarioId);
  if (!query.data) return null;
  const f = query.data;
  return (
    <Card className="mt-4" header={<strong>Frequência consolidada</strong>}>
      <dl className="row">
        <Campo rotulo="Percentual de frequência">{formatarPercentual(f.percentualFrequencia)}</Campo>
        <Campo rotulo="Aulas computadas">{f.aulasComputadas}</Campo>
        <Campo rotulo="Atingiu o mínimo (LDB 75%)">
          <Tag variant={f.atingiuMinimo ? 'success' : 'danger'}>{f.atingiuMinimo ? 'Sim' : 'Não'}</Tag>
        </Campo>
      </dl>
    </Card>
  );
}

export function DiarioClasseDetailPage() {
  const { matriculaId = '' } = useParams<{ matriculaId: string }>();
  const query = useDiarioDaMatricula(matriculaId);
  const [formAberto, setFormAberto] = useState(false);
  const [acao, setAcao] = useState<AcaoDiario>(null);

  return (
    <>
      <PageHeader
        title="Diário de Classe"
        description={`Matrícula ${matriculaId}`}
        actions={
          <Link className="br-button secondary" to="/educacao/matriculas">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<DiarioClasseResumo>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data ?? undefined}
        empty={
          <EmptyState
            icon="fas fa-book-open"
            title="Nenhum diário aberto"
            description="Esta matrícula ainda não possui um diário de classe."
            action={
              <Can permission="educacao.gerenciar">
                <Button variant="primary" onClick={() => setFormAberto(true)}>
                  <i className="fas fa-plus" aria-hidden="true" /> Abrir diário
                </Button>
              </Can>
            }
          />
        }
      >
        {(diario) => {
          const aberto = diario.situacao === 'Aberto';
          return (
            <>
              <Card header={<strong>Situação do diário</strong>}>
                <dl className="row">
                  <Campo rotulo="Situação">
                    <Tag variant={situacaoDiarioTagVariant(diario.situacao)}>{diario.situacao}</Tag>
                  </Campo>
                  <Campo rotulo="Resultado apurado">
                    {diario.resultado ? (
                      <Tag variant={resultadoAlunoTagVariant(diario.resultado)}>{diario.resultado}</Tag>
                    ) : (
                      '—'
                    )}
                  </Campo>
                  <Campo rotulo="Percentual de frequência">
                    {formatarPercentual(diario.percentualFrequencia)}
                  </Campo>
                  <Campo rotulo="Dias letivos registrados">{diario.diasLetivosRegistrados}</Campo>
                </dl>
              </Card>

              <FrequenciaCard diarioId={diario.id} />

              <Can permission="educacao.gerenciar">
                <Card className="mt-4" header={<strong>Ações</strong>}>
                  <div className="d-flex gap-2 flex-wrap">
                    <Button variant="secondary" onClick={() => setAcao('frequencia')} disabled={!aberto}>
                      <i className="fas fa-clipboard-check" aria-hidden="true" /> Registrar frequência
                    </Button>
                    <Button variant="secondary" onClick={() => setAcao('nota')} disabled={!aberto}>
                      <i className="fas fa-pen-to-square" aria-hidden="true" /> Lançar nota
                    </Button>
                    <Button variant="secondary" onClick={() => setAcao('aula')} disabled={!aberto}>
                      <i className="fas fa-chalkboard" aria-hidden="true" /> Registrar aula
                    </Button>
                    <Button variant="primary" onClick={() => setAcao('apurar')} disabled={!aberto}>
                      <i className="fas fa-flag-checkered" aria-hidden="true" /> Apurar resultado
                    </Button>
                  </div>
                </Card>

                <RegistrarFrequenciaModal
                  open={acao === 'frequencia'}
                  onClose={() => setAcao(null)}
                  matriculaId={matriculaId}
                  diarioId={diario.id}
                />
                <LancarNotaModal
                  open={acao === 'nota'}
                  onClose={() => setAcao(null)}
                  matriculaId={matriculaId}
                  diarioId={diario.id}
                />
                <RegistrarAulaModal
                  open={acao === 'aula'}
                  onClose={() => setAcao(null)}
                  matriculaId={matriculaId}
                  diarioId={diario.id}
                />
                <ApurarResultadoModal
                  open={acao === 'apurar'}
                  onClose={() => setAcao(null)}
                  matriculaId={matriculaId}
                  diarioId={diario.id}
                />
              </Can>
            </>
          );
        }}
      </QueryState>

      <DiarioClasseFormModal
        open={formAberto}
        onClose={() => setFormAberto(false)}
        matriculaId={matriculaId}
      />
    </>
  );
}
