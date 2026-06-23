// Detalhe/condução de uma INSPEÇÃO da VISA em modal: cabeçalho (situação/resultado/pendências),
// checklist de itens (conforme/não conforme) e ações (registrar item, concluir, lavrar auto,
// cancelar). Conduzir é gated em "saude.vigilancia.inspecionar"; lavrar auto em ".autuar".
import { useState } from 'react';
import { Button, DataTable, Modal, QueryState, Tag, Toolbar } from '../../components/ui';
import type { Column } from '../../components/ui';
import { useHasPermission } from '../../auth/Can';
import { formatarData } from '../../i18n/format';
import { useCancelarInspecao, useInspecao } from './vigilancia.api';
import type { ItemInspecaoDto } from './vigilancia.api';
import {
  conformidadeLabel,
  conformidadeVariant,
  resultadoLabel,
  resultadoVariant,
  situacaoInspecaoLabel,
  situacaoInspecaoVariant,
} from './vigilancia.helpers';
import { ConcluirInspecaoModal, RegistrarItemModal } from './VisaInspecaoModals';
import { LavrarAutoModal } from './VisaAutoModal';
import { VisaMotivoModal } from './VisaMotivoModal';

export function VisaInspecaoDetalheModal({
  open,
  onClose,
  inspecaoId,
}: {
  open: boolean;
  onClose: () => void;
  inspecaoId: string;
}) {
  const podeInspecionar = useHasPermission('saude.vigilancia.inspecionar');
  const podeAutuar = useHasPermission('saude.vigilancia.autuar');
  const query = useInspecao(inspecaoId, open);
  const cancelar = useCancelarInspecao(inspecaoId);

  const [itemAberto, setItemAberto] = useState(false);
  const [concluirAberto, setConcluirAberto] = useState(false);
  const [autoAberto, setAutoAberto] = useState(false);
  const [cancelarAberto, setCancelarAberto] = useState(false);

  const columns: Column<ItemInspecaoDto>[] = [
    { key: 'requisito', header: 'Requisito', render: (i) => i.requisito },
    {
      key: 'conformidade',
      header: 'Conformidade',
      render: (i) => <Tag variant={conformidadeVariant(i.conformidade)}>{conformidadeLabel[i.conformidade]}</Tag>,
    },
    { key: 'observacao', header: 'Observação', render: (i) => i.observacao || '—' },
  ];

  return (
    <Modal open={open} onClose={onClose} title="Inspeção sanitária">
      <QueryState
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
        empty={<span className="text-secondary">Inspeção não encontrada.</span>}
      >
        {(inspecao) => {
          const aberta = inspecao.situacao === 'Aberta';
          const concluida = inspecao.situacao === 'Concluida';
          return (
          <>
            <div className="d-flex flex-wrap align-items-center mb-3" style={{ gap: '0.75rem' }}>
              <Tag variant={situacaoInspecaoVariant(inspecao.situacao)}>
                {situacaoInspecaoLabel[inspecao.situacao]}
              </Tag>
              {inspecao.resultado && (
                <Tag variant={resultadoVariant(inspecao.resultado)}>{resultadoLabel[inspecao.resultado]}</Tag>
              )}
              <span className="text-secondary">Data: {formatarData(inspecao.dataInspecao)}</span>
              <span className="text-secondary">Pendências: {inspecao.pendencias}</span>
              {inspecao.roteiro && <span className="text-secondary">Roteiro: {inspecao.roteiro}</span>}
            </div>

            <Toolbar className="mb-3">
              {aberta && podeInspecionar && (
                <>
                  <Button variant="primary" className="small" onClick={() => setItemAberto(true)}>
                    <i className="fas fa-plus" aria-hidden="true" /> Registrar item
                  </Button>
                  <Button variant="secondary" className="small" onClick={() => setConcluirAberto(true)}>
                    Concluir inspeção
                  </Button>
                  <Button variant="secondary" className="small" onClick={() => setCancelarAberto(true)}>
                    Cancelar inspeção
                  </Button>
                </>
              )}
              {concluida && podeAutuar && (
                <Button variant="secondary" className="small" onClick={() => setAutoAberto(true)}>
                  Lavrar auto
                </Button>
              )}
            </Toolbar>

            <DataTable
              caption="Itens do roteiro de inspeção"
              columns={columns}
              rows={inspecao.itens}
              rowKey={(i) => `${i.requisito}-${i.conformidade}`}
              empty={<span className="text-secondary">Nenhum item registrado ainda.</span>}
            />

            <RegistrarItemModal open={itemAberto} onClose={() => setItemAberto(false)} inspecaoId={inspecaoId} />
            <ConcluirInspecaoModal
              open={concluirAberto}
              onClose={() => setConcluirAberto(false)}
              inspecaoId={inspecaoId}
            />
            <LavrarAutoModal
              open={autoAberto}
              onClose={() => setAutoAberto(false)}
              inspecaoId={inspecaoId}
              estabelecimentoId={inspecao.estabelecimentoId}
            />
            <VisaMotivoModal
              open={cancelarAberto}
              onClose={() => setCancelarAberto(false)}
              title="Cancelar inspeção"
              label="Motivo do cancelamento"
              acaoLabel="Cancelar inspeção"
              sucessoMensagem="Inspeção cancelada."
              pendente={cancelar.isPending}
              executar={(motivo) => cancelar.mutateAsync(motivo)}
            />
          </>
          );
        }}
      </QueryState>
    </Modal>
  );
}
