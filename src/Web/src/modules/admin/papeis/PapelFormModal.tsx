// Formulário de CRIAÇÃO de papel (command CriarPapel) em Modal (foco preso).
// Captura o nome do papel e o conjunto inicial de permissões selecionadas a
// partir do catálogo canônico (GET /api/identidade/permissoes), agrupado por módulo.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useCriarPapel } from './papel.api';
import type { CriarPapelInput } from './papel.api';
import { PermissoesCheckboxGroup } from './PermissoesCheckboxGroup';

const NOME_MAX = 80;

export interface PapelFormModalProps {
  open: boolean;
  onClose: () => void;
}

interface FormErrors {
  nome?: string;
}

export function PapelFormModal({ open, onClose }: PapelFormModalProps) {
  const toast = useToast();
  const mutation = useCriarPapel();

  const [nome, setNome] = useState('');
  const [selecionadas, setSelecionadas] = useState<Set<string>>(new Set());
  const [errors, setErrors] = useState<FormErrors>({});

  function alternar(chave: string): void {
    setSelecionadas((atual) => {
      const proximo = new Set(atual);
      if (proximo.has(chave)) proximo.delete(chave);
      else proximo.add(chave);
      return proximo;
    });
  }

  function alternarGrupo(chaves: string[], marcar: boolean): void {
    setSelecionadas((atual) => {
      const proximo = new Set(atual);
      for (const chave of chaves) {
        if (marcar) proximo.add(chave);
        else proximo.delete(chave);
      }
      return proximo;
    });
  }

  function validar(): FormErrors {
    const next: FormErrors = {};
    const nomeLimpo = nome.trim();
    if (nomeLimpo === '') next.nome = 'Informe o nome do papel.';
    else if (nomeLimpo.length > NOME_MAX)
      next.nome = `O nome deve ter no máximo ${NOME_MAX} caracteres.`;
    return next;
  }

  function fechar(): void {
    setNome('');
    setSelecionadas(new Set());
    setErrors({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: CriarPapelInput = {
      nome: nome.trim(),
      permissoes: Array.from(selecionadas),
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success(`Papel "${input.nome}" criado.`, 'Sucesso');
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError) {
          const fieldErrors = error.fieldErrors;
          const nomeErro = fieldErrors.Nome?.[0] ?? fieldErrors.nome?.[0];
          if (nomeErro) setErrors({ nome: nomeErro });
          toast.error(error.userMessage);
        } else {
          toast.error('Não foi possível criar o papel.');
        }
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Criar papel"
      size="large"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-criar-papel" loading={mutation.isPending}>
            Criar papel
          </Button>
        </>
      }
    >
      <form id="form-criar-papel" className="br-form" onSubmit={submeter} noValidate>
        <FormField
          label="Nome do papel"
          required
          error={errors.nome}
          help={`Identifica o conjunto de permissões (RBAC). Máx. ${NOME_MAX} caracteres.`}
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={NOME_MAX}
              value={nome}
              onChange={(e) => setNome(e.target.value)}
              placeholder="Ex.: Fiscal de Tributos"
            />
          )}
        </FormField>

        <p className="text-weight-semi-bold mb-2">Permissões iniciais</p>
        <PermissoesCheckboxGroup
          selecionadas={selecionadas}
          onToggle={alternar}
          onToggleGrupo={alternarGrupo}
          enabled={open}
        />
      </form>
    </Modal>
  );
}
