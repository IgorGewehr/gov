// Componente do PLACAR ao vivo de uma votacao (painel eletronico). Recebe o
// PainelVotacao ja carregado e renderiza: totais Sim/Nao/Abstencao, quorum,
// resultado apurado e a lista NOMINAL de votos. Acessivel (region + aria-live).
import { Card, Tag } from '../../components/ui';
import type { TagVariant } from '../../components/ui';
import type { PainelVotacao, VotoNominalPainel } from './votacao.api';
import { resultadoVotacaoTagVariant, situacaoVotacaoTagVariant } from './legislativo.helpers';

function sentidoVariant(sentido: string): TagVariant {
  switch (sentido) {
    case 'Sim':
      return 'success';
    case 'Não':
    case 'Nao':
      return 'danger';
    case 'Abstenção':
    case 'Abstencao':
      return 'warning';
    default:
      return 'default';
  }
}

function Numero({ rotulo, valor, variant }: { rotulo: string; valor: number; variant: TagVariant }) {
  return (
    <div className="col-6 col-sm-4 mb-3 text-center">
      <Tag variant={variant}>{rotulo}</Tag>
      <p className="text-up-03 text-semi-bold mb-0 mt-1" aria-label={`${rotulo}: ${valor}`}>
        {valor}
      </p>
    </div>
  );
}

export interface PainelPlacarProps {
  painel: PainelVotacao;
  /** Indica que ha um refetch em andamento (mostra pulso "ao vivo"). */
  atualizando: boolean;
}

export function PainelPlacar({ painel, atualizando }: PainelPlacarProps) {
  const aberta = painel.situacao === 'Aberta';
  return (
    <section aria-label="Placar da votação" aria-live="polite" aria-busy={atualizando}>
      <Card
        className="mb-4"
        header={
          <div className="d-flex justify-content-between align-items-center">
            <strong>Placar da votação</strong>
            <span>
              <Tag variant={situacaoVotacaoTagVariant(painel.situacao)}>{painel.situacao}</Tag>{' '}
              {aberta && (
                <Tag variant="danger">
                  <i className="fas fa-circle fa-fade" aria-hidden="true" /> AO VIVO
                </Tag>
              )}
            </span>
          </div>
        }
      >
        <div className="row">
          <Numero rotulo="Sim" valor={painel.sim} variant="success" />
          <Numero rotulo="Não" valor={painel.nao} variant="danger" />
          <Numero rotulo="Abstenção" valor={painel.abstencao} variant="warning" />
        </div>

        <dl className="row mt-2">
          <div className="col-6 col-sm-3 mb-2">
            <dt className="text-gray-60 text-down-01">Total de votos</dt>
            <dd className="mb-0 text-semi-bold">{painel.totalVotos}</dd>
          </div>
          <div className="col-6 col-sm-3 mb-2">
            <dt className="text-gray-60 text-down-01">Presentes</dt>
            <dd className="mb-0 text-semi-bold">{painel.presentes}</dd>
          </div>
          <div className="col-6 col-sm-3 mb-2">
            <dt className="text-gray-60 text-down-01">Ausentes</dt>
            <dd className="mb-0 text-semi-bold">{painel.ausentes}</dd>
          </div>
          <div className="col-6 col-sm-3 mb-2">
            <dt className="text-gray-60 text-down-01">Quórum mínimo</dt>
            <dd className="mb-0 text-semi-bold">
              {painel.quorumMinimo}{' '}
              <Tag variant={painel.quorumAtingido ? 'success' : 'warning'}>
                {painel.quorumAtingido ? 'atingido' : 'não atingido'}
              </Tag>
            </dd>
          </div>
          <div className="col-12 mb-0">
            <dt className="text-gray-60 text-down-01">
              {aberta ? 'Resultado parcial' : 'Resultado apurado'}
            </dt>
            <dd className="mb-0">
              <Tag variant={resultadoVotacaoTagVariant(painel.resultadoParcial)}>
                {painel.resultadoParcial}
              </Tag>
            </dd>
          </div>
        </dl>
      </Card>

      <h2 className="text-up-01 mb-3">Votos nominais</h2>
      {painel.votos.length === 0 ? (
        <p className="text-gray-60">Nenhum voto registrado até o momento.</p>
      ) : (
        <ul className="br-list">
          {painel.votos.map((v: VotoNominalPainel) => (
            <li
              key={v.vereadorId}
              className="br-item d-flex justify-content-between align-items-center"
            >
              <span className="text-semi-bold">{v.nomeVereador}</span>
              <Tag variant={sentidoVariant(v.sentido)}>{v.sentido}</Tag>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
