// Tela do CICLO ANUAL da folha (13º salário, férias e rescisão). Cada ação gera uma folha
// do Tipo próprio (DecimoTerceiro/Ferias/Rescisao) e navega ao demonstrativo da folha
// resultante (detalhe da folha, reusando o contracheque existente). Padrão-ouro: páginas
// enxutas, Can gating na ação, gov.br DS, PT-BR, acessível.
import { useState } from 'react';
import { Card, PageHeader } from '../../components/ui';
import { Can } from '../../auth/Can';
import { PERM_RH_GERENCIAR } from './recursosHumanos.helpers';
import { RhSubNav } from './RhSubNav';
import { GerarDecimoTerceiroFormModal } from './GerarDecimoTerceiroFormModal';
import { GerarFeriasFormModal } from './GerarFeriasFormModal';
import { GerarRescisaoFormModal } from './GerarRescisaoFormModal';

const ANO_ATUAL = new Date().getFullYear();
const MES_ATUAL = new Date().getMonth() + 1;

interface CardCiclo {
  chave: 'decimoTerceiro' | 'ferias' | 'rescisao';
  titulo: string;
  icone: string;
  descricao: string;
  acao: string;
}

const CARDS: CardCiclo[] = [
  {
    chave: 'decimoTerceiro',
    titulo: '13º salário',
    icone: 'fas fa-gift',
    descricao:
      'Gratificação natalina em duas parcelas: 1ª (adiantamento sem descontos) e 2ª (integral com INSS/RPPS/IRRF em base separada).',
    acao: 'Gerar 13º',
  },
  {
    chave: 'ferias',
    titulo: 'Férias',
    icone: 'fas fa-umbrella-beach',
    descricao:
      'Remuneração do período + 1/3 constitucional, com abono pecuniário opcional (venda de até 1/3) e seu terço.',
    acao: 'Gerar férias',
  },
  {
    chave: 'rescisao',
    titulo: 'Rescisão',
    icone: 'fas fa-file-signature',
    descricao:
      'Verbas rescisórias compostas pela matriz tipo de desligamento × regime (saldo, 13º e férias proporcionais, aviso e multa para celetista).',
    acao: 'Gerar rescisão',
  },
];

export function CicloAnualPage() {
  const [aberto, setAberto] = useState<CardCiclo['chave'] | null>(null);

  return (
    <>
      <RhSubNav />
      <PageHeader
        title="Ciclo anual da folha"
        description="13º salário, férias e rescisão — cada geração produz sua folha (Tipo próprio) e leva ao demonstrativo."
      />

      <div className="row">
        {CARDS.map((card) => (
          <div key={card.chave} className="col-md-6 col-lg-4 mb-4">
            <Card header={<strong>{card.titulo}</strong>}>
              <p className="text-down-01 text-gray-80">
                <i className={`${card.icone} mr-2`} aria-hidden="true" />
                {card.descricao}
              </p>
              <Can permission={PERM_RH_GERENCIAR}>
                <button
                  type="button"
                  className="br-button primary"
                  onClick={() => setAberto(card.chave)}
                >
                  <i className="fas fa-plus" aria-hidden="true" /> {card.acao}
                </button>
              </Can>
            </Card>
          </div>
        ))}
      </div>

      <GerarDecimoTerceiroFormModal
        open={aberto === 'decimoTerceiro'}
        onClose={() => setAberto(null)}
        anoInicial={ANO_ATUAL}
      />
      <GerarFeriasFormModal
        open={aberto === 'ferias'}
        onClose={() => setAberto(null)}
        anoInicial={ANO_ATUAL}
        mesInicial={MES_ATUAL}
      />
      <GerarRescisaoFormModal
        open={aberto === 'rescisao'}
        onClose={() => setAberto(null)}
      />
    </>
  );
}
