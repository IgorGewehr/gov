// Tela de DETALHE de um cargo (param de rota -> useQuery). Padrão-ouro:
// QueryState para loading/erro, layout de Card com pares rótulo/valor acessíveis.
// Expõe os commands (provimento, vacância, vencimento, extinção) gated por
// "recursoshumanos.gerenciar" via <Can>.
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Button, Card, PageHeader, QueryState, Tag } from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarMoeda } from '../../i18n/format';
import { useCargo } from './api';
import type { CargoDetalhe } from './api';
import { PERM_RH_GERENCIAR, situacaoCargoTagVariant } from './recursosHumanos.helpers';
import {
  AlterarVencimentoModal,
  ExtinguirCargoModal,
  ProverCargoModal,
  VagarCargoModal,
} from './CargoAcaoModais';

type AcaoCargo = 'provimento' | 'vacancia' | 'vencimento' | 'extincao' | null;

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

export function CargoDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const query = useCargo(id);
  const [acao, setAcao] = useState<AcaoCargo>(null);

  return (
    <>
      <PageHeader
        title="Detalhe do Cargo"
        actions={
          <Link className="br-button secondary" to="/recursoshumanos/cargos">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<CargoDetalhe>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
      >
        {(cargo) => (
          <>
            <Card className="mb-4" header={<strong>{cargo.denominacao}</strong>}>
              <dl className="row">
                <Campo rotulo="Situação">
                  <Tag variant={situacaoCargoTagVariant(cargo.situacao)}>{cargo.situacao}</Tag>
                </Campo>
                <Campo rotulo="Tipo">{cargo.tipo}</Campo>
                <Campo rotulo="Regime previdenciário">{cargo.regime}</Campo>
                <Campo rotulo="Vencimento-base">{formatarMoeda(cargo.vencimento)}</Campo>
                <Campo rotulo="Lotação">{cargo.lotacao}</Campo>
                <Campo rotulo="Lei de criação">{cargo.leiCriacao}</Campo>
                <Campo rotulo="Vagas autorizadas">{cargo.quantidadeVagas}</Campo>
                <Campo rotulo="Vagas ocupadas">
                  {cargo.vagasOcupadas} de {cargo.quantidadeVagas}
                </Campo>
              </dl>
            </Card>

            <Can permission={PERM_RH_GERENCIAR}>
              <Card header={<strong>Ações do cargo</strong>}>
                <div className="d-flex flex-wrap" style={{ gap: '1rem' }}>
                  <Button variant="secondary" onClick={() => setAcao('provimento')}>
                    <i className="fas fa-user-check" aria-hidden="true" /> Prover vaga
                  </Button>
                  <Button variant="secondary" onClick={() => setAcao('vacancia')}>
                    <i className="fas fa-user-minus" aria-hidden="true" /> Vagar
                  </Button>
                  <Button variant="secondary" onClick={() => setAcao('vencimento')}>
                    <i className="fas fa-coins" aria-hidden="true" /> Alterar vencimento
                  </Button>
                  <Button variant="danger" onClick={() => setAcao('extincao')}>
                    <i className="fas fa-ban" aria-hidden="true" /> Extinguir
                  </Button>
                </div>
              </Card>

              <ProverCargoModal
                open={acao === 'provimento'}
                onClose={() => setAcao(null)}
                cargoId={cargo.id}
              />
              <VagarCargoModal
                open={acao === 'vacancia'}
                onClose={() => setAcao(null)}
                cargoId={cargo.id}
              />
              <AlterarVencimentoModal
                open={acao === 'vencimento'}
                onClose={() => setAcao(null)}
                cargoId={cargo.id}
              />
              <ExtinguirCargoModal
                open={acao === 'extincao'}
                onClose={() => setAcao(null)}
                cargoId={cargo.id}
              />
            </Can>
          </>
        )}
      </QueryState>
    </>
  );
}
