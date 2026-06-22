// Formulário de registro de Atendimento (encontro assistencial — PEP/e-SUS APS) em Modal.
// Padrão-ouro: mutation + validação por campo + Toast e mapeamento de erros do backend.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useRegistrarAtendimento } from './api';
import type { RegistrarAtendimentoInput } from './api';
import { opcoesModalidade, paraModalidade } from './saude.helpers';

export interface AtendimentoFormModalProps {
  open: boolean;
  onClose: () => void;
  /** Paciente do prontuário em que o atendimento é aberto. */
  pacienteId: string;
}

interface FormErrors {
  estabelecimentoId?: string;
  profissionalId?: string;
  dataHora?: string;
}

const CAMPOS_VALIDOS: Record<keyof FormErrors, true> = {
  estabelecimentoId: true,
  profissionalId: true,
  dataHora: true,
};

export function AtendimentoFormModal({ open, onClose, pacienteId }: AtendimentoFormModalProps) {
  const toast = useToast();
  const mutation = useRegistrarAtendimento();

  const [estabelecimentoId, setEstabelecimentoId] = useState('');
  const [profissionalId, setProfissionalId] = useState('');
  const [dataHora, setDataHora] = useState('');
  const [modalidade, setModalidade] = useState('1');
  const [errors, setErrors] = useState<FormErrors>({});

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (estabelecimentoId.trim() === '') next.estabelecimentoId = 'Informe o estabelecimento (CNES).';
    if (profissionalId.trim() === '') next.profissionalId = 'Informe o profissional responsável.';
    if (dataHora.trim() === '') next.dataHora = 'Informe a data e hora do atendimento.';
    return next;
  }

  function fechar(): void {
    setErrors({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: RegistrarAtendimentoInput = {
      pacienteId,
      estabelecimentoId: estabelecimentoId.trim(),
      profissionalId: profissionalId.trim(),
      dataHora: new Date(dataHora).toISOString(),
      modalidade: paraModalidade(modalidade),
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Atendimento registrado com sucesso.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const mapped: FormErrors = {};
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const key = (field.charAt(0).toLowerCase() + field.slice(1)) as keyof FormErrors;
            if (key in CAMPOS_VALIDOS) mapped[key] = messages[0];
          }
          setErrors(mapped);
        }
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível registrar o atendimento.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar atendimento"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-registrar-atendimento" loading={mutation.isPending}>
            Registrar
          </Button>
        </>
      }
    >
      <form id="form-registrar-atendimento" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Estabelecimento (CNES)" required error={errors.estabelecimentoId}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={estabelecimentoId}
              onChange={(e) => setEstabelecimentoId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>

        <FormField label="Profissional responsável" required error={errors.profissionalId}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={profissionalId}
              onChange={(e) => setProfissionalId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>

        <div className="row">
          <div className="col-sm-7">
            <FormField label="Data e hora" required error={errors.dataHora}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="datetime-local"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={dataHora}
                  onChange={(e) => setDataHora(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-5">
            <FormField label="Modalidade" required>
              {({ id }) => (
                <Select
                  id={id}
                  options={opcoesModalidade}
                  value={modalidade}
                  onChange={(e) => setModalidade(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}
