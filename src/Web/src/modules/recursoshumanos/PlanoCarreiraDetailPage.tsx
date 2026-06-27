// Detalhe de um plano de carreira: parâmetros (lei, percentuais, interstício, nota) + a MATRIZ
// salarial completa (classe × referência) com o vencimento derivado de cada célula.
import { useMemo } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Card, PageHeader, QueryState, Tag } from '../../components/ui';
import { formatarMoeda } from '../../i18n/format';
import { usePlanoCarreira } from './api';
import type { CelulaMatriz, PlanoCarreiraDetalhe } from './api';
import { situacaoPlanoCarreiraTagVariant } from './recursosHumanos.helpers';
import { RhSubNav } from './RhSubNav';

export function PlanoCarreiraDetailPage() {
  const { planoId = '' } = useParams<{ planoId: string }>();
  const query = usePlanoCarreira(planoId);

  const grade = useMemo(() => montarGrade(query.data?.matriz ?? []), [query.data]);

  return (
    <>
      <RhSubNav />
      <PageHeader
        eyebrow="Recursos Humanos"
        title={query.data?.resumo.denominacaoCarreira ?? 'Plano de carreira'}
        description="Matriz salarial: cada célula (classe × referência) tem o vencimento derivado do vencimento-base e dos percentuais do plano."
        actions={
          <Link className="br-button secondary" to="/recursoshumanos/planos-carreira">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<PlanoCarreiraDetalhe>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
      >
        {(plano) => (
          <>
            <Card className="mb-4">
              <dl className="row">
                <dt className="col-sm-3">Lei de instituição</dt>
                <dd className="col-sm-9">{plano.resumo.leiInstituicao}</dd>
                <dt className="col-sm-3">Situação</dt>
                <dd className="col-sm-9">
                  <Tag variant={situacaoPlanoCarreiraTagVariant(plano.resumo.situacao)}>
                    {plano.resumo.situacao}
                  </Tag>
                </dd>
                <dt className="col-sm-3">Vencimento-base</dt>
                <dd className="col-sm-9">{formatarMoeda(plano.resumo.vencimentoBase)}</dd>
                <dt className="col-sm-3">% entre referências / classes</dt>
                <dd className="col-sm-9">
                  {plano.percentualEntreReferencias.toLocaleString('pt-BR', {
                    maximumFractionDigits: 4,
                  })}
                  % / {plano.percentualEntreClasses.toLocaleString('pt-BR', { maximumFractionDigits: 4 })}%
                </dd>
                <dt className="col-sm-3">Interstício / nota mínima</dt>
                <dd className="col-sm-9">
                  {plano.intersticioMeses} meses /{' '}
                  {plano.notaMinimaProgressao.toLocaleString('pt-BR', { maximumFractionDigits: 2 })}
                </dd>
              </dl>
            </Card>

            <Card>
              <h3 className="mb-3">Matriz salarial</h3>
              <div style={{ overflowX: 'auto' }}>
                <table className="br-table" aria-label="Matriz salarial classe por referência">
                  <thead>
                    <tr>
                      <th scope="col">Classe \ Referência</th>
                      {grade.referencias.map((ref) => (
                        <th key={ref} scope="col" className="text-right">
                          R{ref}
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    {grade.classes.map((classe) => (
                      <tr key={classe}>
                        <th scope="row">Classe {classe}</th>
                        {grade.referencias.map((ref) => (
                          <td key={ref} className="text-right">
                            {formatarMoeda(grade.valor(classe, ref))}
                          </td>
                        ))}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </Card>
          </>
        )}
      </QueryState>
    </>
  );
}

function montarGrade(matriz: CelulaMatriz[]) {
  const classes = [...new Set(matriz.map((c) => c.classe))].sort((a, b) => a - b);
  const referencias = [...new Set(matriz.map((c) => c.referencia))].sort((a, b) => a - b);
  const indice = new Map(matriz.map((c) => [`${c.classe}-${c.referencia}`, c.vencimento]));
  return {
    classes,
    referencias,
    valor: (classe: number, referencia: number) => indice.get(`${classe}-${referencia}`) ?? 0,
  };
}
