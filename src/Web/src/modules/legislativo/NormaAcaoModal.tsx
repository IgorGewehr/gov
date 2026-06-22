// Modal de acao de Norma (revogacao/alteracao). Captura a data do evento e a norma
// referenciada (que revoga/altera esta). Mutation + validacao + Toast.
//   Revogar  -> { dataRevogacao, normaRevogadoraId? }   (norma referenciada opcional)
//   Alterar  -> { dataReferencia, normaAlteradoraId }   (norma referenciada obrigatoria)
import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { mensagemErro } from './legislativoAcao.shared';
import { useAlterarNorma, useRevogarNorma } from './normas.api';

export interface NormaAcaoModalProps {
  open: boolean;
  onClose: () => void;
  /** Identificador da norma que sera revogada/alterada. */
  id: string;
  tipo: 'revogar' | 'alterar';
}

interface FormErrors {
  data?: string;
  normaReferenciaId?: string;
}

const TEXTOS = {
  revogar: {
    title: 'Revogar norma',
    rotulo: 'Revogar',
    sucesso: 'Revogação registrada.',
    erro: 'Não foi possível registrar a revogação.',
    rotuloData: 'Data da revogação',
    rotuloNorma: 'Norma revogadora (opcional)',
    normaObrigatoria: false,
    // mapeamento campo-backend(camelCase) -> campo do form (para exibir erros do backend)
    campos: { dataRevogacao: 'data', normaRevogadoraId: 'normaReferenciaId' } as Record<string, keyof FormErrors>,
  },
  alterar: {
    title: 'Registrar alteração',
    rotulo: 'Registrar',
    sucesso: 'Alteração registrada.',
    erro: 'Não foi possível registrar a alteração.',
    rotuloData: 'Data da alteração',
    rotuloNorma: 'Norma alteradora',
    normaObrigatoria: true,
    campos: { dataReferencia: 'data', normaAlteradoraId: 'normaReferenciaId' } as Record<string, keyof FormErrors>,
  },
} as const;

export function NormaAcaoModal({ open, onClose, id, tipo }: NormaAcaoModalProps) {
  const toast = useToast();
  const revogar = useRevogarNorma(id);
  const alterar = useAlterarNorma(id);
  const mutation = tipo === 'revogar' ? revogar : alterar;
  const textos = TEXTOS[tipo];

  const [data, setData] = useState('');
  const [normaReferenciaId, setNormaReferenciaId] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  useEffect(() => {
    if (!open) return;
    setData('');
    setNormaReferenciaId('');
    setErrors({});
  }, [open]);

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (data === '') next.data = 'Informe a data.';
    if (textos.normaObrigatoria && normaReferenciaId.trim() === '')
      next.normaReferenciaId = 'Informe a norma.';
    return next;
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const referencia = normaReferenciaId.trim();
    const onError = (error: unknown) => {
      if (error instanceof ApiError) {
        const mapped: FormErrors = {};
        for (const [field, messages] of Object.entries(error.fieldErrors)) {
          const key = field.charAt(0).toLowerCase() + field.slice(1);
          const alvo = textos.campos[key];
          if (alvo) mapped[alvo] = messages[0];
        }
        setErrors(mapped);
      }
      toast.error(mensagemErro(error, textos.erro));
    };
    const onSuccess = () => {
      toast.success(textos.sucesso, 'Sucesso');
      onClose();
    };

    if (tipo === 'revogar') {
      revogar.mutate(
        { dataRevogacao: data, normaRevogadoraId: referencia === '' ? undefined : referencia },
        { onSuccess, onError },
      );
    } else {
      alterar.mutate(
        { dataReferencia: data, normaAlteradoraId: referencia },
        { onSuccess, onError },
      );
    }
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={textos.title}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-norma-acao" loading={mutation.isPending}>
            {textos.rotulo}
          </Button>
        </>
      }
    >
      <form id="form-norma-acao" className="br-form" onSubmit={submeter} noValidate>
        <FormField label={textos.rotuloData} required error={errors.data}>
          {({ id: fieldId, describedBy, invalid }) => (
            <Input
              id={fieldId}
              aria-describedby={describedBy}
              invalid={invalid}
              type="date"
              value={data}
              onChange={(e) => setData(e.target.value)}
            />
          )}
        </FormField>

        <FormField
          label={textos.rotuloNorma}
          required={textos.normaObrigatoria}
          error={errors.normaReferenciaId}
          help="Identificador da norma que revoga/altera esta."
        >
          {({ id: fieldId, describedBy, invalid }) => (
            <Input
              id={fieldId}
              aria-describedby={describedBy}
              invalid={invalid}
              value={normaReferenciaId}
              onChange={(e) => setNormaReferenciaId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
