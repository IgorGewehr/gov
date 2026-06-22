// Formulario de inclusao de um membro (vereador) na composicao de uma comissao:
// vereador + cargo. Mutation + validacao + Toast.
import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Modal, Select, useToast } from '../../components/ui';
import { CARGOS_COMISSAO, PAPEIS_MEMBRO } from './legislativo.shared';
import { tratarErroCampos, mensagemErro } from './legislativoAcao.shared';
import { useVereadores } from './vereadores.api';
import { useAdicionarMembro, type MembroInput } from './comissoes.api';

export interface MembroComissaoModalProps {
  open: boolean;
  onClose: () => void;
  /** Identificador da comissao. */
  comissaoId: string;
}

interface FormErrors {
  vereadorId?: string;
}

const CAMPOS: Record<string, number> = { vereadorId: 1 };

export function MembroComissaoModal({ open, onClose, comissaoId }: MembroComissaoModalProps) {
  const toast = useToast();
  const vereadores = useVereadores();
  const adicionar = useAdicionarMembro(comissaoId);

  const [vereadorId, setVereadorId] = useState('');
  const [papel, setPapel] = useState<string>(String(PAPEIS_MEMBRO[0].value));
  const [cargo, setCargo] = useState<string>(String(CARGOS_COMISSAO[0].value));
  const [errors, setErrors] = useState<FormErrors>({});

  useEffect(() => {
    if (!open) return;
    setVereadorId('');
    setPapel(String(PAPEIS_MEMBRO[0].value));
    setCargo(String(CARGOS_COMISSAO[0].value));
    setErrors({});
  }, [open]);

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (vereadorId === '') next.vereadorId = 'Selecione o vereador.';
    return next;
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: MembroInput = { vereadorId, papel: Number(papel), cargo: Number(cargo) };

    adicionar.mutate(input, {
      onSuccess: () => {
        toast.success('Membro adicionado.', 'Sucesso');
        onClose();
      },
      onError: (error) => {
        setErrors(tratarErroCampos(error, CAMPOS));
        toast.error(mensagemErro(error, 'Não foi possível adicionar o membro.'));
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Adicionar membro"
      size="small"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={adicionar.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-membro" loading={adicionar.isPending}>
            Adicionar
          </Button>
        </>
      }
    >
      <form id="form-membro" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Vereador" required error={errors.vereadorId}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={vereadorId}
              onChange={(e) => setVereadorId(e.target.value)}
              placeholder="Selecione o vereador"
              options={(vereadores.data ?? []).map((v) => ({
                value: v.id,
                label: `${v.nomeParlamentar} (${v.partido})`,
              }))}
            />
          )}
        </FormField>

        <FormField label="Papel">
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={papel}
              onChange={(e) => setPapel(e.target.value)}
              options={PAPEIS_MEMBRO.map((p) => ({ value: String(p.value), label: p.label }))}
            />
          )}
        </FormField>

        <FormField label="Cargo">
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={cargo}
              onChange={(e) => setCargo(e.target.value)}
              options={CARGOS_COMISSAO.map((c) => ({ value: String(c.value), label: c.label }))}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
