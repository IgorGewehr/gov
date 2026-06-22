// Tela de DETALHE de um servidor (busca por matrícula via param de rota -> useQuery).
// Padrão-ouro: QueryState para loading/erro, layout de Card com pares rótulo/valor
// acessíveis. Expõe os commands de ciclo de vida (posse, exercício, estabilidade,
// afastamento, desligamento) gated por "recursoshumanos.gerenciar" via <Can>.
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Button, Card, EmptyState, PageHeader, QueryState, Tag } from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarData } from '../../i18n/format';
import { useServidorPorMatricula } from './api';
import type { ServidorResumo } from './api';
import { PERM_RH_GERENCIAR, situacaoServidorTagVariant } from './recursosHumanos.helpers';
import {
  ConcederEstabilidadeModal,
  DesligarServidorModal,
  IniciarExercicioModal,
  RegistrarAfastamentoModal,
  RegistrarPosseModal,
} from './ServidorAcaoModais';

type AcaoServidor = 'posse' | 'exercicio' | 'estabilidade' | 'afastamento' | 'desligamento' | null;

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

export function ServidorDetailPage() {
  const { matricula = '' } = useParams<{ matricula: string }>();
  const query = useServidorPorMatricula(matricula);
  const [acao, setAcao] = useState<AcaoServidor>(null);

  return (
    <>
      <PageHeader
        title="Detalhe do Servidor"
        actions={
          <Link className="br-button secondary" to="/recursoshumanos">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<ServidorResumo>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data ?? undefined}
        empty={
          <EmptyState
            icon="fas fa-user-slash"
            title="Servidor não encontrado"
            description={`Nenhum servidor com a matrícula ${matricula}.`}
          />
        }
      >
        {(servidor) => (
          <>
            <Card className="mb-4" header={<strong>{servidor.nomeServidor}</strong>}>
              <dl className="row">
                <Campo rotulo="Matrícula">{servidor.matricula}</Campo>
                <Campo rotulo="CPF">{servidor.cpf}</Campo>
                <Campo rotulo="Situação">
                  <Tag variant={situacaoServidorTagVariant(servidor.situacao)}>
                    {servidor.situacao}
                  </Tag>
                </Campo>
                <Campo rotulo="Regime previdenciário">{servidor.regime}</Campo>
                <Campo rotulo="Data de nomeação">{formatarData(servidor.dataNomeacao)}</Campo>
                <Campo rotulo="Início de exercício">
                  {servidor.dataExercicio ? formatarData(servidor.dataExercicio) : '—'}
                </Campo>
              </dl>
            </Card>

            <Can permission={PERM_RH_GERENCIAR}>
              <Card header={<strong>Ações do vínculo</strong>}>
                <div className="d-flex flex-wrap" style={{ gap: '1rem' }}>
                  <Button variant="secondary" onClick={() => setAcao('posse')}>
                    <i className="fas fa-handshake" aria-hidden="true" /> Registrar posse
                  </Button>
                  <Button variant="secondary" onClick={() => setAcao('exercicio')}>
                    <i className="fas fa-play" aria-hidden="true" /> Iniciar exercício
                  </Button>
                  <Button variant="secondary" onClick={() => setAcao('estabilidade')}>
                    <i className="fas fa-shield" aria-hidden="true" /> Conceder estabilidade
                  </Button>
                  <Button variant="secondary" onClick={() => setAcao('afastamento')}>
                    <i className="fas fa-user-clock" aria-hidden="true" /> Registrar afastamento
                  </Button>
                  <Button variant="danger" onClick={() => setAcao('desligamento')}>
                    <i className="fas fa-user-xmark" aria-hidden="true" /> Desligar
                  </Button>
                </div>
              </Card>

              <RegistrarPosseModal
                open={acao === 'posse'}
                onClose={() => setAcao(null)}
                servidorId={servidor.id}
              />
              <IniciarExercicioModal
                open={acao === 'exercicio'}
                onClose={() => setAcao(null)}
                servidorId={servidor.id}
              />
              <ConcederEstabilidadeModal
                open={acao === 'estabilidade'}
                onClose={() => setAcao(null)}
                servidorId={servidor.id}
              />
              <RegistrarAfastamentoModal
                open={acao === 'afastamento'}
                onClose={() => setAcao(null)}
                servidorId={servidor.id}
              />
              <DesligarServidorModal
                open={acao === 'desligamento'}
                onClose={() => setAcao(null)}
                servidorId={servidor.id}
              />
            </Can>
          </>
        )}
      </QueryState>
    </>
  );
}
