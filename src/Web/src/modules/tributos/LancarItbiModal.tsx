// Ação de LANÇAR o ITBI (command LancarItbi): consolida a transmissão e gera a
// guia/DAM. Confirma transmitente e adquirente e reusa o imóvel/exercício/valor
// declarado/SFH já validados no preview. Ao concluir, exibe a guia gerada.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { formatarMoeda } from '../../i18n/format';
import { useLancarItbi } from './itbi.api';
import type { LancamentoItbiResultado } from './itbi.api';

export interface LancarItbiModalProps {
  open: boolean;
  onClose: () => void;
  imovelId: string;
  exercicio: number;
  valorDeclarado: number;
  sfh: boolean;
  baseCalculo: number;
  impostoDevido: number;
}

interface FormErrors {
  transmitenteId?: string;
  adquirenteId?: string;
  vencimento?: string;
}

export function LancarItbiModal({
  open,
  onClose,
  imovelId,
  exercicio,
  valorDeclarado,
  sfh,
  baseCalculo,
  impostoDevido,
}: LancarItbiModalProps) {
  const toast = useToast();
  const lancar = useLancarItbi();

  const [transmitente, setTransmitente] = useState('');
  const [adquirente, setAdquirente] = useState('');
  const [vencimento, setVencimento] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});
  const [resultado, setResultado] = useState<LancamentoItbiResultado | null>(null);

  function fechar(): void {
    setTransmitente('');
    setAdquirente('');
    setVencimento('');
    setErrors({});
    setResultado(null);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: FormErrors = {};
    if (transmitente.trim() === '') next.transmitenteId = 'Informe o transmitente (Guid do contribuinte).';
    if (adquirente.trim() === '') next.adquirenteId = 'Informe o adquirente (Guid do contribuinte).';
    if (vencimento.trim() === '') next.vencimento = 'Informe o vencimento da guia.';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    lancar.mutate(
      {
        imovelId,
        exercicio,
        valorDeclarado,
        usarAliquotaSfh: sfh,
        transmitenteId: transmitente.trim(),
        adquirenteId: adquirente.trim(),
        vencimento: vencimento.trim(),
      },
      {
        onSuccess: (res) => {
          setResultado(res);
          toast.success(`ITBI lançado — DAM ${res.damId}.`, 'Sucesso');
        },
        onError: (error) =>
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível lançar o ITBI.'),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Lançar ITBI e gerar guia"
      footer={
        resultado ? (
          <Button variant="primary" onClick={fechar}>
            Concluir
          </Button>
        ) : (
          <>
            <Button variant="secondary" onClick={fechar} disabled={lancar.isPending}>
              Cancelar
            </Button>
            <Button variant="primary" type="submit" form="form-lancar-itbi" loading={lancar.isPending}>
              Lançar e gerar guia
            </Button>
          </>
        )
      }
    >
      {resultado ? (
        <Alert variant="success" title="Guia gerada">
          Lançamento <strong>{resultado.lancamentoId}</strong> e DAM <strong>{resultado.damId}</strong>{' '}
          (transmissão {resultado.transmissaoId}) — imposto{' '}
          <strong>{formatarMoeda(resultado.impostoDevido)}</strong> sobre base de{' '}
          {formatarMoeda(resultado.baseCalculo)}.
        </Alert>
      ) : (
        <form id="form-lancar-itbi" className="br-form" onSubmit={submeter} noValidate>
          <Alert variant="info" title="Constituição do crédito">
            O lançamento constitui o crédito do ITBI sobre a base de{' '}
            <strong>{formatarMoeda(baseCalculo)}</strong> (imposto {formatarMoeda(impostoDevido)}) e gera a
            guia/DAM da transmissão.
          </Alert>
          <FormField label="Transmitente — Guid do contribuinte (vendedor)" required error={errors.transmitenteId}>
            {({ id, describedBy, invalid }) => (
              <Input id={id} aria-describedby={describedBy} invalid={invalid} value={transmitente} onChange={(e) => setTransmitente(e.target.value)} placeholder="00000000-0000-0000-0000-000000000000" />
            )}
          </FormField>
          <FormField label="Adquirente — Guid do contribuinte (comprador)" required error={errors.adquirenteId}>
            {({ id, describedBy, invalid }) => (
              <Input id={id} aria-describedby={describedBy} invalid={invalid} value={adquirente} onChange={(e) => setAdquirente(e.target.value)} placeholder="00000000-0000-0000-0000-000000000000" />
            )}
          </FormField>
          <FormField label="Vencimento da guia" required error={errors.vencimento}>
            {({ id, describedBy, invalid }) => (
              <Input id={id} type="date" aria-describedby={describedBy} invalid={invalid} value={vencimento} onChange={(e) => setVencimento(e.target.value)} />
            )}
          </FormField>
        </form>
      )}
    </Modal>
  );
}
