// Formulario de montagem (criacao) de uma edicao do Diario Oficial: o backend
// gera o numero sequencial; o operador informa apenas o ANO. Mutation +
// validacao por campo + Toast.
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
  ano?: string;
}

const CAMPOS: Record<string, number> = { ano: 1 };

export function EdicaoFormModal({ open, onClose }: EdicaoFormModalProps) {
  const toast = useToast();
  const criar = useCriarEdicao();

  const [ano, setAno] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  useEffect(() => {
    if (!open) return;
    setAno('');
    setErrors({});
  }, [open]);

  function validar(): FormErrors {
    const next: FormErrors = {};
    const valor = Number(ano);
    if (ano.trim() === '' || !Number.isInteger(valor) || valor < 1900 || valor > 2100)
      next.ano = 'Informe um ano válido (1900–2100).';
    return next;
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: EdicaoInput = { ano: Number(ano) };

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
        <FormField
          label="Ano da edição"
          required
          error={errors.ano}
          help="O número sequencial é gerado automaticamente pelo sistema."
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              type="number"
              value={ano}
              onChange={(e) => setAno(e.target.value)}
              placeholder="Ex.: 2026"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
