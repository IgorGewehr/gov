// Formulário de CRIAÇÃO/EDIÇÃO de Unidade Organizacional em Modal (foco preso).
//   - Criação raiz:  { codigo, nome, tipo }            (sem pai)
//   - Criação filha: { codigo, nome, tipo, unidadePaiId } (pai pré-fixado)
//   - Edição:        { nome, tipo }                     (renomear/redefinir tipo)
// Espelha o PADRÃO-OURO de UsuarioFormModal: validação local + mapeamento de
// fieldErrors do ProblemDetails, toast de sucesso/erro e invalidação via hook.
import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useCriarUnidade, useRenomearUnidade } from './unidades.api';
import type { CriarUnidadeInput, NoUnidade, TipoUnidade } from './unidades.api';
import { TIPOS_UNIDADE } from './unidades.helpers';

export interface UnidadeFormModalProps {
  open: boolean;
  onClose: () => void;
  /** Quando informada, o modal entra em modo EDIÇÃO (renomear) desta UO. */
  unidade?: NoUnidade | null;
  /** Quando informada (e sem `unidade`), cria uma UO FILHA desta. */
  pai?: NoUnidade | null;
}

interface FormErrors {
  codigo?: string;
  nome?: string;
  tipo?: string;
}

const TIPO_PADRAO: TipoUnidade = 'Secretaria';

export function UnidadeFormModal({ open, onClose, unidade, pai }: UnidadeFormModalProps) {
  const toast = useToast();
  const edicao = Boolean(unidade);
  const criar = useCriarUnidade();
  const renomear = useRenomearUnidade();
  const mutationPending = criar.isPending || renomear.isPending;

  const [codigo, setCodigo] = useState('');
  const [nome, setNome] = useState('');
  const [tipo, setTipo] = useState<TipoUnidade>(TIPO_PADRAO);
  const [errors, setErrors] = useState<FormErrors>({});

  // Hidrata os campos ao abrir/alternar entre criação e edição.
  useEffect(() => {
    if (!open) return;
    setCodigo('');
    setNome(unidade?.nome ?? '');
    setTipo(unidade?.tipo ?? TIPO_PADRAO);
    setErrors({});
  }, [open, unidade]);

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (!edicao && codigo.trim() === '') next.codigo = 'Informe o código da unidade.';
    if (nome.trim() === '') next.nome = 'Informe o nome da unidade.';
    return next;
  }

  function fechar(): void {
    setErrors({});
    onClose();
  }

  function mapearFieldErrors(error: ApiError): void {
    const mapped: FormErrors = {};
    const conhecidos = new Set(['codigo', 'nome', 'tipo']);
    for (const [field, messages] of Object.entries(error.fieldErrors)) {
      const key = field.charAt(0).toLowerCase() + field.slice(1);
      if (conhecidos.has(key)) (mapped as Record<string, string>)[key] = messages[0];
    }
    if (Object.keys(mapped).length > 0) setErrors(mapped);
  }

  function tratarErro(error: unknown, fallback: string): void {
    if (error instanceof ApiError) mapearFieldErrors(error);
    toast.error(error instanceof ApiError ? error.userMessage : fallback);
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    if (edicao && unidade) {
      renomear.mutate(
        { id: unidade.id, input: { nome: nome.trim(), tipo } },
        {
          onSuccess: () => {
            toast.success('Unidade atualizada.', 'Sucesso');
            fechar();
          },
          onError: (error) => tratarErro(error, 'Não foi possível atualizar a unidade.'),
        },
      );
      return;
    }

    const input: CriarUnidadeInput = {
      codigo: codigo.trim(),
      nome: nome.trim(),
      tipo,
      ...(pai ? { unidadePaiId: pai.id } : {}),
    };
    criar.mutate(input, {
      onSuccess: () => {
        toast.success('Unidade criada.', 'Sucesso');
        fechar();
      },
      onError: (error) => tratarErro(error, 'Não foi possível criar a unidade.'),
    });
  }

  const titulo = edicao
    ? 'Editar unidade'
    : pai
      ? `Nova subunidade de ${pai.nome}`
      : 'Nova unidade (raiz)';

  return (
    <Modal
      open={open}
      onClose={fechar}
      title={titulo}
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutationPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-unidade" loading={mutationPending}>
            {edicao ? 'Salvar' : 'Criar'}
          </Button>
        </>
      }
    >
      <form id="form-unidade" className="br-form" onSubmit={submeter} noValidate>
        {!edicao && (
          <FormField
            label="Código"
            required
            error={errors.codigo}
            help="Identificador estável da unidade (ex.: SEMSA, GAB)."
          >
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                aria-describedby={describedBy}
                invalid={invalid}
                value={codigo}
                onChange={(e) => setCodigo(e.target.value)}
                placeholder="Código da unidade"
              />
            )}
          </FormField>
        )}

        <FormField label="Nome" required error={errors.nome}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={nome}
              onChange={(e) => setNome(e.target.value)}
              placeholder="Nome da unidade organizacional"
            />
          )}
        </FormField>

        <FormField label="Tipo" required error={errors.tipo}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={tipo}
              onChange={(e) => setTipo(e.target.value as TipoUnidade)}
              options={TIPOS_UNIDADE}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
