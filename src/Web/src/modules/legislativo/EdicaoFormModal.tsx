// Formulario de montagem (criacao) de uma edicao do Diario Oficial: numero
// sequencial + data de referencia. Mutation + validacao por campo + Toast.
import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { tratarErroCampos, mensagemErro } from './legislativoAcao.shared';
import { useCriarEdicao, type EdicaoInput } from './diario.api';

export interface EdicaoFormModalProps {
  open: boolean;
  onClose: () => void;
}

interface FormErrors {
  numero?: string;
  dataReferencia?: string;
}

const CAMPOS: Record<string, number> = { numero: 1, dataReferencia: 1 };

export function EdicaoFormModal({ open, onClose }: EdicaoFormModalProps) {
  const toast = useToast();
  const criar = useCriarEdicao();

  const [numero, setNumero] = useState('');
  const [dataReferencia, setDataReferencia] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  useEffect(() => {
    if (!open) return;
    setNumero('');
    setDataReferencia('');
    setErrors({});
  }, [open]);

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (numero.trim() === '' || Number.isNaN(Number(numero))) next.numero = 'Informe o número da edição.';
    if (dataReferencia === '') next.dataReferencia = 'Informe a data de referência.';
    return next;
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: EdicaoInput = { numero: Number(numero), dataReferencia };

    criar.mutate(input, {
      onSuccess: () => {
        toast.success('Edição criada.', 'Sucesso');
        onClose();
      },
      onError: (error) => {
        setErrors(tratarErroCampos(error, CAMPOS));
        toast.error(mensagemErro(error, 'Não foi possível criar a edição.'));
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Montar edição do Diário"
      size="small"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={criar.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-edicao" loading={criar.isPending}>
            Criar edição
          </Button>
        </>
      }
    >
      <form id="form-edicao" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Número da edição" required error={errors.numero}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              type="number"
              value={numero}
              onChange={(e) => setNumero(e.target.value)}
              placeholder="Ex.: 142"
            />
          )}
        </FormField>

        <FormField label="Data de referência" required error={errors.dataReferencia}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              type="date"
              value={dataReferencia}
              onChange={(e) => setDataReferencia(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
