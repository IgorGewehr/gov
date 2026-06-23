// Modais de LICENÇA/alvará sanitário da VISA: emitir (com estabelecimento + inspeção fundante
// opcional) e renovar. Padrão mutation + validação por campo + Toast + ProblemDetails. Gating
// feito nas páginas que os abrem (saude.vigilancia.licenciar).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { EstabelecimentoVisaPicker } from './EstabelecimentoVisaPicker';
import { useEmitirLicenca, useRenovarLicenca } from './vigilancia.api';
import type { EmitirLicencaInput, RenovarLicencaInput } from './vigilancia.api';

// ---------- Emitir licença ----------

export function EmitirLicencaModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  const toast = useToast();
  const emitir = useEmitirLicenca();
  const [estabelecimentoId, setEstabelecimentoId] = useState('');
  const [numero, setNumero] = useState('');
  const [validadeAte, setValidadeAte] = useState('');
  const [inspecaoId, setInspecaoId] = useState('');
  const [errors, setErrors] = useState<{ estabelecimentoId?: string; numero?: string; validadeAte?: string }>({});

  function fechar(): void {
    setEstabelecimentoId('');
    setNumero('');
    setValidadeAte('');
    setInspecaoId('');
    setErrors({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: typeof errors = {};
    if (estabelecimentoId === '') next.estabelecimentoId = 'Selecione o estabelecimento.';
    if (numero.trim() === '') next.numero = 'Informe o número do alvará.';
    if (validadeAte === '') next.validadeAte = 'Informe a validade.';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    const input: EmitirLicencaInput = {
      estabelecimentoId,
      numero: numero.trim(),
      validadeAte,
      inspecaoId: inspecaoId.trim() || null,
    };
    emitir.mutate(input, {
      onSuccess: () => {
        toast.success('Licença emitida com sucesso.', 'Sucesso');
        fechar();
      },
      onError: (error: unknown) =>
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível emitir a licença.'),
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Emitir licença sanitária"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={emitir.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-emitir-licenca" loading={emitir.isPending}>
            Emitir
          </Button>
        </>
      }
    >
      <form id="form-emitir-licenca" className="br-form" onSubmit={submeter} noValidate>
        <EstabelecimentoVisaPicker
          label="Estabelecimento"
          required
          value={estabelecimentoId}
          onChange={(id) => {
            setEstabelecimentoId(id);
            if (id) setErrors((e) => ({ ...e, estabelecimentoId: undefined }));
          }}
          error={errors.estabelecimentoId}
        />
        <div className="row">
          <div className="col-sm-6">
            <FormField label="Número do alvará" required error={errors.numero}>
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
            <FormField label="Validade até" required error={errors.validadeAte}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  type="date"
                  value={validadeAte}
                  onChange={(e) => setValidadeAte(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>
        <FormField
          label="Inspeção fundante (ID)"
          help="Obrigatória para risco médio/alto; dispensável para risco baixo."
        >
          {({ id, describedBy }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              value={inspecaoId}
              onChange={(e) => setInspecaoId(e.target.value)}
              placeholder="GUID da inspeção concluída"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

// ---------- Renovar licença ----------

export function RenovarLicencaModal({
  open,
  onClose,
  licencaId,
}: {
  open: boolean;
  onClose: () => void;
  licencaId: string;
}) {
  const toast = useToast();
  const renovar = useRenovarLicenca(licencaId);
  const [numero, setNumero] = useState('');
  const [validadeAte, setValidadeAte] = useState('');
  const [inspecaoId, setInspecaoId] = useState('');
  const [errors, setErrors] = useState<{ numero?: string; validadeAte?: string }>({});

  function fechar(): void {
    setNumero('');
    setValidadeAte('');
    setInspecaoId('');
    setErrors({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: typeof errors = {};
    if (numero.trim() === '') next.numero = 'Informe o número do alvará renovado.';
    if (validadeAte === '') next.validadeAte = 'Informe a nova validade.';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    const input: RenovarLicencaInput = {
      numero: numero.trim(),
      validadeAte,
      inspecaoId: inspecaoId.trim() || null,
    };
    renovar.mutate(input, {
      onSuccess: () => {
        toast.success('Licença renovada com sucesso.', 'Sucesso');
        fechar();
      },
      onError: (error: unknown) =>
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível renovar a licença.'),
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Renovar licença sanitária"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={renovar.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-renovar-licenca" loading={renovar.isPending}>
            Renovar
          </Button>
        </>
      }
    >
      <form id="form-renovar-licenca" className="br-form" onSubmit={submeter} noValidate>
        <div className="row">
          <div className="col-sm-6">
            <FormField label="Número do alvará" required error={errors.numero}>
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
            <FormField label="Nova validade" required error={errors.validadeAte}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  type="date"
                  value={validadeAte}
                  onChange={(e) => setValidadeAte(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>
        <FormField label="Inspeção fundante (ID)" help="Obrigatória para risco médio/alto.">
          {({ id, describedBy }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              value={inspecaoId}
              onChange={(e) => setInspecaoId(e.target.value)}
              placeholder="GUID da inspeção concluída"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
