// Modais de AÇÃO do agregado Escola, abertos a partir da EscolaDetailPage:
//   - Atualizar dados do EducaCenso (endereço/infraestrutura) -> PUT dados-censo
//   - Desativar escola (estado terminal, destrutivo)          -> POST desativacao
// Cada um é WIRED a uma mutation TanStack Query, com mapeamento de
// ProblemDetails.fieldErrors e Toast de sucesso/erro. A desativação exige confirmação.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, Modal, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useAtualizarDadosCenso, useDesativarEscola } from './api';
import type { AtualizarDadosCensoInput } from './api';
import {
  EscolaCamposCenso,
  censoVazio,
  montarCenso,
  validarCenso,
} from './EscolaCamposCenso';
import type { CamposCensoErrors, CamposCensoState } from './EscolaCamposCenso';

interface AcaoBaseProps {
  open: boolean;
  onClose: () => void;
  escolaId: string;
  /** Código INEP usado para invalidar o detalhe após a ação. */
  codigoInep: string;
}

// ---------------------------------------------------------------------------
// ATUALIZAR DADOS DO CENSO (PUT /escolas/{id}/dados-censo)
// ---------------------------------------------------------------------------

export function AtualizarDadosCensoModal({ open, onClose, escolaId, codigoInep }: AcaoBaseProps) {
  const toast = useToast();
  const mutation = useAtualizarDadosCenso(codigoInep);
  const [censo, setCenso] = useState<CamposCensoState>(censoVazio());
  const [errors, setErrors] = useState<CamposCensoErrors>({});

  function alterar(parcial: Partial<CamposCensoState>): void {
    setCenso((atual) => ({ ...atual, ...parcial }));
  }

  function fechar(): void {
    setErrors({});
    setCenso(censoVazio());
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validarCenso(censo);
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: AtualizarDadosCensoInput = montarCenso(censo);
    mutation.mutate(
      { escolaId, input },
      {
        onSuccess: () => {
          toast.success('Dados do Censo atualizados.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
            const mapped: CamposCensoErrors = {};
            for (const [field, messages] of Object.entries(error.fieldErrors)) {
              const key = field.charAt(0).toLowerCase() + field.slice(1);
              (mapped as Record<string, string>)[key] = messages[0];
            }
            setErrors(mapped);
          }
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível atualizar os dados.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Atualizar dados do EducaCenso"
      size="large"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-dados-censo" loading={mutation.isPending}>
            Salvar
          </Button>
        </>
      }
    >
      <form id="form-dados-censo" className="br-form" onSubmit={submeter} noValidate>
        <EscolaCamposCenso estado={censo} errors={errors} onChange={alterar} idSufixo="censo" />
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------
// DESATIVAR ESCOLA (POST /escolas/{id}/desativacao) — terminal/destrutivo
// ---------------------------------------------------------------------------

export function DesativarEscolaModal({ open, onClose, escolaId, codigoInep }: AcaoBaseProps) {
  const toast = useToast();
  const mutation = useDesativarEscola(codigoInep);

  function confirmar(event: FormEvent): void {
    event.preventDefault();
    mutation.mutate(escolaId, {
      onSuccess: () => {
        toast.success('Escola desativada.', 'Sucesso');
        onClose();
      },
      onError: (error) => {
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível desativar a escola.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Desativar escola"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="danger" type="submit" form="form-desativar-escola" loading={mutation.isPending}>
            Desativar definitivamente
          </Button>
        </>
      }
    >
      <form id="form-desativar-escola" className="br-form" onSubmit={confirmar} noValidate>
        <Alert variant="danger" title="Ação irreversível:">
          A desativação é um estado terminal. A escola deixa de operar na rede de ensino e não admite
          novas matrículas. Confirme para prosseguir.
        </Alert>
      </form>
    </Modal>
  );
}
