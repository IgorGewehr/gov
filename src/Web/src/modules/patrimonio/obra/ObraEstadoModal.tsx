// Transições de estado da obra (máquina de estados): [Command EmitirOrdemInicio]
// (Planejada -> EmExecucao), [Command ParalisarObra] (EmExecucao -> Paralisada — I-15)
// e [Command ReiniciarObra] (Paralisada -> EmExecucao). O modo define o comando.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useEmitirOrdemInicio, useParalisarObra, useReiniciarObra } from './obra.api';
import type { MotivoParalisacaoValor } from './obra.api';
import { OPCOES_MOTIVO_PARALISACAO, hojeIso } from './obra.helpers';

export type EstadoAcao = 'ordemInicio' | 'paralisar' | 'reiniciar';

export interface ObraEstadoModalProps {
  open: boolean;
  onClose: () => void;
  obraId: string;
  acao: EstadoAcao;
}

const TITULO: Record<EstadoAcao, string> = {
  ordemInicio: 'Emitir ordem de início',
  paralisar: 'Paralisar obra',
  reiniciar: 'Reiniciar obra',
};

export function ObraEstadoModal({ open, onClose, obraId, acao }: ObraEstadoModalProps) {
  const toast = useToast();
  const ordemInicio = useEmitirOrdemInicio(obraId);
  const paralisar = useParalisarObra(obraId);
  const reiniciar = useReiniciarObra(obraId);

  const [data, setData] = useState(hojeIso());
  const [motivo, setMotivo] = useState(String(OPCOES_MOTIVO_PARALISACAO[0]?.value ?? ''));
  const [erro, setErro] = useState<string | null>(null);

  const pendente = ordemInicio.isPending || paralisar.isPending || reiniciar.isPending;

  function fechar(): void {
    setErro(null);
    onClose();
  }

  function sucesso(msg: string): void {
    toast.success(msg, 'Sucesso');
    fechar();
  }

  function falha(error: unknown, padrao: string): void {
    toast.error(error instanceof ApiError ? error.userMessage : padrao);
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (data.trim() === '') {
      setErro('Informe a data.');
      return;
    }
    setErro(null);

    if (acao === 'ordemInicio') {
      ordemInicio.mutate(data, {
        onSuccess: () => sucesso('Ordem de início emitida — obra em execução.'),
        onError: (e) => falha(e, 'Não foi possível emitir a ordem de início.'),
      });
      return;
    }
    if (acao === 'paralisar') {
      paralisar.mutate(
        { motivo: Number(motivo) as MotivoParalisacaoValor, data },
        {
          onSuccess: () => sucesso('Obra paralisada.'),
          onError: (e) => falha(e, 'Não foi possível paralisar a obra.'),
        },
      );
      return;
    }
    reiniciar.mutate(data, {
      onSuccess: () => sucesso('Obra reiniciada — em execução.'),
      onError: (e) => falha(e, 'Não foi possível reiniciar a obra.'),
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title={TITULO[acao]}
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={pendente}>
            Cancelar
          </Button>
          <Button
            variant={acao === 'paralisar' ? 'danger' : 'primary'}
            type="submit"
            form="form-estado"
            loading={pendente}
          >
            Confirmar
          </Button>
        </>
      }
    >
      <form id="form-estado" className="br-form" onSubmit={submeter} noValidate>
        {erro && (
          <p className="text-danger text-down-01 mb-3" role="alert">
            {erro}
          </p>
        )}
        <FormField label="Data" required>
          {({ id, describedBy }) => (
            <Input
              id={id}
              type="date"
              aria-describedby={describedBy}
              value={data}
              onChange={(e) => setData(e.target.value)}
            />
          )}
        </FormField>
        {acao === 'paralisar' && (
          <FormField label="Motivo da paralisação" required>
            {({ id, describedBy }) => (
              <Select
                id={id}
                aria-describedby={describedBy}
                options={OPCOES_MOTIVO_PARALISACAO}
                value={motivo}
                onChange={(e) => setMotivo(e.target.value)}
              />
            )}
          </FormField>
        )}
      </form>
    </Modal>
  );
}
