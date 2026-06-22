// Formulario de criacao de Comissao: nome, tipo e finalidade. Mutation +
// validacao por campo + Toast.
import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { TIPOS_COMISSAO } from './legislativo.shared';
import { tratarErroCampos, mensagemErro } from './legislativoAcao.shared';
import { useCriarComissao, type ComissaoInput } from './comissoes.api';

export interface ComissaoFormModalProps {
  open: boolean;
  onClose: () => void;
}

interface FormErrors {
  nome?: string;
}

const CAMPOS: Record<string, number> = { nome: 1 };

export function ComissaoFormModal({ open, onClose }: ComissaoFormModalProps) {
  const toast = useToast();
  const criar = useCriarComissao();

  const [nome, setNome] = useState('');
  const [tipo, setTipo] = useState<string>(String(TIPOS_COMISSAO[0].value));
  const [errors, setErrors] = useState<FormErrors>({});

  useEffect(() => {
    if (!open) return;
    setNome('');
    setTipo(String(TIPOS_COMISSAO[0].value));
    setErrors({});
  }, [open]);

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (nome.trim() === '') next.nome = 'Informe o nome da comissão.';
    return next;
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: ComissaoInput = { nome: nome.trim(), tipo: Number(tipo) };

    criar.mutate(input, {
      onSuccess: () => {
        toast.success('Comissão criada.', 'Sucesso');
        onClose();
      },
      onError: (error) => {
        setErrors(tratarErroCampos(error, CAMPOS));
        toast.error(mensagemErro(error, 'Não foi possível criar a comissão.'));
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Criar comissão"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={criar.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-comissao" loading={criar.isPending}>
            Criar
          </Button>
        </>
      }
    >
      <form id="form-comissao" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Nome" required error={errors.nome}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={nome}
              onChange={(e) => setNome(e.target.value)}
              placeholder="Ex.: Comissão de Constituição e Justiça"
            />
          )}
        </FormField>

        <FormField label="Tipo">
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={tipo}
              onChange={(e) => setTipo(e.target.value)}
              options={TIPOS_COMISSAO.map((t) => ({ value: String(t.value), label: t.label }))}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
