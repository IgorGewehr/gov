// Modal de lançamento contábil manual (POST /financas/contabilidade/lancamentos). Permite
// montar as partidas (conta analítica · lado · valor) e valida ΣDébitos = ΣCréditos antes
// de enviar — as mesmas invariantes são reforçadas no domínio (fonte da verdade).
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Alert,
  Button,
  FormField,
  Input,
  Modal,
  Select,
  Textarea,
  useToast,
} from '../../../components/ui';
import { formatarMoeda } from '../../../i18n/format';
import { hojeIso, mensagemErro } from '../financas.helpers';
import { LADO_PARTIDA, useRegistrarLancamentoManual } from './contabilidade.api';
import type { ContaContabil, LinhaManual } from './contabilidade.api';
import { LADOS_PARTIDA } from './contabilidade.helpers';

interface LinhaForm {
  codigoConta: string;
  lado: string;
  valor: string;
}

const LINHA_VAZIA: LinhaForm = { codigoConta: '', lado: String(LADO_PARTIDA.Debito), valor: '' };

function somaPorLado(linhas: LinhaForm[], lado: number): number {
  return linhas
    .filter((l) => Number(l.lado) === lado)
    .reduce((acc, l) => acc + (Number(l.valor) || 0), 0);
}

export interface LancamentoManualModalProps {
  open: boolean;
  onClose: () => void;
  /** Contas analíticas (lançáveis) para o seletor. */
  contas: ContaContabil[];
}

export function LancamentoManualModal({ open, onClose, contas }: LancamentoManualModalProps) {
  const toast = useToast();
  const mutation = useRegistrarLancamentoManual();
  const [data, setData] = useState(hojeIso());
  const [historico, setHistorico] = useState('');
  const [linhas, setLinhas] = useState<LinhaForm[]>([{ ...LINHA_VAZIA }, { ...LINHA_VAZIA }]);
  const [erro, setErro] = useState<string | undefined>();

  const opcoesContas = contas.map((c) => ({ value: c.codigo, label: `${c.codigo} — ${c.titulo}` }));
  const totalDebitos = somaPorLado(linhas, LADO_PARTIDA.Debito);
  const totalCreditos = somaPorLado(linhas, LADO_PARTIDA.Credito);
  const balanceado = totalDebitos > 0 && Math.abs(totalDebitos - totalCreditos) < 0.005;

  function fechar(): void {
    setData(hojeIso());
    setHistorico('');
    setLinhas([{ ...LINHA_VAZIA }, { ...LINHA_VAZIA }]);
    setErro(undefined);
    onClose();
  }

  function atualizarLinha(indice: number, campo: keyof LinhaForm, valor: string): void {
    setLinhas((atuais) => atuais.map((l, i) => (i === indice ? { ...l, [campo]: valor } : l)));
  }

  function adicionarLinha(): void {
    setLinhas((atuais) => [...atuais, { ...LINHA_VAZIA }]);
  }

  function removerLinha(indice: number): void {
    setLinhas((atuais) => (atuais.length <= 2 ? atuais : atuais.filter((_, i) => i !== indice)));
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (historico.trim() === '') {
      setErro('Informe o histórico do lançamento.');
      return;
    }
    if (linhas.some((l) => l.codigoConta === '' || !(Number(l.valor) > 0))) {
      setErro('Cada partida exige uma conta e um valor maior que zero.');
      return;
    }
    if (!balanceado) {
      setErro('A soma dos débitos deve ser igual à soma dos créditos.');
      return;
    }
    setErro(undefined);

    const payload: LinhaManual[] = linhas.map((l) => ({
      codigoConta: l.codigoConta,
      lado: Number(l.lado) as LinhaManual['lado'],
      valor: Number(l.valor),
    }));

    mutation.mutate(
      { data, historico: historico.trim(), linhas: payload },
      {
        onSuccess: () => {
          toast.success('Lançamento contábil registrado.', 'Sucesso');
          fechar();
        },
        onError: (error) =>
          toast.error(mensagemErro(error, 'Não foi possível registrar o lançamento.')),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Novo lançamento manual"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button
            variant="primary"
            type="submit"
            form="form-lancamento-manual"
            loading={mutation.isPending}
          >
            Registrar
          </Button>
        </>
      }
    >
      <form id="form-lancamento-manual" className="br-form" onSubmit={submeter} noValidate>
        {erro && (
          <Alert variant="danger" title="Verifique o lançamento">
            {erro}
          </Alert>
        )}

        <div className="row">
          <div className="col-12 col-md-4">
            <FormField label="Data" required>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="date"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={data}
                  onChange={(e) => setData(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-12 col-md-8">
            <FormField label="Histórico" required>
              {({ id, describedBy, invalid }) => (
                <Textarea
                  id={id}
                  rows={2}
                  maxLength={500}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={historico}
                  onChange={(e) => setHistorico(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>

        <fieldset className="mt-2">
          <legend className="text-up-01 text-semi-bold">Partidas</legend>
          {linhas.map((linha, indice) => (
            <div className="row align-items-end" key={indice}>
              <div className="col-12 col-md-6">
                <FormField label="Conta" required>
                  {({ id, describedBy, invalid }) => (
                    <Select
                      id={id}
                      options={opcoesContas}
                      placeholder="Selecione a conta"
                      aria-describedby={describedBy}
                      invalid={invalid}
                      value={linha.codigoConta}
                      onChange={(e) => atualizarLinha(indice, 'codigoConta', e.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-6 col-md-3">
                <FormField label="Lado" required>
                  {({ id, describedBy, invalid }) => (
                    <Select
                      id={id}
                      options={LADOS_PARTIDA}
                      aria-describedby={describedBy}
                      invalid={invalid}
                      value={linha.lado}
                      onChange={(e) => atualizarLinha(indice, 'lado', e.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-6 col-md-2">
                <FormField label="Valor (R$)" required>
                  {({ id, describedBy, invalid }) => (
                    <Input
                      id={id}
                      type="number"
                      min="0"
                      step="0.01"
                      inputMode="decimal"
                      aria-describedby={describedBy}
                      invalid={invalid}
                      value={linha.valor}
                      onChange={(e) => atualizarLinha(indice, 'valor', e.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-12 col-md-1 mb-3">
                <Button
                  variant="tertiary"
                  className="small"
                  onClick={() => removerLinha(indice)}
                  disabled={linhas.length <= 2}
                  aria-label={`Remover partida ${indice + 1}`}
                >
                  <i className="fas fa-trash" aria-hidden="true" />
                </Button>
              </div>
            </div>
          ))}
          <Button variant="secondary" className="small" onClick={adicionarLinha}>
            <i className="fas fa-plus" aria-hidden="true" /> Adicionar partida
          </Button>
        </fieldset>

        <dl className="row mt-3 mb-0">
          <dt className="col-6 col-md-3 text-gray-60">Total débitos</dt>
          <dd className="col-6 col-md-3 text-semi-bold">{formatarMoeda(totalDebitos)}</dd>
          <dt className="col-6 col-md-3 text-gray-60">Total créditos</dt>
          <dd className="col-6 col-md-3 text-semi-bold">{formatarMoeda(totalCreditos)}</dd>
        </dl>
      </form>
    </Modal>
  );
}
