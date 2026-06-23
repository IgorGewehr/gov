// Reclassificação de ramo/risco de um estabelecimento fiscalizável (ReclassificarPayload).
// Ação gated por "saude.vigilancia.gerenciar" na página que o abre.
import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Modal, Select, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useReclassificarEstabelecimento } from './vigilancia.api';
import type { EstabelecimentoVisaDto, GrauRiscoSanitario, RamoVisa } from './vigilancia.api';
import { opcoesRamo, opcoesRisco } from './vigilancia.helpers';

export function VisaReclassificarModal({
  open,
  onClose,
  estabelecimento,
}: {
  open: boolean;
  onClose: () => void;
  estabelecimento: EstabelecimentoVisaDto | null;
}) {
  const toast = useToast();
  const reclassificar = useReclassificarEstabelecimento(estabelecimento?.id ?? '');
  const [ramo, setRamo] = useState<RamoVisa>('Alimentacao');
  const [risco, setRisco] = useState<GrauRiscoSanitario>('Baixo');

  useEffect(() => {
    if (open && estabelecimento) {
      setRamo(estabelecimento.ramo);
      setRisco(estabelecimento.risco);
    }
  }, [open, estabelecimento]);

  function submeter(event: FormEvent): void {
    event.preventDefault();
    reclassificar.mutate(
      { ramo, risco },
      {
        onSuccess: () => {
          toast.success('Classificação atualizada.', 'Sucesso');
          onClose();
        },
        onError: (error: unknown) =>
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível reclassificar.'),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Reclassificar estabelecimento"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={reclassificar.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-reclassificar" loading={reclassificar.isPending}>
            Salvar
          </Button>
        </>
      }
    >
      <form id="form-reclassificar" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Ramo de atividade" required>
          {({ id }) => (
            <Select id={id} options={opcoesRamo} value={ramo} onChange={(e) => setRamo(e.target.value as RamoVisa)} />
          )}
        </FormField>
        <FormField label="Grau de risco" required>
          {({ id }) => (
            <Select
              id={id}
              options={opcoesRisco}
              value={risco}
              onChange={(e) => setRisco(e.target.value as GrauRiscoSanitario)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
