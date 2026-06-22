// Tela de DETALHE de uma escola (param de rota = código INEP). Padrão-ouro:
// param de rota -> useQuery, QueryState para loading/erro, layout de Card com
// pares rótulo/valor acessíveis. Expõe as ações de gestão (atualizar dados do
// Censo / desativar) gated por permissão (educacao.gerenciar).
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Button, Card, PageHeader, QueryState, Tag } from '../../components/ui';
import { Can } from '../../auth/Can';
import { useEscolaPorInep } from './api';
import type { EscolaResumo } from './api';
import { situacaoEscolaTagVariant } from './educacao.helpers';
import { AtualizarDadosCensoModal, DesativarEscolaModal } from './EscolaAcaoModais';

type AcaoAberta = 'censo' | 'desativar' | null;

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

export function EscolaDetailPage() {
  const { codigoInep = '' } = useParams<{ codigoInep: string }>();
  const query = useEscolaPorInep(codigoInep);
  const [acao, setAcao] = useState<AcaoAberta>(null);

  return (
    <>
      <PageHeader
        title="Detalhe da escola"
        actions={
          <Link className="br-button secondary" to="/educacao">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<EscolaResumo>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
        empty={
          <Card>
            <p className="mb-0">Nenhuma escola encontrada para o código INEP informado.</p>
          </Card>
        }
      >
        {(escola) => {
          const desativada = escola.situacao === 'Desativada';
          return (
            <>
              <Card className="mb-4" header={<strong>{escola.nome}</strong>}>
                <dl className="row">
                  <Campo rotulo="Código INEP">{escola.codigoInep}</Campo>
                  <Campo rotulo="Dependência administrativa">{escola.dependencia}</Campo>
                  <Campo rotulo="Situação">
                    <Tag variant={situacaoEscolaTagVariant(escola.situacao)}>{escola.situacao}</Tag>
                  </Campo>
                  <Campo rotulo="Identificador da escola">{escola.id}</Campo>
                </dl>
              </Card>

              <Can permission="educacao.gerenciar">
                <Card header={<strong>Ações</strong>}>
                  <div className="d-flex gap-2 flex-wrap">
                    <Button variant="secondary" onClick={() => setAcao('censo')} disabled={desativada}>
                      <i className="fas fa-pen" aria-hidden="true" /> Atualizar dados do Censo
                    </Button>
                    <Button variant="danger" onClick={() => setAcao('desativar')} disabled={desativada}>
                      <i className="fas fa-ban" aria-hidden="true" /> Desativar escola
                    </Button>
                  </div>
                </Card>

                <AtualizarDadosCensoModal
                  open={acao === 'censo'}
                  onClose={() => setAcao(null)}
                  escolaId={escola.id}
                  codigoInep={escola.codigoInep}
                />
                <DesativarEscolaModal
                  open={acao === 'desativar'}
                  onClose={() => setAcao(null)}
                  escolaId={escola.id}
                  codigoInep={escola.codigoInep}
                />
              </Can>
            </>
          );
        }}
      </QueryState>
    </>
  );
}
