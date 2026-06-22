// Tela das TABELAS LEGAIS (INSS/IRRF federais + RPPS municipal) — base do motor de
// descontos da folha. Não há GET (somente escrita): a página orienta e aciona a
// semeadura federal e o cadastro do RPPS municipal. Padrão-ouro: Card + Can gating.
import { useState } from 'react';
import { Alert, Button, Card, PageHeader } from '../../components/ui';
import { Can } from '../../auth/Can';
import { PERM_RH_GERENCIAR } from './recursosHumanos.helpers';
import { SemearTabelasFederaisModal } from './SemearTabelasFederaisModal';
import { CriarTabelaRppsFormModal } from './CriarTabelaRppsFormModal';
import { RhSubNav } from './RhSubNav';

const ANO_ATUAL = new Date().getFullYear();

export function TabelasLegaisPage() {
  const [semearAberto, setSemearAberto] = useState(false);
  const [rppsAberto, setRppsAberto] = useState(false);

  return (
    <>
      <RhSubNav />
      <PageHeader
        title="Tabelas Legais"
        description="Parâmetros oficiais de INSS, IRRF e RPPS que alimentam o motor de descontos da folha."
      />

      <Alert variant="info" title="Por que isto importa.">
        O cálculo de INSS, IRRF e RPPS usa estas tabelas como dado parametrizado (nunca
        embutido no motor). As tabelas federais são oficiais; a tabela RPPS é municipal e
        obrigatória para o cálculo de servidores efetivos.
      </Alert>

      <Card className="mb-4" header={<strong>Tabelas federais (INSS / IRRF)</strong>}>
        <p className="mb-3">
          Semeia, para o seu ente, as tabelas progressivas federais oficiais de INSS e IRRF
          por competência. Operação idempotente e auditada.
        </p>
        <Can permission={PERM_RH_GERENCIAR}>
          <Button variant="primary" onClick={() => setSemearAberto(true)}>
            <i className="fas fa-seedling" aria-hidden="true" /> Semear tabelas federais
          </Button>
        </Can>
      </Card>

      <Card header={<strong>Tabela RPPS municipal</strong>}>
        <p className="mb-3">
          Cadastra a tabela do Regime Próprio de Previdência conforme a lei previdenciária
          do município. Sem ela, o cálculo de servidores efetivos é recusado (fail-closed).
        </p>
        <Can permission={PERM_RH_GERENCIAR}>
          <Button variant="secondary" onClick={() => setRppsAberto(true)}>
            <i className="fas fa-plus" aria-hidden="true" /> Cadastrar tabela RPPS
          </Button>
        </Can>
      </Card>

      <SemearTabelasFederaisModal
        open={semearAberto}
        onClose={() => setSemearAberto(false)}
      />
      <CriarTabelaRppsFormModal
        open={rppsAberto}
        onClose={() => setRppsAberto(false)}
        anoInicial={ANO_ATUAL}
        mesInicial={new Date().getMonth() + 1}
      />
    </>
  );
}
