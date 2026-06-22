// Modais de TRANSIÇÃO/INTEGRAÇÃO do Atendimento: assinatura ICP-Brasil (NGS2),
// compartilhamento na RNDS, lançamento no SISAB e cancelamento (terminal).
// WIRED a mutations TanStack Query + Toast. Ações gated por "saude.gerenciar".
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, Textarea, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import {
  useAssinarAtendimento,
  useCancelarAtendimento,
  useCompartilharNaRnds,
  useLancarNoSisab,
} from './api';

interface AcaoAtendimentoProps {
  open: boolean;
  onClose: () => void;
  atendimentoId: string;
}

const MOTIVO_MAX = 500;

// --- ASSINAR ATENDIMENTO (ICP-Brasil / NGS2) ----------------------------------

export function AssinarAtendimentoModal({ open, onClose, atendimentoId }: AcaoAtendimentoProps) {
  const toast = useToast();
  const mutation = useAssinarAtendimento(atendimentoId);
  const [certificado, setCertificado] = useState('');
  const [hash, setHash] = useState('');
  const [errors, setErrors] = useState<{ certificadoIcpBrasil?: string; hash?: string }>({});

  function fechar(): void {
    setErrors({});
    setCertificado('');
    setHash('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: { certificadoIcpBrasil?: string; hash?: string } = {};
    if (certificado.trim() === '') next.certificadoIcpBrasil = 'Informe o certificado ICP-Brasil.';
    if (hash.trim() === '') next.hash = 'Informe o hash da assinatura.';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    mutation.mutate(
      { certificadoIcpBrasil: certificado.trim(), hash: hash.trim() },
      {
        onSuccess: () => {
          toast.success('Atendimento assinado (ICP-Brasil).', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível assinar o atendimento.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Assinar atendimento"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-assinar" loading={mutation.isPending}>
            Assinar
          </Button>
        </>
      }
    >
      <form id="form-assinar" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="warning" title="Assinatura imutável:">
          A assinatura em ICP-Brasil (NGS2) torna o atendimento imutável. Alterações posteriores
          somente por adendo assinado.
        </Alert>
        <FormField label="Certificado ICP-Brasil" required error={errors.certificadoIcpBrasil}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} aria-describedby={describedBy} invalid={invalid} value={certificado} onChange={(e) => setCertificado(e.target.value)} />
          )}
        </FormField>
        <FormField label="Hash da assinatura" required error={errors.hash}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} aria-describedby={describedBy} invalid={invalid} value={hash} onChange={(e) => setHash(e.target.value)} />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

// --- COMPARTILHAR NA RNDS (confirmação) ---------------------------------------

export function CompartilharRndsModal({ open, onClose, atendimentoId }: AcaoAtendimentoProps) {
  const toast = useToast();
  const mutation = useCompartilharNaRnds(atendimentoId);

  function confirmar(event: FormEvent): void {
    event.preventDefault();
    mutation.mutate(undefined, {
      onSuccess: () => {
        toast.success('RES compartilhado na RNDS.', 'Sucesso');
        onClose();
      },
      onError: (error) => {
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível compartilhar na RNDS.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Compartilhar na RNDS"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-rnds" loading={mutation.isPending}>
            Compartilhar
          </Button>
        </>
      }
    >
      <form id="form-rnds" className="br-form" onSubmit={confirmar} noValidate>
        <Alert variant="info" title="RNDS:">
          Envia o Registro Eletrônico de Saúde (RES) deste atendimento para a Rede Nacional de
          Dados em Saúde. Requer atendimento assinado.
        </Alert>
      </form>
    </Modal>
  );
}

// --- LANÇAR NO SISAB (confirmação) --------------------------------------------

export function LancarSisabModal({ open, onClose, atendimentoId }: AcaoAtendimentoProps) {
  const toast = useToast();
  const mutation = useLancarNoSisab(atendimentoId);

  function confirmar(event: FormEvent): void {
    event.preventDefault();
    mutation.mutate(undefined, {
      onSuccess: () => {
        toast.success('Atendimento lançado no SISAB.', 'Sucesso');
        onClose();
      },
      onError: (error) => {
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível lançar no SISAB.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Lançar no SISAB"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-sisab" loading={mutation.isPending}>
            Lançar
          </Button>
        </>
      }
    >
      <form id="form-sisab" className="br-form" onSubmit={confirmar} noValidate>
        <Alert variant="info" title="SISAB / e-SUS APS:">
          Registra a produção deste atendimento no Sistema de Informação em Saúde para a Atenção
          Básica.
        </Alert>
      </form>
    </Modal>
  );
}

// --- CANCELAR ATENDIMENTO (terminal) ------------------------------------------

export function CancelarAtendimentoModal({ open, onClose, atendimentoId }: AcaoAtendimentoProps) {
  const toast = useToast();
  const mutation = useCancelarAtendimento(atendimentoId);
  const [motivo, setMotivo] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setErro(undefined);
    setMotivo('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const valor = motivo.trim();
    if (valor === '') {
      setErro('Informe o motivo do cancelamento.');
      return;
    }
    if (valor.length > MOTIVO_MAX) {
      setErro(`O motivo deve ter no máximo ${MOTIVO_MAX} caracteres.`);
      return;
    }
    setErro(undefined);
    mutation.mutate(
      { motivo: valor },
      {
        onSuccess: () => {
          toast.success('Atendimento cancelado.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível cancelar o atendimento.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Cancelar atendimento"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Voltar
          </Button>
          <Button variant="danger" type="submit" form="form-cancelar-atendimento" loading={mutation.isPending}>
            Cancelar atendimento
          </Button>
        </>
      }
    >
      <form id="form-cancelar-atendimento" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="danger" title="Ação terminal:">
          O cancelamento só é possível antes da assinatura e é irreversível. Confirme para
          prosseguir.
        </Alert>
        <FormField label="Motivo do cancelamento" required error={erro} help={`Máx. ${MOTIVO_MAX} caracteres.`}>
          {({ id, describedBy, invalid }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              aria-invalid={invalid || undefined}
              maxLength={MOTIVO_MAX}
              rows={4}
              value={motivo}
              onChange={(e) => setMotivo(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
