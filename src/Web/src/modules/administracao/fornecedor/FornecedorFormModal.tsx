// Formulario de CADASTRO de Fornecedor em Modal (CadastrarFornecedorCommand).
// Padrao-ouro: mutation + validacao por campo (FormField/aria-describedby) +
// Toast de sucesso/erro + mapeamento de ApiError.fieldErrors.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useCadastrarFornecedor } from './fornecedor.api';
import type { CadastrarFornecedorInput } from './fornecedor.api';

export interface FornecedorFormModalProps {
  open: boolean;
  onClose: () => void;
  /** Chamado com o id do novo fornecedor apos cadastro bem-sucedido. */
  onCadastrado?: (id: string) => void;
}

interface FormErrors {
  cnpj?: string;
  razaoSocial?: string;
}

const CAMPOS: Record<keyof FormErrors, true> = { cnpj: true, razaoSocial: true };

export function FornecedorFormModal({ open, onClose, onCadastrado }: FornecedorFormModalProps) {
  const toast = useToast();
  const mutation = useCadastrarFornecedor();

  const [cnpj, setCnpj] = useState('');
  const [razaoSocial, setRazaoSocial] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  function validar(): FormErrors {
    const next: FormErrors = {};
    const digitos = cnpj.replace(/\D/g, '');
    if (cnpj.trim() === '') next.cnpj = 'Informe o CNPJ.';
    else if (digitos.length !== 14) next.cnpj = 'O CNPJ deve ter 14 digitos.';
    if (razaoSocial.trim() === '') next.razaoSocial = 'Informe a razao social.';
    else if (razaoSocial.trim().length > 200) next.razaoSocial = 'Razao social: maximo 200 caracteres.';
    return next;
  }

  function fechar(): void {
    setCnpj('');
    setRazaoSocial('');
    setErrors({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: CadastrarFornecedorInput = {
      cnpj: cnpj.trim(),
      razaoSocial: razaoSocial.trim(),
    };

    mutation.mutate(input, {
      onSuccess: (resultado) => {
        toast.success('Fornecedor cadastrado com sucesso.', 'Sucesso');
        onCadastrado?.(resultado.id);
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const mapped: FormErrors = {};
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const key = (field.charAt(0).toLowerCase() + field.slice(1)) as keyof FormErrors;
            if (key in CAMPOS) mapped[key] = messages[0];
          }
          setErrors(mapped);
        }
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Nao foi possivel cadastrar o fornecedor.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Cadastrar fornecedor"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-cadastrar-fornecedor" loading={mutation.isPending}>
            Cadastrar
          </Button>
        </>
      }
    >
      <form id="form-cadastrar-fornecedor" className="br-form" onSubmit={submeter} noValidate>
        <FormField
          label="CNPJ"
          required
          error={errors.cnpj}
          help="Validado na Receita ao cadastrar. Com ou sem mascara."
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={cnpj}
              onChange={(e) => setCnpj(e.target.value)}
              placeholder="00.000.000/0000-00"
              inputMode="numeric"
              autoComplete="off"
            />
          )}
        </FormField>

        <FormField label="Razao social" required error={errors.razaoSocial}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={razaoSocial}
              onChange={(e) => setRazaoSocial(e.target.value)}
              maxLength={200}
              placeholder="Fornecedora X Ltda"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
