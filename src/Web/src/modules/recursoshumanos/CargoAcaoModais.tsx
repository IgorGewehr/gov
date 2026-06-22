// Modais de AÇÃO (commands) do agregado Cargo, abertos a partir da CargoDetailPage.
// Cada um é wired a uma mutation TanStack Query, com Toast e mapeamento de erros.
// Provimento e vacância não têm payload (confirmação); vencimento e extinção têm form.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import {
  useAlterarVencimento,
  useExtinguirCargo,
  useProverCargo,
  useVagarCargo,
} from './api';

interface CargoModalBaseProps {
  open: boolean;
  onClose: () => void;
  cargoId: string;
}

// ---------------------------------------------------------------------------
// PROVIMENTO (ProverCargo) — ocupa uma vaga (sem payload)
// ---------------------------------------------------------------------------

export function ProverCargoModal({ open, onClose, cargoId }: CargoModalBaseProps) {
  const toast = useToast();
  const mutation = useProverCargo(cargoId);

  function confirmar(event: FormEvent): void {
    event.preventDefault();
    mutation.mutate(undefined, {
      onSuccess: () => {
        toast.success('Vaga provida.', 'Sucesso');
        onClose();
      },
      onError: (error) =>
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível prover o cargo.',
        ),
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Prover vaga do cargo"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-prover" loading={mutation.isPending}>
            Prover
          </Button>
        </>
      }
    >
      <form id="form-prover" className="br-form" onSubmit={confirmar} noValidate>
        <Alert variant="info" title="Provimento:">
          Ocupa uma das vagas autorizadas do cargo. Confirme para reduzir as vagas disponíveis.
        </Alert>
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------
// VACÂNCIA (VagarCargo) — libera uma vaga (sem payload)
// ---------------------------------------------------------------------------

export function VagarCargoModal({ open, onClose, cargoId }: CargoModalBaseProps) {
  const toast = useToast();
  const mutation = useVagarCargo(cargoId);

  function confirmar(event: FormEvent): void {
    event.preventDefault();
    mutation.mutate(undefined, {
      onSuccess: () => {
        toast.success('Vaga liberada.', 'Sucesso');
        onClose();
      },
      onError: (error) =>
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível vagar o cargo.',
        ),
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Vagar cargo"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-vagar" loading={mutation.isPending}>
            Vagar
          </Button>
        </>
      }
    >
      <form id="form-vagar" className="br-form" onSubmit={confirmar} noValidate>
        <Alert variant="info" title="Vacância:">
          Libera uma vaga do cargo (ex.: por desligamento de ocupante). Confirme para aumentar as
          vagas disponíveis.
        </Alert>
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------
// VENCIMENTO (AlterarVencimento) — novo vencimento-base
// ---------------------------------------------------------------------------

export function AlterarVencimentoModal({ open, onClose, cargoId }: CargoModalBaseProps) {
  const toast = useToast();
  const mutation = useAlterarVencimento(cargoId);
  const [novoVencimento, setNovoVencimento] = useState('');
  const [errors, setErrors] = useState<{ novoVencimento?: string }>({});

  function fechar(): void {
    setErrors({});
    setNovoVencimento('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const valor = Number(novoVencimento);
    if (novoVencimento.trim() === '' || Number.isNaN(valor) || valor <= 0) {
      setErrors({ novoVencimento: 'Informe um vencimento maior que zero.' });
      return;
    }
    setErrors({});
    mutation.mutate(
      { novoVencimento: valor },
      {
        onSuccess: () => {
          toast.success('Vencimento alterado.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          if (error instanceof ApiError && error.fieldErrors.NovoVencimento) {
            setErrors({ novoVencimento: error.fieldErrors.NovoVencimento[0] });
          }
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível alterar o vencimento.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Alterar vencimento-base"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-vencimento" loading={mutation.isPending}>
            Alterar
          </Button>
        </>
      }
    >
      <form id="form-vencimento" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Novo vencimento (R$)" required error={errors.novoVencimento}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="0"
              step="0.01"
              inputMode="decimal"
              aria-describedby={describedBy}
              invalid={invalid}
              value={novoVencimento}
              onChange={(e) => setNovoVencimento(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------
// EXTINÇÃO (ExtinguirCargo) — terminal, exige lei de extinção
// ---------------------------------------------------------------------------

export function ExtinguirCargoModal({ open, onClose, cargoId }: CargoModalBaseProps) {
  const toast = useToast();
  const mutation = useExtinguirCargo(cargoId);
  const [leiExtincao, setLeiExtincao] = useState('');
  const [errors, setErrors] = useState<{ leiExtincao?: string }>({});

  function fechar(): void {
    setErrors({});
    setLeiExtincao('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (leiExtincao.trim() === '') {
      setErrors({ leiExtincao: 'Informe a lei de extinção do cargo.' });
      return;
    }
    setErrors({});
    mutation.mutate(
      { leiExtincao: leiExtincao.trim() },
      {
        onSuccess: () => {
          toast.success('Cargo extinto.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          if (error instanceof ApiError && error.fieldErrors.LeiExtincao) {
            setErrors({ leiExtincao: error.fieldErrors.LeiExtincao[0] });
          }
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível extinguir o cargo.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Extinguir cargo"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="danger" type="submit" form="form-extincao" loading={mutation.isPending}>
            Extinguir
          </Button>
        </>
      }
    >
      <form id="form-extincao" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="danger" title="Ação terminal:">
          A extinção encerra o cargo (somente cargos sem ocupantes). Exige lei específica.
        </Alert>
        <FormField label="Lei de extinção" required error={errors.leiExtincao}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={leiExtincao}
              onChange={(e) => setLeiExtincao(e.target.value)}
              placeholder="Lei Municipal nº 0.000/2026"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
