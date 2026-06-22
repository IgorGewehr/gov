// Modais de ACAO do agregado Sessao com corpo de formulario (Registrar presenca e
// Incluir proposicao na Ordem do Dia). As transicoes simples (abertura, suspensao,
// reabertura, encerramento, cancelamento, verificacao de quorum) usam
// ConfirmarAcaoModal diretamente na DetailPage.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { useRegistrarPresenca, useIncluirNaOrdemDoDia } from './sessao.api';
import { mensagemErro, tratarErroCampos } from './legislativoAcao.shared';
import type { AcaoModalBaseProps } from './legislativoAcao.shared';

// ---------------------------------------------------------------------------
// REGISTRAR PRESENCA
// ---------------------------------------------------------------------------

export function PresencaModal({ open, onClose, id }: AcaoModalBaseProps) {
  const toast = useToast();
  const mutation = useRegistrarPresenca(id);
  const [vereadorId, setVereadorId] = useState('');
  const [errors, setErrors] = useState<{ vereadorId?: string }>({});

  function fechar(): void {
    setErrors({});
    setVereadorId('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (vereadorId.trim() === '') {
      setErrors({ vereadorId: 'Informe o identificador do vereador.' });
      return;
    }
    setErrors({});
    mutation.mutate(
      { vereadorId: vereadorId.trim() },
      {
        onSuccess: () => {
          toast.success('Presença registrada.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          setErrors(tratarErroCampos(error, { vereadorId: 1 }));
          toast.error(mensagemErro(error, 'Não foi possível registrar a presença.'));
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar presença"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-presenca" loading={mutation.isPending}>
            Registrar
          </Button>
        </>
      }
    >
      <form id="form-presenca" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Vereador (identificador)" required error={errors.vereadorId}>
          {({ id: fid, describedBy, invalid }) => (
            <Input
              id={fid}
              aria-describedby={describedBy}
              invalid={invalid}
              value={vereadorId}
              onChange={(e) => setVereadorId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------
// INCLUIR NA ORDEM DO DIA
// ---------------------------------------------------------------------------

export function IncluirOrdemDoDiaModal({ open, onClose, id }: AcaoModalBaseProps) {
  const toast = useToast();
  const mutation = useIncluirNaOrdemDoDia(id);
  const [proposicaoId, setProposicaoId] = useState('');
  const [errors, setErrors] = useState<{ proposicaoId?: string }>({});

  function fechar(): void {
    setErrors({});
    setProposicaoId('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (proposicaoId.trim() === '') {
      setErrors({ proposicaoId: 'Informe o identificador da proposição.' });
      return;
    }
    setErrors({});
    mutation.mutate(
      { proposicaoId: proposicaoId.trim() },
      {
        onSuccess: () => {
          toast.success('Proposição incluída na Ordem do Dia.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          setErrors(tratarErroCampos(error, { proposicaoId: 1 }));
          toast.error(mensagemErro(error, 'Não foi possível incluir a proposição.'));
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Incluir na Ordem do Dia"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-ordem-dia" loading={mutation.isPending}>
            Incluir
          </Button>
        </>
      }
    >
      <form id="form-ordem-dia" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Proposição (identificador)" required error={errors.proposicaoId}>
          {({ id: fid, describedBy, invalid }) => (
            <Input
              id={fid}
              aria-describedby={describedBy}
              invalid={invalid}
              value={proposicaoId}
              onChange={(e) => setProposicaoId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
