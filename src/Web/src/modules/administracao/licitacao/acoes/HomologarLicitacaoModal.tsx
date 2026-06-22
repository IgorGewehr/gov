// HomologarLicitacao (EmJulgamento -> Homologada) — ato do ordenador (I-8).
import { Alert, Button, Modal, useToast } from '../../../../components/ui';
import { ApiError } from '../../../../api/problemDetails';
import { formatarMoeda } from '../../../../i18n/format';
import { useHomologarLicitacao } from '../licitacao.api';
import type { LicitacaoDetalhe } from '../licitacao.api';
import type { AcaoModalProps } from './licitacaoModais.shared';

export function HomologarLicitacaoModal({
  open,
  onClose,
  licitacaoId,
  licitacao,
}: AcaoModalProps & { licitacao: LicitacaoDetalhe }) {
  const toast = useToast();
  const mutation = useHomologarLicitacao(licitacaoId);
  const vencedora = licitacao.propostas.find((p) => p.propostaId === licitacao.propostaVencedoraId);

  function confirmar(): void {
    mutation.mutate(undefined, {
      onSuccess: () => {
        toast.success('Licitação homologada.', 'Sucesso');
        onClose();
      },
      onError: (error) => {
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível homologar a licitação.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Homologar licitação"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" onClick={confirmar} loading={mutation.isPending} disabled={!licitacao.propostaVencedoraId}>
            Homologar
          </Button>
        </>
      }
    >
      <p>
        A homologação é ato privativo do <strong>ordenador de despesa</strong> e valida o resultado do certame,
        autorizando a formalização do contrato.
      </p>
      {licitacao.propostaVencedoraId ? (
        <Alert variant="info">
          Proposta vencedora: {vencedora ? formatarMoeda(vencedora.valor) : licitacao.propostaVencedoraId}.
        </Alert>
      ) : (
        <Alert variant="warning">
          Não há proposta vencedora indicada. Julgue as propostas antes de homologar (I-8).
        </Alert>
      )}
    </Modal>
  );
}
