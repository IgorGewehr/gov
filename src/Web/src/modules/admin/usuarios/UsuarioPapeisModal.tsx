// Modais de ações dedicadas ao usuário: "Definir papéis" (PUT /usuarios/{id}/papeis)
// e "Redefinir senha" (PUT /usuarios/{id}/senha). Seguem o PADRÃO-OURO dos modais de
// ação do Protocolo (ProcessoAcaoModals): controlados, validação local, toast e
// invalidação via hooks da camada de API.
import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useDefinirPapeis, usePapeis, useRedefinirSenha } from './usuario.api';
import type { UsuarioResumo } from './usuario.api';
import { SENHA_MIN } from './usuario.helpers';

// ---------------------------------------------------------------------------
// Definir papéis
// ---------------------------------------------------------------------------

export interface UsuarioPapeisModalProps {
  open: boolean;
  onClose: () => void;
  usuario: UsuarioResumo | null;
}

export function UsuarioPapeisModal({ open, onClose, usuario }: UsuarioPapeisModalProps) {
  const toast = useToast();
  const papeisQuery = usePapeis(open);
  const mutation = useDefinirPapeis();
  const [selecionados, setSelecionados] = useState<string[]>([]);

  // Pré-seleciona os papéis atuais do usuário (casados por NOME, pois a lista
  // resumida expõe nomes; o PUT envia os IDs correspondentes do catálogo).
  useEffect(() => {
    if (!open || !usuario || !papeisQuery.data) return;
    const atuais = new Set(usuario.papeis);
    setSelecionados(papeisQuery.data.filter((p) => atuais.has(p.nome)).map((p) => p.id));
  }, [open, usuario, papeisQuery.data]);

  function alternar(id: string): void {
    setSelecionados((atual) =>
      atual.includes(id) ? atual.filter((p) => p !== id) : [...atual, id],
    );
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (!usuario) return;
    mutation.mutate(
      { id: usuario.id, input: { papeisIds: selecionados } },
      {
        onSuccess: () => {
          toast.success('Papéis atualizados.', 'Sucesso');
          onClose();
        },
        onError: (error) => {
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível definir os papéis.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={usuario ? `Definir papéis — ${usuario.nome}` : 'Definir papéis'}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-papeis" loading={mutation.isPending}>
            Salvar papéis
          </Button>
        </>
      }
    >
      <form id="form-papeis" className="br-form" onSubmit={submeter} noValidate>
        {papeisQuery.isLoading && <p>Carregando papéis…</p>}
        {papeisQuery.isError && (
          <Alert variant="warning">Não foi possível carregar os papéis.</Alert>
        )}
        {papeisQuery.data?.length === 0 && (
          <p className="text-down-01">Nenhum papel cadastrado.</p>
        )}
        <fieldset className="br-fieldset">
          <legend className="sr-only">Papéis do usuário</legend>
          {papeisQuery.data?.map((papel) => (
            <div className="br-checkbox" key={papel.id}>
              <input
                id={`def-papel-${papel.id}`}
                type="checkbox"
                checked={selecionados.includes(papel.id)}
                onChange={() => alternar(papel.id)}
              />
              <label htmlFor={`def-papel-${papel.id}`}>{papel.nome}</label>
            </div>
          ))}
        </fieldset>
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------
// Redefinir senha
// ---------------------------------------------------------------------------

export interface UsuarioSenhaModalProps {
  open: boolean;
  onClose: () => void;
  usuario: UsuarioResumo | null;
}

export function UsuarioSenhaModal({ open, onClose, usuario }: UsuarioSenhaModalProps) {
  const toast = useToast();
  const mutation = useRedefinirSenha();
  const [novaSenha, setNovaSenha] = useState('');
  const [erro, setErro] = useState<string | undefined>(undefined);

  useEffect(() => {
    if (!open) return;
    setNovaSenha('');
    setErro(undefined);
  }, [open]);

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (!usuario) return;
    if (novaSenha.length < SENHA_MIN) {
      setErro(`A senha deve ter no mínimo ${SENHA_MIN} caracteres.`);
      return;
    }
    setErro(undefined);
    mutation.mutate(
      { id: usuario.id, input: { novaSenha } },
      {
        onSuccess: () => {
          toast.success('Senha redefinida.', 'Sucesso');
          onClose();
        },
        onError: (error) => {
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível redefinir a senha.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={usuario ? `Redefinir senha — ${usuario.nome}` : 'Redefinir senha'}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-senha" loading={mutation.isPending}>
            Redefinir
          </Button>
        </>
      }
    >
      <form id="form-senha" className="br-form" onSubmit={submeter} noValidate>
        <FormField
          label="Nova senha"
          required
          error={erro}
          help={`Mínimo de ${SENHA_MIN} caracteres.`}
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="password"
              aria-describedby={describedBy}
              invalid={invalid}
              value={novaSenha}
              onChange={(e) => setNovaSenha(e.target.value)}
              autoComplete="new-password"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
