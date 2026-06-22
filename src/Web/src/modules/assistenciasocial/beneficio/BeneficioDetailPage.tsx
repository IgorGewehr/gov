// Tela de DETALHE de um beneficio. Como o contrato real nao expoe GET por id de beneficio,
// o detalhe e derivado da query ObterBeneficiosDaFamilia (a rota carrega familiaId + beneficioId).
// Hospeda a acao de transicao EntregarCestaBasica (command) como botao -> Modal, com a guarda I-8.
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Alert, Button, Card, PageHeader, QueryState, Tag } from '../../../components/ui';
import { formatarData, formatarMoeda } from '../../../i18n/format';
import { useBeneficiosDaFamilia } from './beneficio.api';
import type { BeneficioResumo } from './beneficio.api';
import { podeEntregarCesta, situacaoLabel, situacaoTagVariant, tipoLabel } from './beneficio.helpers';
import { EntregarCestaModal } from './EntregarCestaModal';
import { Can } from '../../../auth/Can';

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

export function BeneficioDetailPage() {
  const { familiaId = '', id = '' } = useParams<{ familiaId: string; id: string }>();
  const query = useBeneficiosDaFamilia(familiaId);
  const [cestaAberta, setCestaAberta] = useState(false);

  // Deriva o beneficio especifico da lista da familia.
  const beneficio: BeneficioResumo | undefined = query.data?.find((b) => b.id === id);

  return (
    <>
      <PageHeader
        title="Detalhe do benefício"
        actions={
          <Link className="br-button secondary" to="/assistenciasocial">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<BeneficioResumo[]>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
      >
        {() =>
          beneficio ? (
            <Card
              header={<strong>{tipoLabel(beneficio.tipo)}</strong>}
              footer={
                podeEntregarCesta(beneficio.situacao, beneficio.tipo) ? (
                  <Can permission="assistenciasocial.gerenciar">
                    <Button variant="primary" onClick={() => setCestaAberta(true)}>
                      <i className="fas fa-box-open" aria-hidden="true" /> Entregar cesta básica
                    </Button>
                  </Can>
                ) : undefined
              }
            >
              <dl className="row">
                <Campo rotulo="Situação">
                  <Tag variant={situacaoTagVariant(beneficio.situacao)}>
                    {situacaoLabel(beneficio.situacao)}
                  </Tag>
                </Campo>
                <Campo rotulo="Tipo">{tipoLabel(beneficio.tipo)}</Campo>
                <Campo rotulo="Competência">{beneficio.competencia}</Campo>
                <Campo rotulo="Valor">
                  {beneficio.valor != null ? formatarMoeda(beneficio.valor) : '—'}
                </Campo>
                <Campo rotulo="Data da decisão">{formatarData(beneficio.dataDecisao)}</Campo>
                <Campo rotulo="Identificador da família">{beneficio.familiaId}</Campo>
                <Campo rotulo="Motivo do indeferimento">
                  {beneficio.motivoIndeferimento ?? '—'}
                </Campo>
              </dl>

              {beneficio.situacao === 'Concedida' && beneficio.tipo !== 'Eventual' && (
                <Alert variant="info">
                  A entrega de cesta básica é restrita a benefícios eventuais (cesta básica).
                </Alert>
              )}
            </Card>
          ) : (
            <Alert variant="warning">
              Benefício não encontrado para esta família. Verifique se a consulta foi realizada
              ou volte para a lista.
            </Alert>
          )
        }
      </QueryState>

      {beneficio && (
        <EntregarCestaModal
          open={cestaAberta}
          onClose={() => setCestaAberta(false)}
          beneficio={beneficio}
        />
      )}
    </>
  );
}
