// Ficha (detalhe) do Estabelecimento (CNES). Param de rota -> useQuery + QueryState.
// Ações gated por "saude.gerenciar": editar (PUT) e inativar/reativar (ciclo de vida).
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  Button,
  Card,
  CardSecao,
  PageHeader,
  QueryState,
  Tag,
} from '../../components/ui';
import { Can } from '../../auth/Can';
import {
  useEstabelecimento,
  useInativarEstabelecimento,
  useReativarEstabelecimento,
} from './api';
import type { EstabelecimentoDetalhe } from './api';
import { situacaoCadastroVariant } from './saude.helpers';
import { EstabelecimentoFormModal } from './EstabelecimentoFormModal';
import { useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';

export function EstabelecimentoDetailPage() {
  const { estabelecimentoId = '' } = useParams<{ estabelecimentoId: string }>();
  const toast = useToast();
  const query = useEstabelecimento(estabelecimentoId);
  const inativar = useInativarEstabelecimento(estabelecimentoId);
  const reativar = useReativarEstabelecimento(estabelecimentoId);
  const [editarAberto, setEditarAberto] = useState(false);

  function alternarSituacao(ativo: boolean): void {
    const mutation = ativo ? inativar : reativar;
    const acao = ativo ? 'inativado' : 'reativado';
    mutation.mutate(undefined, {
      onSuccess: () => toast.success(`Estabelecimento ${acao}.`, 'Sucesso'),
      onError: (error) =>
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível alterar a situação.',
        ),
    });
  }

  return (
    <>
      <PageHeader
        eyebrow="Saúde · Estabelecimentos"
        title="Ficha do estabelecimento"
        description="Dados cadastrais do estabelecimento de saúde (CNES)."
        actions={
          <Link className="br-button secondary" to="/saude/estabelecimentos">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<EstabelecimentoDetalhe>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
      >
        {(estabelecimento) => {
          const ativo = estabelecimento.situacao === 'Ativo';
          return (
            <>
              <Can permission="saude.gerenciar">
                <Card className="mb-4" header={<strong>Ações</strong>}>
                  <div className="d-flex gap-2 flex-wrap">
                    <Button variant="secondary" onClick={() => setEditarAberto(true)}>
                      <i className="fas fa-pen" aria-hidden="true" /> Editar
                    </Button>
                    {ativo ? (
                      <Button
                        variant="danger"
                        loading={inativar.isPending}
                        onClick={() => alternarSituacao(true)}
                      >
                        <i className="fas fa-ban" aria-hidden="true" /> Inativar
                      </Button>
                    ) : (
                      <Button
                        variant="secondary"
                        loading={reativar.isPending}
                        onClick={() => alternarSituacao(false)}
                      >
                        <i className="fas fa-rotate-left" aria-hidden="true" /> Reativar
                      </Button>
                    )}
                  </div>
                </Card>
              </Can>

              <CardSecao
                className="mb-4"
                titulo={estabelecimento.nome}
                subtitulo={`CNES ${estabelecimento.cnes} · ${estabelecimento.tipo}`}
                acao={
                  <Tag variant={situacaoCadastroVariant(estabelecimento.situacao)}>
                    {estabelecimento.situacao}
                  </Tag>
                }
              >
                <dl className="row mb-0">
                  <dt className="col-sm-3 text-semi-bold">Logradouro</dt>
                  <dd className="col-sm-9">
                    {estabelecimento.logradouro}
                    {estabelecimento.numero ? `, ${estabelecimento.numero}` : ''}
                  </dd>
                  <dt className="col-sm-3 text-semi-bold">Bairro</dt>
                  <dd className="col-sm-9">{estabelecimento.bairro || '—'}</dd>
                  <dt className="col-sm-3 text-semi-bold">Município/UF</dt>
                  <dd className="col-sm-9">
                    {estabelecimento.municipio || '—'}/{estabelecimento.uf || '—'}
                  </dd>
                  <dt className="col-sm-3 text-semi-bold">CEP</dt>
                  <dd className="col-sm-9">{estabelecimento.cep || '—'}</dd>
                </dl>
              </CardSecao>

              <EstabelecimentoFormModal
                open={editarAberto}
                onClose={() => setEditarAberto(false)}
                estabelecimento={estabelecimento}
              />
            </>
          );
        }}
      </QueryState>
    </>
  );
}
