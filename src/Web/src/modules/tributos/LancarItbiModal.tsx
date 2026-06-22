// Ação de LANÇAR o ITBI (command LancarItbi): consolida a transmissão e gera a
// guia/DAM. Confirma transmitente e adquirente e reusa o imóvel/exercício/valor
// declarado/SFH já validados no preview. Ao concluir, exibe a guia gerada.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { formatarData, formatarMoeda } from '../../i18n/format';
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
  transmitente?: string;
  adquirente?: string;
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
  const [errors, setErrors] = useState<FormErrors>({});
  const [resultado, setResultado] = useState<LancamentoItbiResultado | null>(null);

  function fechar(): void {
    setTransmitente('');
    setAdquirente('');
    setErrors({});
    setResultado(null);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: FormErrors = {};
    if (transmitente.trim() === '') next.transmitente = 'Informe o transmitente (vendedor).';
    if (adquirente.trim() === '') next.adquirente = 'Informe o adquirente (comprador).';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    lancar.mutate(
      {
        imovelId,
        exercicio,
        valorDeclarado,
        sfh,
        transmitente: transmitente.trim(),
        adquirente: adquirente.trim(),
      },
      {
        onSuccess: (res) => {
          setResultado(res);
          toast.success(`ITBI lançado — guia ${res.guiaNumero}.`, 'Sucesso');
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
          Guia <strong>{resultado.guiaNumero}</strong> (lançamento {resultado.lancamentoId}) — imposto{' '}
          <strong>{formatarMoeda(resultado.impostoDevido)}</strong> sobre base de{' '}
          {formatarMoeda(resultado.baseCalculo)}, com vencimento em {formatarData(resultado.vencimento)}.
        </Alert>
      ) : (
        <form id="form-lancar-itbi" className="br-form" onSubmit={submeter} noValidate>
          <Alert variant="info" title="Constituição do crédito">
            O lançamento constitui o crédito do ITBI sobre a base de{' '}
            <strong>{formatarMoeda(baseCalculo)}</strong> (imposto {formatarMoeda(impostoDevido)}) e gera a
            guia/DAM da transmissão.
          </Alert>
          <FormField label="Transmitente (vendedor)" required error={errors.transmitente}>
            {({ id, describedBy, invalid }) => (
              <Input id={id} aria-describedby={describedBy} invalid={invalid} value={transmitente} onChange={(e) => setTransmitente(e.target.value)} />
            )}
          </FormField>
          <FormField label="Adquirente (comprador)" required error={errors.adquirente}>
            {({ id, describedBy, invalid }) => (
              <Input id={id} aria-describedby={describedBy} invalid={invalid} value={adquirente} onChange={(e) => setAdquirente(e.target.value)} />
            )}
          </FormField>
        </form>
      )}
    </Modal>
  );
}
