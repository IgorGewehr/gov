// JulgarPropostas (Aberta -> EmJulgamento) — indica a proposta vencedora (I-6).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Modal, Select, useToast } from '../../../../components/ui';
import type { SelectOption } from '../../../../components/ui';
import { ApiError } from '../../../../api/problemDetails';
import { formatarMoeda } from '../../../../i18n/format';
import { useJulgarPropostas } from '../licitacao.api';
import type { PropostaResumo } from '../licitacao.api';
import { SITUACAO_PROPOSTA_LABEL } from '../licitacao.helpers';
import { primeiraMensagem } from './licitacaoModais.shared';
import type { AcaoModalProps } from './licitacaoModais.shared';

export function JulgarPropostasModal({
  open,
  onClose,
  licitacaoId,
  propostas,
}: AcaoModalProps & { propostas: PropostaResumo[] }) {
  const toast = useToast();
  const mutation = useJulgarPropostas(licitacaoId);
  const [propostaVencedoraId, setPropostaVencedoraId] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  const options: SelectOption[] = propostas.map((p) => ({
    value: p.propostaId,
    label: `Fornecedor ${p.fornecedorId.slice(0, 8)}… — ${formatarMoeda(p.valor)} (${SITUACAO_PROPOSTA_LABEL[p.situacao]})`,
  }));

  function fechar(): void {
    setPropostaVencedoraId('');
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (propostaVencedoraId === '') {
      setErro('Selecione a proposta vencedora.');
      return;
    }
    setErro(undefined);
    mutation.mutate(
      { propostaVencedoraId },
      {
        onSuccess: () => {
          toast.success('Propostas julgadas. Vencedora indicada.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          if (error instanceof ApiError) setErro(primeiraMensagem(error, 'propostaVencedoraId'));
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível julgar as propostas.');
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Julgar propostas"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-julgar" loading={mutation.isPending}>
            Julgar
          </Button>
        </>
      }
    >
      <form id="form-julgar" className="br-form" onSubmit={submeter} noValidate>
        {propostas.length === 0 ? (
          <Alert variant="warning">
            Não há propostas recebidas para julgar. Cadastre/classifique propostas antes do julgamento.
          </Alert>
        ) : (
          <FormField label="Proposta vencedora" required error={erro}>
            {({ id, describedBy, invalid }) => (
              <Select
                id={id}
                aria-describedby={describedBy}
                invalid={invalid}
                placeholder="Selecione a proposta vencedora…"
                options={options}
                value={propostaVencedoraId}
                onChange={(e) => setPropostaVencedoraId(e.target.value)}
              />
            )}
          </FormField>
        )}
      </form>
    </Modal>
  );
}
