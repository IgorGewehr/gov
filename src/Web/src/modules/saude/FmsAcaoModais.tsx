// Modais de GESTÃO do Fundo Municipal de Saúde (FMS): abrir o fundo e movimentar
// blocos (receber parcela do FNS / executar despesa). Todas as ações exigem
// "saude.gerenciar" (já gated pelos botões que abrem estes modais). Padrão
// mutation + validação por campo + Toast (espelha DiarioClasseFormModal).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import type { SelectOption } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import {
  BLOCO_SAUDE_VALOR,
  useAbrirFms,
  useExecutarDespesaFms,
  useReceberParcelaFms,
} from './fiscal.api';
import type { BlocoFinanciamentoSaude } from './fiscal.api';

const BLOCO_OPCOES: SelectOption[] = (
  Object.keys(BLOCO_SAUDE_VALOR) as BlocoFinanciamentoSaude[]
).map((k) => ({ value: k, label: k }));

// ---------------------------------------------------------------------------
// Abrir fundo (POST /saude/fiscal/fms)
// ---------------------------------------------------------------------------

export interface AbrirFmsModalProps {
  open: boolean;
  onClose: () => void;
  /** Recebe o id do fundo criado (preenche o campo de consulta do painel). */
  onCriado?: (id: string) => void;
}

export function AbrirFmsModal({ open, onClose, onCriado }: AbrirFmsModalProps) {
  const toast = useToast();
  const mutation = useAbrirFms();
  const [nome, setNome] = useState('Fundo Municipal de Saúde');
  const [cnpj, setCnpj] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (nome.trim() === '') {
      setErro('Informe o nome da unidade gestora.');
      return;
    }
    mutation.mutate(
      { nome: nome.trim(), cnpj: cnpj.trim() },
      {
        onSuccess: (r) => {
          toast.success('Fundo Municipal de Saúde aberto.', 'Sucesso');
          onCriado?.(r.id);
          fechar();
        },
        onError: (error) =>
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível abrir o fundo.'),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Abrir Fundo Municipal de Saúde"
      size="small"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-abrir-fms" loading={mutation.isPending}>
            Abrir fundo
          </Button>
        </>
      }
    >
      <form id="form-abrir-fms" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Nome da unidade gestora" required error={erro}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={nome}
              onChange={(e) => setNome(e.target.value)}
            />
          )}
        </FormField>
        <FormField label="CNPJ do Fundo" help="CNPJ próprio da unidade gestora.">
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={cnpj}
              onChange={(e) => setCnpj(e.target.value)}
              placeholder="00.000.000/0001-00"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------
// Movimentar bloco (parcela FNS / execução de despesa)
// ---------------------------------------------------------------------------

export interface MovimentoFmsModalProps {
  open: boolean;
  /** 'parcela' = receber do FNS; 'execucao' = executar despesa. */
  tipo: 'parcela' | 'execucao';
  fundoId: string;
  onClose: () => void;
}

export function MovimentoFmsModal({ open, tipo, fundoId, onClose }: MovimentoFmsModalProps) {
  const toast = useToast();
  const receber = useReceberParcelaFms(fundoId);
  const executar = useExecutarDespesaFms(fundoId);
  const mutation = tipo === 'parcela' ? receber : executar;

  const [bloco, setBloco] = useState<BlocoFinanciamentoSaude>('Custeio');
  const [fonteRecurso, setFonteRecurso] = useState('');
  const [valor, setValor] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  const titulo = tipo === 'parcela' ? 'Receber parcela do FNS' : 'Executar despesa no bloco';
  const acaoLabel = tipo === 'parcela' ? 'Receber parcela' : 'Executar despesa';

  function fechar(): void {
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const valorNum = Number((valor || '').replace(',', '.'));
    if (!Number.isFinite(valorNum) || valorNum <= 0) {
      setErro('Informe um valor maior que zero.');
      return;
    }
    if (fonteRecurso.trim() === '') {
      setErro('Informe a fonte de recurso.');
      return;
    }
    mutation.mutate(
      { bloco: BLOCO_SAUDE_VALOR[bloco], fonteRecurso: fonteRecurso.trim(), valor: valorNum },
      {
        onSuccess: () => {
          toast.success(`${acaoLabel} registrada.`, 'Sucesso');
          setValor('');
          setFonteRecurso('');
          fechar();
        },
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível registrar o movimento.',
          ),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title={titulo}
      size="small"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-movimento-fms" loading={mutation.isPending}>
            {acaoLabel}
          </Button>
        </>
      }
    >
      <form id="form-movimento-fms" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Bloco de financiamento" required>
          {({ id }) => (
            <Select
              id={id}
              options={BLOCO_OPCOES}
              value={bloco}
              onChange={(e) => setBloco(e.target.value as BlocoFinanciamentoSaude)}
            />
          )}
        </FormField>
        <FormField label="Fonte de recurso" required help="Identificação da fonte (carimbo do recurso).">
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={fonteRecurso}
              onChange={(e) => setFonteRecurso(e.target.value)}
            />
          )}
        </FormField>
        <FormField label="Valor (R$)" required error={erro}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="0"
              step="0.01"
              inputMode="decimal"
              aria-describedby={describedBy}
              invalid={invalid}
              value={valor}
              onChange={(e) => setValor(e.target.value)}
              placeholder="0,00"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
