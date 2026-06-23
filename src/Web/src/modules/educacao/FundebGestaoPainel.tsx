// Gestão da DISTRIBUIÇÃO do FUNDEB (E-3) — visão do GESTOR. Não há endpoint de
// leitura da distribuição bruta; o painel expõe as AÇÕES de conciliação por
// origem (cota-parte / VAAF / VAAT / VAAR — Lei 14.113/2020) e o cruzamento com a
// folha (remuneração do magistério, numerador dos 70%). Ações gated por
// "educacao.gerenciar"; alimentam o indicador de 70% exibido acima.
import { useState } from 'react';
import { Alert, Button, Card } from '../../components/ui';
import { Can } from '../../auth/Can';
import { ORIGEM_FUNDEB_LABEL } from './fiscal.api';
import {
  AbrirDistribuicaoFundebModal,
  MovimentoFundebModal,
  RegistrarRemuneracaoModal,
} from './FundebAcaoModais';

export interface FundebGestaoPainelProps {
  exercicio: number;
}

type AcaoFundeb = 'abrir' | 'esperado' | 'parcela' | 'remuneracao' | null;

export function FundebGestaoPainel({ exercicio }: FundebGestaoPainelProps) {
  const [distribuicaoId, setDistribuicaoId] = useState('');
  const [acao, setAcao] = useState<AcaoFundeb>(null);

  return (
    <Card className="mb-4" header={<strong>Distribuição do FUNDEB — gestão</strong>}>
      <p className="text-gray-60">
        Concilie o recebido por origem e registre a remuneração do magistério do exercício{' '}
        <strong>{exercicio}</strong>. As origens são:{' '}
        {Object.values(ORIGEM_FUNDEB_LABEL).join('; ')}.
      </p>

      {distribuicaoId !== '' && (
        <Alert variant="success" title="Distribuição ativa">
          Distribuição <strong>{distribuicaoId}</strong> — use Definir esperado / Receber parcela.
        </Alert>
      )}

      <Can permission="educacao.gerenciar">
        <div className="d-flex gap-2 flex-wrap mt-2">
          <Button variant="secondary" onClick={() => setAcao('abrir')}>
            <i className="fas fa-folder-plus" aria-hidden="true" /> Abrir distribuição
          </Button>
          <Button
            variant="secondary"
            onClick={() => setAcao('esperado')}
            disabled={distribuicaoId === ''}
          >
            <i className="fas fa-bullseye" aria-hidden="true" /> Definir esperado
          </Button>
          <Button
            variant="secondary"
            onClick={() => setAcao('parcela')}
            disabled={distribuicaoId === ''}
          >
            <i className="fas fa-arrow-down" aria-hidden="true" /> Receber parcela
          </Button>
          <Button variant="primary" onClick={() => setAcao('remuneracao')}>
            <i className="fas fa-user-tie" aria-hidden="true" /> Registrar remuneração
          </Button>
        </div>
      </Can>

      <Alert variant="warning" title="Conciliação, não recálculo">
        Quem rateia/complementa é o ente estadual/FNDE; o município não recalcula a cota, apenas
        comprova. Fatores de ponderação e valores VAAF/VAAT/VAAR são parametrizáveis e dependem do ato
        vigente (// validar-oficial).
      </Alert>

      <AbrirDistribuicaoFundebModal
        open={acao === 'abrir'}
        exercicio={exercicio}
        onClose={() => setAcao(null)}
        onCriado={(id) => setDistribuicaoId(id)}
      />
      <MovimentoFundebModal
        open={acao === 'esperado' || acao === 'parcela'}
        tipo={acao === 'esperado' ? 'esperado' : 'parcela'}
        distribuicaoId={distribuicaoId}
        onClose={() => setAcao(null)}
      />
      <RegistrarRemuneracaoModal
        open={acao === 'remuneracao'}
        exercicio={exercicio}
        onClose={() => setAcao(null)}
      />
    </Card>
  );
}
