// Tela de DETALHE de uma Familia. Reune:
//  - identidade/resumo da familia (recebido via router state na navegacao da lista);
//  - a query ObterResumoCadUnico (read model federal, somente leitura) com QueryState;
//  - as acoes de command como botoes que abrem Modal+form:
//      * AtualizarRendaFamiliar (atualiza composicao/renda)
//      * ProcessarVigenciaCadastral (avalia vigencia de 24 meses; confirmacao)
// gov.br DS + WCAG AA + i18n/format. Estados loading/vazio/erro.
import { useState } from 'react';
import { Link, useLocation, useParams } from 'react-router-dom';
import { Alert, Button, Card, PageHeader, QueryState, Tag } from '../../../components/ui';
import { formatarData, formatarMoeda } from '../../../i18n/format';
import { useResumoCadUnico, SITUACAO_LABEL, situacaoTagVariant } from './familia.api';
import type { FamiliaResumo, ResumoCadUnico } from './familia.api';
import { AtualizarRendaModal } from './AtualizarRendaModal';
import { ProcessarVigenciaModal } from './ProcessarVigenciaModal';
import { Can } from '../../../auth/Can';

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

interface DetailState {
  familia?: FamiliaResumo;
}

export function FamiliaDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const location = useLocation();
  const familia = (location.state as DetailState | null)?.familia;

  const cadUnicoQuery = useResumoCadUnico(id);

  const [rendaAberta, setRendaAberta] = useState(false);
  const [vigenciaAberta, setVigenciaAberta] = useState(false);

  const vencida = familia?.situacao === 'AtualizacaoVencida';

  return (
    <>
      <PageHeader
        title="Detalhe da família"
        description="Gestão socioeconômica da família referenciada no SUAS."
        actions={
          <Link className="br-button secondary" to="/assistenciasocial">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      {/* Identidade/resumo da familia (origem: lista por territorio). */}
      {familia ? (
        <Card
          className="mb-4"
          header={
            <div className="d-flex justify-content-between align-items-center flex-wrap">
              <strong>NIS (mascarado): {familia.nisMascarado}</strong>
              <Tag variant={situacaoTagVariant(familia.situacao)}>
                {SITUACAO_LABEL[familia.situacao]}
              </Tag>
            </div>
          }
          footer={
            <Can permission="assistenciasocial.gerenciar">
              <div className="d-flex flex-wrap" style={{ gap: '0.5rem' }}>
                <Button variant="primary" onClick={() => setRendaAberta(true)}>
                  <i className="fas fa-money-bill-wave" aria-hidden="true" /> Atualizar renda/composição
                </Button>
                <Button variant="secondary" onClick={() => setVigenciaAberta(true)}>
                  <i className="fas fa-calendar-check" aria-hidden="true" /> Processar vigência cadastral
                </Button>
              </div>
            </Can>
          }
        >
          {vencida && (
            <Alert variant="warning">
              Cadastro vencido: a elegibilidade a novos benefícios está condicionada à regularização
              (atualize a renda/composição para regularizar).
            </Alert>
          )}
          <dl className="row">
            <Campo rotulo="Território">{familia.territorio}</Campo>
            <Campo rotulo="Unidade de atendimento (CRAS)">{familia.unidadeAtendimentoId}</Campo>
            <Campo rotulo="Renda per capita">{formatarMoeda(familia.rendaPerCapita)}</Campo>
            <Campo rotulo="Data de referenciamento">{formatarData(familia.dataReferenciamento)}</Campo>
            <Campo rotulo="Última atualização cadastral">
              {formatarData(familia.dataUltimaAtualizacaoCadastral)}
            </Campo>
          </dl>
        </Card>
      ) : (
        <Alert variant="info">
          Abra o detalhe a partir da lista de famílias do território para ver o resumo completo. As ações
          de gestão abaixo continuam disponíveis.
          <Can permission="assistenciasocial.gerenciar">
            <div className="mt-3 d-flex flex-wrap" style={{ gap: '0.5rem' }}>
              <Button variant="primary" onClick={() => setRendaAberta(true)}>
                <i className="fas fa-money-bill-wave" aria-hidden="true" /> Atualizar renda/composição
              </Button>
              <Button variant="secondary" onClick={() => setVigenciaAberta(true)}>
                <i className="fas fa-calendar-check" aria-hidden="true" /> Processar vigência cadastral
              </Button>
            </div>
          </Can>
        </Alert>
      )}

      {/* Read model federal (CadUnico) — somente leitura, isolado por tenant. */}
      <Card header={<strong>Resumo do CadÚnico (base federal — somente leitura)</strong>}>
        <QueryState<ResumoCadUnico>
          isLoading={cadUnicoQuery.isLoading}
          isError={cadUnicoQuery.isError}
          error={cadUnicoQuery.error}
          data={cadUnicoQuery.data}
        >
          {(resumo) => (
            <dl className="row">
              <Campo rotulo="NIS (mascarado)">{resumo.nisMascarado}</Campo>
              <Campo rotulo="Renda familiar declarada">
                {formatarMoeda(resumo.rendaFamiliarDeclarada)}
              </Campo>
              <Campo rotulo="Quantidade de membros">{resumo.quantidadeMembros}</Campo>
              <Campo rotulo="Última atualização (federal)">
                {formatarData(resumo.dataUltimaAtualizacao)}
              </Campo>
              <Campo rotulo="Vigência (24 meses)">
                <Tag variant={resumo.dentroDaVigencia ? 'success' : 'danger'}>
                  {resumo.dentroDaVigencia ? 'Dentro da vigência' : 'Vencida'}
                </Tag>
              </Campo>
            </dl>
          )}
        </QueryState>
      </Card>

      <AtualizarRendaModal open={rendaAberta} onClose={() => setRendaAberta(false)} familiaId={id} />
      <ProcessarVigenciaModal
        open={vigenciaAberta}
        onClose={() => setVigenciaAberta(false)}
        familiaId={id}
        nisMascarado={familia?.nisMascarado}
      />
    </>
  );
}
