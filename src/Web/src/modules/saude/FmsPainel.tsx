// Painel do FUNDO MUNICIPAL DE SAÚDE (FMS) — saldos segregados por bloco de
// financiamento (Custeio/Investimento — Port. GM/MS 3.992/2017; transposição
// custeio↔investimento é vedada). O gestor informa o identificador do fundo
// (não há endpoint de listagem); pode abrir um novo fundo e movimentar blocos.
// Leitura gated por "saude.ver"; ações por "saude.gerenciar".
import { useState } from 'react';
import {
  Alert,
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  Input,
  QueryState,
  useToast,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarMoeda } from '../../i18n/format';
import { BLOCO_SAUDE_LABEL, useExecucaoFms } from './fiscal.api';
import type { ExecucaoFms, SaldoBlocoFms } from './fiscal.api';
import { AbrirFmsModal, MovimentoFmsModal } from './FmsAcaoModais';

const COLUNAS: Column<SaldoBlocoFms>[] = [
  { key: 'bloco', header: 'Bloco', render: (b) => BLOCO_SAUDE_LABEL[b.bloco] ?? `Bloco ${b.bloco}` },
  { key: 'recebido', header: 'Recebido (FNS)', render: (b) => formatarMoeda(b.recebido), align: 'end' },
  { key: 'executado', header: 'Executado', render: (b) => formatarMoeda(b.executado), align: 'end' },
  { key: 'saldo', header: 'Saldo', render: (b) => formatarMoeda(b.saldo), align: 'end' },
];

export function FmsPainel() {
  const toast = useToast();
  const [fundoInput, setFundoInput] = useState('');
  const [fundoId, setFundoId] = useState('');
  const [abrirAberto, setAbrirAberto] = useState(false);
  const [movimento, setMovimento] = useState<'parcela' | 'execucao' | null>(null);

  const query = useExecucaoFms(fundoId);

  function consultar(): void {
    const id = fundoInput.trim();
    if (id === '') {
      toast.error('Informe o identificador do Fundo Municipal de Saúde.');
      return;
    }
    setFundoId(id);
  }

  return (
    <Card
      className="mb-4"
      header={
        <div className="d-flex justify-content-between align-items-center">
          <strong>Fundo Municipal de Saúde — saldos por bloco</strong>
          <Can permission="saude.gerenciar">
            <Button variant="secondary" onClick={() => setAbrirAberto(true)}>
              <i className="fas fa-plus" aria-hidden="true" /> Abrir fundo
            </Button>
          </Can>
        </div>
      }
    >
      <div className="row align-items-end mb-3">
        <div className="col-md-8">
          <FormField label="Identificador do Fundo (FMS)">
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                aria-describedby={describedBy}
                invalid={invalid}
                value={fundoInput}
                onChange={(e) => setFundoInput(e.target.value)}
                placeholder="00000000-0000-0000-0000-000000000000"
              />
            )}
          </FormField>
        </div>
        <div className="col-md-4 mb-3">
          <Button variant="primary" onClick={consultar} loading={query.isFetching && fundoId !== ''}>
            <i className="fas fa-magnifying-glass" aria-hidden="true" /> Consultar saldos
          </Button>
        </div>
      </div>

      {fundoId === '' ? (
        <EmptyState
          icon="fas fa-building-columns"
          title="Informe o fundo"
          description="Consulte os saldos segregados por bloco de financiamento do FMS."
        />
      ) : (
        <QueryState<ExecucaoFms>
          isLoading={query.isLoading}
          isError={query.isError}
          error={query.error}
          data={query.data}
        >
          {(fms) => (
            <>
              <p className="text-gray-60">
                Fundo <strong>{fms.nome}</strong> ({fms.fundoId}).
              </p>
              <DataTable<SaldoBlocoFms>
                columns={COLUNAS}
                rows={fms.blocos}
                rowKey={(b) => String(b.bloco)}
                caption="Saldos do FMS por bloco de financiamento"
              />
              <Can permission="saude.gerenciar">
                <div className="d-flex gap-2 flex-wrap mt-3">
                  <Button variant="secondary" onClick={() => setMovimento('parcela')}>
                    <i className="fas fa-arrow-down" aria-hidden="true" /> Receber parcela (FNS)
                  </Button>
                  <Button variant="secondary" onClick={() => setMovimento('execucao')}>
                    <i className="fas fa-arrow-up" aria-hidden="true" /> Executar despesa
                  </Button>
                </div>
              </Can>
            </>
          )}
        </QueryState>
      )}

      <Alert variant="warning" title="Segregação por bloco">
        O recurso fundo a fundo chega carimbado por bloco; a transposição livre entre Custeio e
        Investimento é vedada (Port. GM/MS 3.992/2017). A taxonomia de blocos é parametrizável e
        deve ser confirmada no manual vigente.
      </Alert>

      <AbrirFmsModal
        open={abrirAberto}
        onClose={() => setAbrirAberto(false)}
        onCriado={(id) => setFundoInput(id)}
      />
      <MovimentoFmsModal
        open={movimento !== null}
        tipo={movimento ?? 'parcela'}
        fundoId={fundoId}
        onClose={() => setMovimento(null)}
      />
    </Card>
  );
}
