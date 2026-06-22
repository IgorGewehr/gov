// Tela de PARÂMETROS do IPTU: ponto de entrada para configurar a Planta Genérica
// de Valores (PGV) e a tabela de alíquotas por exercício. Cada parâmetro é
// publicado por um Modal dedicado (PgvFormModal / AliquotasFormModal). Toda ação
// é gated por "tributos.gerenciar". A apuração consome estes parâmetros.
import { useState } from 'react';
import { Button, Card, PageHeader } from '../../components/ui';
import { Can } from '../../auth/Can';
import { TributosSubNav } from './TributosSubNav';
import { PgvFormModal } from './PgvFormModal';
import { AliquotasFormModal } from './AliquotasFormModal';

const PERM_GERENCIAR = 'tributos.gerenciar';

export function IptuParametrosPage() {
  const [pgvAberto, setPgvAberto] = useState(false);
  const [aliquotasAberto, setAliquotasAberto] = useState(false);

  return (
    <>
      <PageHeader
        title="Parâmetros do IPTU"
        description="Configure a Planta de Valores e a tabela de alíquotas que alimentam a apuração."
      />

      <TributosSubNav />

      <div className="row">
        <div className="col-md-6 mb-4">
          <Card
            header={<strong>Planta Genérica de Valores (PGV)</strong>}
            footer={
              <Can permission={PERM_GERENCIAR}>
                <div className="d-flex justify-content-end">
                  <Button variant="primary" onClick={() => setPgvAberto(true)}>
                    <i className="fas fa-map-location-dot" aria-hidden="true" /> Publicar PGV
                  </Button>
                </div>
              </Can>
            }
          >
            <p className="mb-0">
              Define, por exercício, o Valor Unitário de Terreno (VUT, R$/m²) e o Valor Unitário de
              Construção (VUC, R$/m²) de cada zona, além de fatores de correção. É a base do valor venal.
            </p>
          </Card>
        </div>

        <div className="col-md-6 mb-4">
          <Card
            header={<strong>Tabela de alíquotas</strong>}
            footer={
              <Can permission={PERM_GERENCIAR}>
                <div className="d-flex justify-content-end">
                  <Button variant="primary" onClick={() => setAliquotasAberto(true)}>
                    <i className="fas fa-percent" aria-hidden="true" /> Publicar alíquotas
                  </Button>
                </div>
              </Can>
            }
          >
            <p className="mb-0">
              Define o regime (única ou progressiva) e as alíquotas predial e territorial, o desconto de
              cota única e a quantidade padrão de parcelas do exercício.
            </p>
          </Card>
        </div>
      </div>

      <PgvFormModal open={pgvAberto} onClose={() => setPgvAberto(false)} />
      <AliquotasFormModal open={aliquotasAberto} onClose={() => setAliquotasAberto(false)} />
    </>
  );
}
