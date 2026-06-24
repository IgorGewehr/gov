// Abertura de conta da tesouraria (POST /financas/tesouraria/contas) em Modal. Dados bancários
// só são exigidos para conta bancária. useMutation + validação por campo + Toast.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../../components/ui';
import { mensagemErro, tratarErroCampos } from '../financas.helpers';
import { TIPO_CONTA_FINANCEIRA, useAbrirConta } from './tesouraria.api';
import type { AbrirContaInput } from './tesouraria.api';

export interface AbrirContaModalProps {
  open: boolean;
  onClose: () => void;
}

interface Campos {
  nome?: string;
  tipo?: string;
  banco?: string;
  agencia?: string;
  conta?: string;
  saldoInicial?: string;
}

const CONHECIDOS: Record<string, true> = {
  nome: true,
  tipo: true,
  banco: true,
  agencia: true,
  conta: true,
  saldoInicial: true,
};

const TIPOS = [
  { value: String(TIPO_CONTA_FINANCEIRA.Bancaria), label: 'Bancária' },
  { value: String(TIPO_CONTA_FINANCEIRA.Caixa), label: 'Caixa' },
];

export function AbrirContaModal({ open, onClose }: AbrirContaModalProps) {
  const toast = useToast();
  const mutation = useAbrirConta();

  const [nome, setNome] = useState('');
  const [tipo, setTipo] = useState(String(TIPO_CONTA_FINANCEIRA.Bancaria));
  const [banco, setBanco] = useState('');
  const [agencia, setAgencia] = useState('');
  const [conta, setConta] = useState('');
  const [pix, setPix] = useState('');
  const [saldo, setSaldo] = useState('0');
  const [errors, setErrors] = useState<Campos>({});

  const ehBancaria = Number(tipo) === TIPO_CONTA_FINANCEIRA.Bancaria;

  function validar(): Campos {
    const next: Campos = {};
    if (nome.trim() === '') next.nome = 'Informe o nome da conta.';
    const v = Number(saldo);
    if (saldo.trim() === '' || Number.isNaN(v) || v < 0) next.saldoInicial = 'Saldo inicial inválido.';
    if (ehBancaria) {
      if (banco.trim() === '') next.banco = 'Informe o banco.';
      if (agencia.trim() === '') next.agencia = 'Informe a agência.';
      if (conta.trim() === '') next.conta = 'Informe a conta.';
    }
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

    const input: AbrirContaInput = {
      nome: nome.trim(),
      tipo: Number(tipo) as AbrirContaInput['tipo'],
      banco: ehBancaria ? banco.trim() : null,
      agencia: ehBancaria ? agencia.trim() : null,
      conta: ehBancaria ? conta.trim() : null,
      pix: ehBancaria && pix.trim() !== '' ? pix.trim() : null,
      saldoInicial: Number(saldo),
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Conta aberta.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        setErrors(tratarErroCampos(error, CONHECIDOS));
        toast.error(mensagemErro(error, 'Não foi possível abrir a conta.'));
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Abrir conta da tesouraria"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-abrir-conta" loading={mutation.isPending}>
            Abrir
          </Button>
        </>
      }
    >
      <form id="form-abrir-conta" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Nome da conta" required error={errors.nome}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} aria-describedby={describedBy} invalid={invalid} maxLength={120}
              value={nome} onChange={(e) => setNome(e.target.value)} placeholder="Ex.: Conta Movimento — Banrisul 1234" />
          )}
        </FormField>
        <FormField label="Espécie" required error={errors.tipo}>
          {({ id, describedBy, invalid }) => (
            <Select id={id} aria-describedby={describedBy} invalid={invalid}
              value={tipo} onChange={(e) => setTipo(e.target.value)} options={TIPOS} />
          )}
        </FormField>
        {ehBancaria && (
          <>
            <FormField label="Banco" required error={errors.banco}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} aria-describedby={describedBy} invalid={invalid} maxLength={20}
                  value={banco} onChange={(e) => setBanco(e.target.value)} placeholder="Ex.: 041 — Banrisul" />
              )}
            </FormField>
            <FormField label="Agência" required error={errors.agencia}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} aria-describedby={describedBy} invalid={invalid} maxLength={20}
                  value={agencia} onChange={(e) => setAgencia(e.target.value)} />
              )}
            </FormField>
            <FormField label="Conta" required error={errors.conta}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} aria-describedby={describedBy} invalid={invalid} maxLength={30}
                  value={conta} onChange={(e) => setConta(e.target.value)} />
              )}
            </FormField>
            <FormField label="Chave PIX (opcional)">
              {({ id, describedBy }) => (
                <Input id={id} aria-describedby={describedBy} maxLength={140}
                  value={pix} onChange={(e) => setPix(e.target.value)} />
              )}
            </FormField>
          </>
        )}
        <FormField label="Saldo inicial (R$)" required error={errors.saldoInicial}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} type="number" min="0" step="0.01" inputMode="decimal"
              aria-describedby={describedBy} invalid={invalid}
              value={saldo} onChange={(e) => setSaldo(e.target.value)} />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
