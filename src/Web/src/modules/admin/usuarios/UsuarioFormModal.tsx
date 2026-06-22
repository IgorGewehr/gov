// Formulário de CRIAÇÃO/EDIÇÃO de usuário em Modal (foco preso).
//   - Criação: { nome, email, senha, papeisIds[] } (papéis de GET /identidade/papeis);
//   - Edição:  { nome, email } (papéis e senha têm modais dedicados).
// Espelha o PADRÃO-OURO de ProcessoFormModal: validação local + mapeamento de
// fieldErrors do ProblemDetails, toast de sucesso/erro e invalidação via hook.
import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import {
  useAtualizarUsuario,
  useCriarUsuario,
  usePapeis,
} from './usuario.api';
import type {
  AtualizarUsuarioInput,
  CriarUsuarioInput,
  UsuarioResumo,
} from './usuario.api';
import { SENHA_MIN, emailValido } from './usuario.helpers';

export interface UsuarioFormModalProps {
  open: boolean;
  onClose: () => void;
  /** Quando informado, o modal entra em modo EDIÇÃO desse usuário. */
  usuario?: UsuarioResumo | null;
}

interface FormErrors {
  nome?: string;
  email?: string;
  senha?: string;
  papeisIds?: string;
}

export function UsuarioFormModal({ open, onClose, usuario }: UsuarioFormModalProps) {
  const toast = useToast();
  const edicao = Boolean(usuario);
  const criar = useCriarUsuario();
  const atualizar = useAtualizarUsuario();
  const mutationPending = criar.isPending || atualizar.isPending;

  const papeisQuery = usePapeis(open && !edicao);

  const [nome, setNome] = useState('');
  const [email, setEmail] = useState('');
  const [senha, setSenha] = useState('');
  const [papeisIds, setPapeisIds] = useState<string[]>([]);
  const [errors, setErrors] = useState<FormErrors>({});

  // Hidrata os campos ao abrir/alternar entre criação e edição.
  useEffect(() => {
    if (!open) return;
    setNome(usuario?.nome ?? '');
    setEmail(usuario?.email ?? '');
    setSenha('');
    setPapeisIds([]);
    setErrors({});
  }, [open, usuario]);

  function alternarPapel(id: string): void {
    setPapeisIds((atual) =>
      atual.includes(id) ? atual.filter((p) => p !== id) : [...atual, id],
    );
  }

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (nome.trim() === '') next.nome = 'Informe o nome do usuário.';
    if (email.trim() === '') next.email = 'Informe o e-mail.';
    else if (!emailValido(email)) next.email = 'Informe um e-mail válido.';
    if (!edicao && senha.length < SENHA_MIN)
      next.senha = `A senha deve ter no mínimo ${SENHA_MIN} caracteres.`;
    return next;
  }

  function fechar(): void {
    setErrors({});
    onClose();
  }

  function mapearFieldErrors(error: ApiError): void {
    const mapped: FormErrors = {};
    const conhecidos = new Set(['nome', 'email', 'senha', 'papeisIds']);
    for (const [field, messages] of Object.entries(error.fieldErrors)) {
      const key = field.charAt(0).toLowerCase() + field.slice(1);
      if (conhecidos.has(key)) (mapped as Record<string, string>)[key] = messages[0];
    }
    if (Object.keys(mapped).length > 0) setErrors(mapped);
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    if (edicao && usuario) {
      const input: AtualizarUsuarioInput = { nome: nome.trim(), email: email.trim() };
      atualizar.mutate(
        { id: usuario.id, input },
        {
          onSuccess: () => {
            toast.success('Usuário atualizado.', 'Sucesso');
            fechar();
          },
          onError: (error) => {
            if (error instanceof ApiError) mapearFieldErrors(error);
            toast.error(
              error instanceof ApiError ? error.userMessage : 'Não foi possível atualizar o usuário.',
            );
          },
        },
      );
      return;
    }

    const input: CriarUsuarioInput = {
      nome: nome.trim(),
      email: email.trim(),
      senha,
      papeisIds,
    };
    criar.mutate(input, {
      onSuccess: () => {
        toast.success('Usuário criado.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError) mapearFieldErrors(error);
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível criar o usuário.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title={edicao ? 'Editar usuário' : 'Novo usuário'}
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutationPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-usuario" loading={mutationPending}>
            {edicao ? 'Salvar' : 'Criar'}
          </Button>
        </>
      }
    >
      <form id="form-usuario" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Nome" required error={errors.nome}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={nome}
              onChange={(e) => setNome(e.target.value)}
              placeholder="Nome completo do servidor"
            />
          )}
        </FormField>

        <FormField label="E-mail" required error={errors.email}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="email"
              aria-describedby={describedBy}
              invalid={invalid}
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="servidor@orgao.gov.br"
            />
          )}
        </FormField>

        {!edicao && (
          <>
            <FormField
              label="Senha"
              required
              error={errors.senha}
              help={`Mínimo de ${SENHA_MIN} caracteres.`}
            >
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="password"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={senha}
                  onChange={(e) => setSenha(e.target.value)}
                  autoComplete="new-password"
                />
              )}
            </FormField>

            <fieldset className="br-fieldset mt-3">
              <legend className="text-weight-semi-bold">Papéis</legend>
              {papeisQuery.isLoading && <p>Carregando papéis…</p>}
              {papeisQuery.isError && (
                <Alert variant="warning">Não foi possível carregar os papéis.</Alert>
              )}
              {papeisQuery.data?.length === 0 && (
                <p className="text-down-01">Nenhum papel cadastrado.</p>
              )}
              {papeisQuery.data?.map((papel) => (
                <div className="br-checkbox" key={papel.id}>
                  <input
                    id={`papel-${papel.id}`}
                    type="checkbox"
                    checked={papeisIds.includes(papel.id)}
                    onChange={() => alternarPapel(papel.id)}
                  />
                  <label htmlFor={`papel-${papel.id}`}>{papel.nome}</label>
                </div>
              ))}
            </fieldset>
          </>
        )}
      </form>
    </Modal>
  );
}
