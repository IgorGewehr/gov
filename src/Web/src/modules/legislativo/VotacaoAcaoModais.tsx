// Modal de ACAO do agregado Votacao com corpo de formulario (Registrar voto
// nominal). As transicoes simples (encerramento, cancelamento) usam
// ConfirmarAcaoModal diretamente na DetailPage.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { SENTIDOS_VOTO } from './legislativo.shared';
import { useRegistrarVoto } from './votacao.api';
import { mensagemErro, tratarErroCampos } from './legislativoAcao.shared';
import type { AcaoModalBaseProps } from './legislativoAcao.shared';

export function RegistrarVotoModal({ open, onClose, id }: AcaoModalBaseProps) {
  const toast = useToast();
  const mutation = useRegistrarVoto(id);
  const [votoId, setVotoId] = useState('');
  const [vereadorId, setVereadorId] = useState('');
  const [sentido, setSentido] = useState<string>(String(SENTIDOS_VOTO[0].value));
  const [errors, setErrors] = useState<{ votoId?: string; vereadorId?: string }>({});

  function fechar(): void {
    setErrors({});
    setVotoId('');
    setVereadorId('');
    setSentido(String(SENTIDOS_VOTO[0].value));
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: { votoId?: string; vereadorId?: string } = {};
    if (votoId.trim() === '') next.votoId = 'Informe o identificador do voto (idempotência).';
    if (vereadorId.trim() === '') next.vereadorId = 'Informe o identificador do vereador.';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    mutation.mutate(
      { votoId: votoId.trim(), vereadorId: vereadorId.trim(), sentido: Number(sentido) },
      {
        onSuccess: () => {
          toast.success('Voto registrado.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          setErrors(tratarErroCampos(error, { votoId: 1, vereadorId: 1 }));
          toast.error(mensagemErro(error, 'Não foi possível registrar o voto.'));
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar voto"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-voto" loading={mutation.isPending}>
            Registrar
          </Button>
        </>
      }
    >
      <form id="form-voto" className="br-form" onSubmit={submeter} noValidate>
        <FormField
          label="Identificador do voto"
          required
          error={errors.votoId}
          help="UUID único do voto (garante idempotência do registro)."
        >
          {({ id: fid, describedBy, invalid }) => (
            <Input
              id={fid}
              aria-describedby={describedBy}
              invalid={invalid}
              value={votoId}
              onChange={(e) => setVotoId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>
        <FormField label="Vereador (identificador)" required error={errors.vereadorId}>
          {({ id: fid, describedBy, invalid }) => (
            <Input
              id={fid}
              aria-describedby={describedBy}
              invalid={invalid}
              value={vereadorId}
              onChange={(e) => setVereadorId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>
        <FormField label="Sentido do voto" required>
          {({ id: fid, describedBy }) => (
            <Select
              id={fid}
              aria-describedby={describedBy}
              value={sentido}
              onChange={(e) => setSentido(e.target.value)}
              options={SENTIDOS_VOTO.map((s) => ({ value: String(s.value), label: s.label }))}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
