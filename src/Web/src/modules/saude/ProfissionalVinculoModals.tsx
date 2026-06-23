// Modais de AÇÃO de vínculo do agregado Profissional: abrir vínculo CNES/CBO e encerrá-lo.
// O estabelecimento é informado por GUID (o backend valida existência/ativação).
// WIRED a mutations TanStack Query + Toast. Ações gated por "saude.gerenciar" na DetailPage.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useEncerrarVinculo, useVincularProfissional } from './api';

interface VinculoProps {
  open: boolean;
  onClose: () => void;
  profissionalId: string;
}

const HOJE = () => new Date().toISOString().slice(0, 10);

// --- ABRIR VÍNCULO CNES/CBO ----------------------------------------------------

export function VincularProfissionalModal({ open, onClose, profissionalId }: VinculoProps) {
  const toast = useToast();
  const mutation = useVincularProfissional(profissionalId);
  const [estabelecimentoId, setEstabelecimentoId] = useState('');
  const [cbo, setCbo] = useState('');
  const [dataInicio, setDataInicio] = useState(HOJE());
  const [erros, setErros] = useState<{ estabelecimentoId?: string; cbo?: string; dataInicio?: string }>({});

  function fechar(): void {
    setErros({});
    setEstabelecimentoId('');
    setCbo('');
    setDataInicio(HOJE());
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: typeof erros = {};
    if (estabelecimentoId.trim() === '') next.estabelecimentoId = 'Informe o estabelecimento (GUID).';
    if (cbo.replace(/\D/g, '').length !== 6) next.cbo = 'O CBO deve ter 6 dígitos.';
    if (dataInicio.trim() === '') next.dataInicio = 'Informe a data de início.';
    setErros(next);
    if (Object.keys(next).length > 0) return;

    mutation.mutate(
      { estabelecimentoId: estabelecimentoId.trim(), cbo: cbo.replace(/\D/g, ''), dataInicio },
      {
        onSuccess: () => {
          toast.success('Vínculo aberto com sucesso.', 'Sucesso');
          fechar();
        },
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível abrir o vínculo.',
          ),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Abrir vínculo (CNES/CBO)"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-vincular" loading={mutation.isPending}>
            Vincular
          </Button>
        </>
      }
    >
      <form id="form-vincular" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="info" title="Coerência:">
          O estabelecimento (CNES) deve existir e estar ativo no acervo local para receber o vínculo.
        </Alert>
        <FormField
          label="Estabelecimento (ID)"
          required
          error={erros.estabelecimentoId}
          help="Identificador (GUID) do estabelecimento de saúde."
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={estabelecimentoId}
              onChange={(e) => setEstabelecimentoId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>
        <div className="row">
          <div className="col-sm-6">
            <FormField label="CBO" required error={erros.cbo} help="Ocupação — 6 dígitos.">
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  inputMode="numeric"
                  maxLength={6}
                  value={cbo}
                  onChange={(e) => setCbo(e.target.value.replace(/\D/g, ''))}
                  placeholder="225125"
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="Data de início" required error={erros.dataInicio}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="date"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={dataInicio}
                  onChange={(e) => setDataInicio(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}

// --- ENCERRAR VÍNCULO ----------------------------------------------------------

export interface EncerrarVinculoModalProps extends VinculoProps {
  /** Estabelecimento do vínculo a encerrar (pré-selecionado). */
  estabelecimentoId: string;
}

export function EncerrarVinculoModal({
  open,
  onClose,
  profissionalId,
  estabelecimentoId,
}: EncerrarVinculoModalProps) {
  const toast = useToast();
  const mutation = useEncerrarVinculo(profissionalId);
  const [dataFim, setDataFim] = useState(HOJE());
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setErro(undefined);
    setDataFim(HOJE());
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (dataFim.trim() === '') {
      setErro('Informe a data de encerramento.');
      return;
    }
    setErro(undefined);
    mutation.mutate(
      { estabelecimentoId, dataFim },
      {
        onSuccess: () => {
          toast.success('Vínculo encerrado.', 'Sucesso');
          fechar();
        },
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível encerrar o vínculo.',
          ),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Encerrar vínculo"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="danger" type="submit" form="form-encerrar-vinculo" loading={mutation.isPending}>
            Encerrar
          </Button>
        </>
      }
    >
      <form id="form-encerrar-vinculo" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Data de encerramento" required error={erro}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="date"
              aria-describedby={describedBy}
              invalid={invalid}
              value={dataFim}
              onChange={(e) => setDataFim(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
