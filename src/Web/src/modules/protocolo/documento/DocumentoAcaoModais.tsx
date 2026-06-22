// Modais de ACAO do agregado Documento (abertos a partir da DetailPage):
//   - AssinarDocumentoModal -> Command 5.2 (transicao Assinar; valida nivel x criticidade)
//   - TornarSemEfeitoModal  -> Command 5.3 (transicao SemEfeito; destrutivo -> confirmacao)
//   - VerificarIntegridadeModal -> Query 6.2 (reexportado de VerificarIntegridadeModal.tsx)
// Cada um e wired ao hook correspondente, com validacao e Toast. WCAG AA via FormField/Modal.
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Alert,
  Button,
  FormField,
  Input,
  Modal,
  Select,
  useToast,
} from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import {
  NivelMinimoPorCriticidade,
  TipoAssinaturaValor,
  useAssinarDocumento,
  useTornarDocumentoSemEfeito,
} from './documento.api';
import type { CriticidadeAto, DocumentoResumo, TipoAssinatura } from './documento.api';
import {
  OPCOES_TIPO_ASSINATURA,
  assinaturaAtendeCriticidade,
  rotuloTipoAssinatura,
} from './documento.helpers';

// Verificacao de integridade (Query 6.2) vive em arquivo proprio; reexportada aqui
// para preservar o ponto unico de import da DetailPage (cada arquivo < 300 linhas).
export { VerificarIntegridadeModal } from './VerificarIntegridadeModal';

interface AcaoModalProps {
  open: boolean;
  onClose: () => void;
  processoId: string;
  documento: DocumentoResumo;
}

// ---------------------------------------------------------------------------
// Command 5.2 — Assinar documento (transicao Juntado|Assinado -> Assinado)
// ---------------------------------------------------------------------------

interface AssinarErrors {
  signatarioId?: string;
  tipo?: string;
}

export function AssinarDocumentoModal({ open, onClose, processoId, documento }: AcaoModalProps) {
  const toast = useToast();
  const mutation = useAssinarDocumento(processoId);
  const [signatarioId, setSignatarioId] = useState('');
  const [tipo, setTipo] = useState<TipoAssinatura | ''>('');
  const [errors, setErrors] = useState<AssinarErrors>({});

  const criticidade = documento.criticidade as CriticidadeAto;
  const nivelInsuficiente =
    tipo !== '' && !assinaturaAtendeCriticidade(criticidade, tipo as TipoAssinatura);

  function fechar(): void {
    setSignatarioId('');
    setTipo('');
    setErrors({});
    onClose();
  }

  function validar(): AssinarErrors {
    const next: AssinarErrors = {};
    if (signatarioId.trim() === '') next.signatarioId = 'Informe o identificador do signatario.';
    if (tipo === '') next.tipo = 'Selecione o tipo de assinatura.';
    else if (!assinaturaAtendeCriticidade(criticidade, tipo as TipoAssinatura))
      next.tipo = `Criticidade ${criticidade} exige assinatura de nivel igual ou superior ao minimo (Decreto 10.543/2020).`;
    return next;
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    mutation.mutate(
      {
        documentoId: documento.id,
        input: { signatarioId: signatarioId.trim(), tipo: TipoAssinaturaValor[tipo as TipoAssinatura] },
      },
      {
        onSuccess: () => {
          toast.success('Documento assinado com carimbo de tempo.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
            const mapped: AssinarErrors = {};
            for (const [field, messages] of Object.entries(error.fieldErrors)) {
              const key = field.charAt(0).toLowerCase() + field.slice(1);
              if (key === 'signatarioId' || key === 'tipo') {
                (mapped as Record<string, string>)[key] = messages[0];
              }
            }
            setErrors((prev) => ({ ...prev, ...mapped }));
          }
          toast.error(error instanceof ApiError ? error.userMessage : 'Nao foi possivel assinar o documento.');
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Assinar documento"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button
            variant="primary"
            type="submit"
            form="form-assinar-documento"
            loading={mutation.isPending}
            disabled={nivelInsuficiente}
          >
            Assinar
          </Button>
        </>
      }
    >
      <form id="form-assinar-documento" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="info">
          Criticidade <strong>{criticidade}</strong>: nivel minimo exigido{' '}
          <strong>{rotuloTipoAssinatura(NivelMinimoPorCriticidade[criticidade])}</strong>. Toda assinatura recebe
          carimbo de tempo (Lei 14.063/2020).
        </Alert>

        <FormField label="Identificador do signatario" required error={errors.signatarioId}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={signatarioId}
              onChange={(e) => setSignatarioId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>

        <FormField label="Tipo de assinatura" required error={errors.tipo}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              options={OPCOES_TIPO_ASSINATURA}
              placeholder="Selecione o tipo de assinatura"
              value={tipo}
              onChange={(e) => setTipo(e.target.value as TipoAssinatura | '')}
            />
          )}
        </FormField>

        {nivelInsuficiente && (
          <Alert variant="danger">
            O nivel selecionado e inferior ao exigido pela criticidade {criticidade} e sera rejeitado pelo
            sistema (I-5/I-6).
          </Alert>
        )}
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------
// Command 5.3 — Tornar sem efeito (DESTRUTIVO: estado terminal -> confirmacao)
// ---------------------------------------------------------------------------

export function TornarSemEfeitoModal({ open, onClose, processoId, documento }: AcaoModalProps) {
  const toast = useToast();
  const mutation = useTornarDocumentoSemEfeito(processoId);
  const [motivo, setMotivo] = useState('');
  const [confirmado, setConfirmado] = useState(false);
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setMotivo('');
    setConfirmado(false);
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (motivo.trim() === '') {
      setErro('Informe o motivo (obrigatorio, max. 500 caracteres).');
      return;
    }
    if (motivo.trim().length > 500) {
      setErro('O motivo deve ter no maximo 500 caracteres.');
      return;
    }
    setErro(undefined);

    mutation.mutate(
      { documentoId: documento.id, input: { motivo: motivo.trim() } },
      {
        onSuccess: () => {
          toast.success('Documento tornado sem efeito. O registro permanece na trilha documental.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          if (error instanceof ApiError && error.fieldErrors.motivo?.length) {
            setErro(error.fieldErrors.motivo[0]);
          }
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Nao foi possivel tornar o documento sem efeito.',
          );
        },
      },
    );
  }

  const podeConfirmar = confirmado && motivo.trim() !== '';

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Tornar documento sem efeito"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button
            variant="primary"
            type="submit"
            form="form-sem-efeito"
            loading={mutation.isPending}
            disabled={!podeConfirmar}
          >
            Tornar sem efeito
          </Button>
        </>
      }
    >
      <form id="form-sem-efeito" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="warning">
          Acao <strong>irreversivel</strong>: o documento passa a <strong>Sem efeito</strong> (estado terminal) e
          nao admite novas transicoes. O registro <strong>nao</strong> e excluido — a trilha documental e
          preservada (Lei 11.419/2006; I-4/I-9/I-10).
        </Alert>

        <FormField
          label="Motivo"
          required
          error={erro}
          help={`${motivo.trim().length}/500 caracteres.`}
        >
          {({ id, describedBy, invalid }) => (
            <textarea
              id={id}
              className="br-textarea"
              aria-describedby={describedBy}
              aria-invalid={invalid || undefined}
              maxLength={500}
              rows={4}
              value={motivo}
              onChange={(e) => setMotivo(e.target.value)}
            />
          )}
        </FormField>

        <div className="br-checkbox">
          <input
            id="confirmar-sem-efeito"
            type="checkbox"
            checked={confirmado}
            onChange={(e) => setConfirmado(e.target.checked)}
          />
          <label htmlFor="confirmar-sem-efeito">
            Confirmo que desejo tornar este documento sem efeito.
          </label>
        </div>
      </form>
    </Modal>
  );
}
