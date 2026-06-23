// Modal de LAVRATURA de auto da VISA (LavrarAutoPayload), pendurado numa inspeção concluída:
// intimação (sem multa) ou infração/penalidade (com multa opcional). Padrão mutation +
// validação por campo + Toast + ProblemDetails. Gating "saude.vigilancia.autuar" no chamador.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, Textarea, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useLavrarAuto } from './vigilancia.api';
import type { LavrarAutoInput, TipoAutoVisa } from './vigilancia.api';
import { opcoesTipoAuto } from './vigilancia.helpers';

export function LavrarAutoModal({
  open,
  onClose,
  inspecaoId,
  estabelecimentoId,
}: {
  open: boolean;
  onClose: () => void;
  inspecaoId: string;
  estabelecimentoId: string;
}) {
  const toast = useToast();
  const lavrar = useLavrarAuto(inspecaoId);
  const [tipo, setTipo] = useState<TipoAutoVisa>('Intimacao');
  const [numero, setNumero] = useState('');
  const [fundamentacao, setFundamentacao] = useState('');
  const [prazoFinal, setPrazoFinal] = useState('');
  const [valorMulta, setValorMulta] = useState('');
  const [errors, setErrors] = useState<{ numero?: string; fundamentacao?: string; prazoFinal?: string }>({});

  const exigeMulta = tipo !== 'Intimacao';

  function fechar(): void {
    setTipo('Intimacao');
    setNumero('');
    setFundamentacao('');
    setPrazoFinal('');
    setValorMulta('');
    setErrors({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: typeof errors = {};
    if (numero.trim() === '') next.numero = 'Informe o número do auto.';
    if (fundamentacao.trim() === '') next.fundamentacao = 'Informe a fundamentação.';
    if (prazoFinal === '') next.prazoFinal = 'Informe o prazo final.';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    const input: LavrarAutoInput = {
      estabelecimentoId,
      tipo,
      numero: numero.trim(),
      fundamentacao: fundamentacao.trim(),
      prazoFinal,
      valorMulta: exigeMulta && valorMulta !== '' ? Number(valorMulta) : null,
    };
    lavrar.mutate(input, {
      onSuccess: () => {
        toast.success('Auto lavrado com sucesso.', 'Sucesso');
        fechar();
      },
      onError: (error: unknown) =>
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível lavrar o auto.'),
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Lavrar auto"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={lavrar.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-lavrar-auto" loading={lavrar.isPending}>
            Lavrar auto
          </Button>
        </>
      }
    >
      <form id="form-lavrar-auto" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Tipo do auto" required>
          {({ id }) => (
            <Select
              id={id}
              options={opcoesTipoAuto}
              value={tipo}
              onChange={(e) => setTipo(e.target.value as TipoAutoVisa)}
            />
          )}
        </FormField>
        <div className="row">
          <div className="col-sm-6">
            <FormField label="Número do auto" required error={errors.numero}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  maxLength={40}
                  value={numero}
                  onChange={(e) => setNumero(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="Prazo final" required error={errors.prazoFinal}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  type="date"
                  value={prazoFinal}
                  onChange={(e) => setPrazoFinal(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>
        <FormField label="Fundamentação / pendências" required error={errors.fundamentacao}>
          {({ id, describedBy, invalid }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              rows={4}
              maxLength={2000}
              value={fundamentacao}
              onChange={(e) => setFundamentacao(e.target.value)}
            />
          )}
        </FormField>
        {exigeMulta && (
          <FormField label="Valor da multa (R$)" help="Deixe vazio se ainda não há valor cominado.">
            {({ id, describedBy }) => (
              <Input
                id={id}
                aria-describedby={describedBy}
                inputMode="decimal"
                value={valorMulta}
                onChange={(e) => setValorMulta(e.target.value.replace(/[^\d.,]/g, '').replace(',', '.'))}
                placeholder="0,00"
              />
            )}
          </FormField>
        )}
      </form>
    </Modal>
  );
}
