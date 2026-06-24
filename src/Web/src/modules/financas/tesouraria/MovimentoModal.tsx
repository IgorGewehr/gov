// Registro de movimento na conta (recebimento ou pagamento) em Modal.
//   POST /financas/tesouraria/contas/{id}/recebimentos | /pagamentos
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../../components/ui';
import { mensagemErro, tratarErroCampos } from '../financas.helpers';
import { hojeIso, useRegistrarPagamento, useRegistrarRecebimento } from './tesouraria.api';
import type { MovimentoInput } from './tesouraria.api';

export interface MovimentoModalProps {
  open: boolean;
  onClose: () => void;
  contaId: string;
  tipo: 'recebimento' | 'pagamento';
}

interface Campos {
  data?: string;
  valor?: string;
  historico?: string;
}

const CONHECIDOS: Record<string, true> = { data: true, valor: true, historico: true };

export function MovimentoModal({ open, onClose, contaId, tipo }: MovimentoModalProps) {
  const toast = useToast();
  const recebimento = useRegistrarRecebimento(contaId);
  const pagamento = useRegistrarPagamento(contaId);
  const mutation = tipo === 'recebimento' ? recebimento : pagamento;

  const [data, setData] = useState(hojeIso());
  const [valor, setValor] = useState('');
  const [historico, setHistorico] = useState('');
  const [documento, setDocumento] = useState('');
  const [errors, setErrors] = useState<Campos>({});

  const ehRecebimento = tipo === 'recebimento';

  function validar(): Campos {
    const next: Campos = {};
    if (data === '') next.data = 'Informe a data.';
    const v = Number(valor);
    if (valor.trim() === '' || Number.isNaN(v) || v <= 0) next.valor = 'Informe um valor maior que zero.';
    if (historico.trim() === '') next.historico = 'Informe o histórico.';
    return next;
  }

  function fechar(): void {
    setErrors({});
    setValor('');
    setHistorico('');
    setDocumento('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: MovimentoInput = {
      data,
      valor: Number(valor),
      historico: historico.trim(),
      documento: documento.trim() !== '' ? documento.trim() : null,
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success(ehRecebimento ? 'Recebimento registrado.' : 'Pagamento registrado.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        setErrors(tratarErroCampos(error, CONHECIDOS));
        toast.error(mensagemErro(error, 'Não foi possível registrar o movimento.'));
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title={ehRecebimento ? 'Registrar recebimento' : 'Registrar pagamento'}
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-movimento" loading={mutation.isPending}>
            Registrar
          </Button>
        </>
      }
    >
      <form id="form-movimento" className="br-form" onSubmit={submeter} noValidate>
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
              value={historico} onChange={(e) => setHistorico(e.target.value)}
              placeholder={ehRecebimento ? 'Ex.: Arrecadação IPTU' : 'Ex.: Pagamento OP 123 — Fornecedor X'} />
          )}
        </FormField>
        <FormField label="Documento (opcional)">
          {({ id, describedBy }) => (
            <Input id={id} aria-describedby={describedBy} maxLength={60}
              value={documento} onChange={(e) => setDocumento(e.target.value)}
              placeholder="Ex.: OP 123 / Guia 456" />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
