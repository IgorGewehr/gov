// Modais de DECISÃO da Solicitação de Regulação (SISREG): autorizar, negar, devolver,
// executar e cancelar. WIRED a mutations TanStack Query + Toast. As ações que exigem
// justificativa usam MotivoPayload. Gated por "saude.gerenciar" na DetailPage.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Modal, Textarea, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import {
  useAutorizarSolicitacao,
  useCancelarSolicitacao,
  useDevolverSolicitacao,
  useExecutarSolicitacao,
  useNegarSolicitacao,
} from './api';

const MOTIVO_MAX = 2000;

interface AcaoRegulacaoProps {
  open: boolean;
  onClose: () => void;
  solicitacaoId: string;
}

// --- AUTORIZAR (confirmação) --------------------------------------------------

export function AutorizarSolicitacaoModal({ open, onClose, solicitacaoId }: AcaoRegulacaoProps) {
  const toast = useToast();
  const mutation = useAutorizarSolicitacao(solicitacaoId);

  function confirmar(event: FormEvent): void {
    event.preventDefault();
    mutation.mutate(undefined, {
      onSuccess: () => {
        toast.success('Solicitação autorizada.', 'Sucesso');
        onClose();
      },
      onError: (error) => {
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível autorizar a solicitação.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Autorizar solicitação"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-autorizar" loading={mutation.isPending}>
            Autorizar
          </Button>
        </>
      }
    >
      <form id="form-autorizar" className="br-form" onSubmit={confirmar} noValidate>
        <Alert variant="info" title="Autorização:">
          Defere a solicitação, reserva a vaga no SISREG e consome a cota do procedimento.
        </Alert>
      </form>
    </Modal>
  );
}

// --- EXECUTAR (confirmação) ---------------------------------------------------

export function ExecutarSolicitacaoModal({ open, onClose, solicitacaoId }: AcaoRegulacaoProps) {
  const toast = useToast();
  const mutation = useExecutarSolicitacao(solicitacaoId);

  function confirmar(event: FormEvent): void {
    event.preventDefault();
    mutation.mutate(undefined, {
      onSuccess: () => {
        toast.success('Solicitação marcada como executada.', 'Sucesso');
        onClose();
      },
      onError: (error) => {
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível executar a solicitação.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Registrar execução"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-executar" loading={mutation.isPending}>
            Confirmar execução
          </Button>
        </>
      }
    >
      <form id="form-executar" className="br-form" onSubmit={confirmar} noValidate>
        <Alert variant="info" title="Execução:">
          Registra que o procedimento autorizado foi realizado. Estado terminal.
        </Alert>
      </form>
    </Modal>
  );
}

// --- AÇÃO COM MOTIVO (negar / devolver / cancelar) ----------------------------

type TipoMotivo = 'negar' | 'devolver' | 'cancelar';

interface MotivoModalProps extends AcaoRegulacaoProps {
  tipo: TipoMotivo;
}

const CONFIG: Record<
  TipoMotivo,
  { title: string; label: string; botao: string; variante: 'danger' | 'primary'; aviso: string }
> = {
  negar: {
    title: 'Negar solicitação',
    label: 'Motivo da negativa',
    botao: 'Negar',
    variante: 'danger',
    aviso: 'A negativa é terminal: a solicitação é indeferida e não retorna à fila.',
  },
  devolver: {
    title: 'Devolver solicitação',
    label: 'Motivo da devolução',
    botao: 'Devolver',
    variante: 'primary',
    aviso: 'A devolução retorna a solicitação ao solicitante para complementação.',
  },
  cancelar: {
    title: 'Cancelar solicitação',
    label: 'Motivo do cancelamento',
    botao: 'Cancelar solicitação',
    variante: 'danger',
    aviso: 'O cancelamento é terminal e encerra a solicitação.',
  },
};

export function MotivoRegulacaoModal({ open, onClose, solicitacaoId, tipo }: MotivoModalProps) {
  const toast = useToast();
  const negar = useNegarSolicitacao(solicitacaoId);
  const devolver = useDevolverSolicitacao(solicitacaoId);
  const cancelar = useCancelarSolicitacao(solicitacaoId);
  const mutation = tipo === 'negar' ? negar : tipo === 'devolver' ? devolver : cancelar;

  const cfg = CONFIG[tipo];
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
      setErro('Informe o motivo.');
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
          toast.success(`${cfg.title} concluída.`, 'Sucesso');
          fechar();
        },
        onError: (error) => {
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível concluir a ação.');
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title={cfg.title}
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Voltar
          </Button>
          <Button variant={cfg.variante} type="submit" form="form-motivo-regulacao" loading={mutation.isPending}>
            {cfg.botao}
          </Button>
        </>
      }
    >
      <form id="form-motivo-regulacao" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant={cfg.variante === 'danger' ? 'danger' : 'warning'} title="Atenção:">
          {cfg.aviso}
        </Alert>
        <FormField label={cfg.label} required error={erro} help={`Máx. ${MOTIVO_MAX} caracteres.`}>
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
