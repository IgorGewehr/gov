// Tela de DETALHE de uma edicao do Diario Oficial. Em montagem: permite adicionar
// materias e PUBLICAR (verbo fino, gated `legislativo.diario.publicar`). Publicada:
// exibe a visualizacao oficial e imutavel da edicao com suas materias ordenadas.
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Button, Card, EmptyState, PageHeader, QueryState, Tag } from '../../components/ui';
import { formatarDataHora } from './legislativo.helpers';
import { Can } from '../../auth/Can';
import { ConfirmarAcaoModal } from './ConfirmarAcaoModal';
import { MateriaFormModal } from './MateriaFormModal';
import { usePublicarEdicao, useEdicao } from './diario.api';
import type { EdicaoDetalhe, MateriaResumo } from './diario.api';
import { situacaoEdicaoTagVariant } from './legislativo.helpers';

function Materia({ materia }: { materia: MateriaResumo }) {
  return (
    <Card className="mb-3">
      <div className="d-flex align-items-baseline mb-2" style={{ gap: '0.5rem' }}>
        <span className="text-gray-60">{materia.ordem}.</span>
        <Tag variant="info">{materia.tipo}</Tag>
        <strong>{materia.titulo}</strong>
      </div>
      <p className="mb-0" style={{ whiteSpace: 'pre-wrap' }}>
        {materia.conteudo ?? ''}
      </p>
    </Card>
  );
}

export function EdicaoDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const query = useEdicao(id);
  const publicar = usePublicarEdicao(id);
  const [materiaAberta, setMateriaAberta] = useState(false);
  const [publicarAberto, setPublicarAberto] = useState(false);

  return (
    <>
      <PageHeader
        title="Edição do Diário"
        actions={
          <Link className="br-button secondary" to="/legislativo/diario">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<EdicaoDetalhe>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
      >
        {(edicao) => {
          const emMontagem = edicao.situacao !== 'Publicada';
          return (
            <>
              <Card className="mb-4" header={<strong>Edição nº {edicao.numero}</strong>}>
                <dl className="row">
                  <div className="col-sm-4 mb-3">
                    <dt className="text-gray-60 text-down-01">Situação</dt>
                    <dd className="mb-0">
                      <Tag variant={situacaoEdicaoTagVariant(edicao.situacao)}>{edicao.situacao}</Tag>
                    </dd>
                  </div>
                  <div className="col-sm-4 mb-3">
                    <dt className="text-gray-60 text-down-01">Ano</dt>
                    <dd className="mb-0 text-semi-bold">{edicao.ano}</dd>
                  </div>
                  <div className="col-sm-4 mb-3">
                    <dt className="text-gray-60 text-down-01">Publicada em</dt>
                    <dd className="mb-0 text-semi-bold">{formatarDataHora(edicao.dataPublicacao)}</dd>
                  </div>
                </dl>
              </Card>

              {emMontagem && (
                <Can permission="legislativo.diario.publicar">
                  <div
                    className="d-flex flex-wrap gap-2 mb-4"
                    role="group"
                    aria-label="Ações da edição"
                  >
                    <Button variant="secondary" onClick={() => setMateriaAberta(true)}>
                      <i className="fas fa-plus" aria-hidden="true" /> Adicionar matéria
                    </Button>
                    <Button
                      variant="primary"
                      onClick={() => setPublicarAberto(true)}
                      disabled={edicao.materias.length === 0}
                    >
                      <i className="fas fa-stamp" aria-hidden="true" /> Publicar edição
                    </Button>
                  </div>

                  <MateriaFormModal
                    open={materiaAberta}
                    onClose={() => setMateriaAberta(false)}
                    edicaoId={edicao.id}
                  />
                  <ConfirmarAcaoModal
                    open={publicarAberto}
                    onClose={() => setPublicarAberto(false)}
                    title="Publicar edição"
                    mensagem="Após publicada, a edição torna-se oficial e imutável. Confirma a publicação?"
                    rotuloConfirmar="Publicar"
                    mutation={publicar}
                    sucesso="Edição publicada."
                    erroFallback="Não foi possível publicar a edição."
                  />
                </Can>
              )}

              <h2 className="text-up-01 mb-3">Matérias</h2>
              {edicao.materias.length === 0 ? (
                <EmptyState
                  icon="fas fa-file-lines"
                  title="Nenhuma matéria"
                  description="Adicione matérias à edição antes de publicá-la."
                />
              ) : (
                [...edicao.materias]
                  .sort((a, b) => a.ordem - b.ordem)
                  .map((m) => <Materia key={m.id} materia={m} />)
              )}
            </>
          );
        }}
      </QueryState>
    </>
  );
}
