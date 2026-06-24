// Transferência entre contas da tesouraria (POST /financas/tesouraria/transferencias) em Modal.
// Operação auditada e atômica; a conta destino deve ser diferente da origem.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../../components/ui';
import { mensagemErro, tratarErroCampos } from '../financas.helpers';
import { hojeIso, useTransferir } from './tesouraria.api';
import type { ContaFinanceira, TransferenciaInput } from './tesouraria.api';

export interface TransferenciaModalProps {
  open: boolean;
  onClose: () => void;
  contas: ContaFinanceira[];
}

interface Campos {
  contaOrigemId?: string;
  contaDestinoId?: string;
  data?: string;
  valor?: string;
  historico?: string;
}

const CONHECIDOS: Record<string, true> = {
  contaOrigemId: true,
  contaDestinoId: true,
  data: true,
  valor: true,
  historico: true,
};

export function TransferenciaModal({ open, onClose, contas }: TransferenciaModalProps) {
  const toast = useToast();
  const mutation = useTransferir();

  const [origem, setOrigem] = useState('');
  const [destino, setDestino] = useState('');
  const [data, setData] = useState(hojeIso());
  const [valor, setValor] = useState('');
  const [historico, setHistorico] = useState('');
  const [errors, setErrors] = useState<Campos>({});

  const opcoes = contas
    .filter((c) => c.situacao === 'Ativa')
    .map((c) => ({ value: c.id, label: c.nome }));

  function validar(): Campos {
    const next: Campos = {};
    if (origem === '') next.contaOrigemId = 'Selecione a conta de origem.';
    if (destino === '') next.contaDestinoId = 'Selecione a conta de destino.';
    if (destino !== '' && destino === origem) next.contaDestinoId = 'Destino deve ser diferente da origem.';
    if (data === '') next.data = 'Informe a data.';
    const v = Number(valor);
    if (valor.trim() === '' || Number.isNaN(v) || v <= 0) next.valor = 'Informe um valor maior que zero.';
    if (historico.trim() === '') next.historico = 'Informe o histórico.';
    return next;
  }

  function fechar(): void {
    setErrors({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: TransferenciaInput = {
      contaOrigemId: origem,
      contaDestinoId: destino,
      data,
      valor: Number(valor),
      historico: historico.trim(),
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Transferência efetuada.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        setErrors(tratarErroCampos(error, CONHECIDOS));
        toast.error(mensagemErro(error, 'Não foi possível transferir.'));
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Transferir entre contas"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-transferir" loading={mutation.isPending}>
            Transferir
          </Button>
        </>
      }
    >
      <form id="form-transferir" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Conta de origem" required error={errors.contaOrigemId}>
          {({ id, describedBy, invalid }) => (
            <Select id={id} aria-describedby={describedBy} invalid={invalid}
              value={origem} onChange={(e) => setOrigem(e.target.value)}
              placeholder="Selecione…" options={opcoes} />
          )}
        </FormField>
        <FormField label="Conta de destino" required error={errors.contaDestinoId}>
          {({ id, describedBy, invalid }) => (
            <Select id={id} aria-describedby={describedBy} invalid={invalid}
              value={destino} onChange={(e) => setDestino(e.target.value)}
              placeholder="Selecione…" options={opcoes} />
          )}
        </FormField>
        <FormField label="Data" required error={errors.data}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} type="date" aria-describedby={describedBy} invalid={invalid}
              value={data} onChange={(e) => setData(e.target.value)} />
          )}
        </FormField>
        <FormField label="Valor (R$)" required error={errors.valor}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} type="number" min="0" step="0.01" inputMode="decimal"
              aria-describedby={describedBy} invalid={invalid}
              value={valor} onChange={(e) => setValor(e.target.value)} />
          )}
        </FormField>
        <FormField label="Histórico" required error={errors.historico}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} aria-describedby={describedBy} invalid={invalid} maxLength={300}
              value={historico} onChange={(e) => setHistorico(e.target.value)} />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
