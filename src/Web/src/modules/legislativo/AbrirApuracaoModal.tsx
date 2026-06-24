// Modal de ABERTURA da apuracao do art. 29-A para um exercicio. Coleta a populacao
// (faixa do caput), o repasse/duodecimo (base do subteto §1), a base de receita do
// ano anterior — INFORMADA ou OBTIDA DE FINANCAS — e as despesas discriminadas por
// natureza. Mutation + validacao por campo + Toast. Demonstrativo nasce em Rascunho.
import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { NATUREZAS_DESPESA_CAMARA } from './legislativo.shared';
import { tratarErroCampos, mensagemErro } from './legislativoAcao.shared';
import {
  useAbrirApuracaoArt29A,
  type AbrirApuracaoInput,
  type DespesaCamaraInput,
} from './limiteCamara.api';

export interface AbrirApuracaoModalProps {
  open: boolean;
  onClose: () => void;
  /** Exercicio sob teto, ja escolhido na tela (a base usa o ano anterior). */
  exercicio: number;
}

type FonteBase = 'informada' | 'financas';

interface LinhaDespesa {
  natureza: string;
  valor: string;
  descricao: string;
}

interface FormErrors {
  populacao?: string;
  repasseRecebido?: string;
  receitaTributaria?: string;
  transferencias?: string;
  despesas?: string;
}

const CAMPOS: Record<string, number> = {
  populacao: 1,
  repasseRecebido: 1,
  receitaTributaria: 1,
  transferencias: 1,
  despesas: 1,
};

function linhaInicial(): LinhaDespesa {
  return { natureza: String(NATUREZAS_DESPESA_CAMARA[0].value), valor: '', descricao: '' };
}

function numeroValido(texto: string): boolean {
  if (texto.trim() === '') return false;
  const valor = Number(texto);
  return Number.isFinite(valor) && valor >= 0;
}

export function AbrirApuracaoModal({ open, onClose, exercicio }: AbrirApuracaoModalProps) {
  const toast = useToast();
  const abrir = useAbrirApuracaoArt29A();

  const [populacao, setPopulacao] = useState('');
  const [repasse, setRepasse] = useState('');
  const [fonte, setFonte] = useState<FonteBase>('informada');
  const [receitaTributaria, setReceitaTributaria] = useState('');
  const [transferencias, setTransferencias] = useState('');
  const [despesas, setDespesas] = useState<LinhaDespesa[]>([linhaInicial()]);
  const [errors, setErrors] = useState<FormErrors>({});

  useEffect(() => {
    if (!open) return;
    setPopulacao('');
    setRepasse('');
    setFonte('informada');
    setReceitaTributaria('');
    setTransferencias('');
    setDespesas([linhaInicial()]);
    setErrors({});
  }, [open]);

  function atualizarDespesa(indice: number, campo: keyof LinhaDespesa, valor: string): void {
    setDespesas((atual) =>
      atual.map((linha, i) => (i === indice ? { ...linha, [campo]: valor } : linha)),
    );
  }

  function adicionarDespesa(): void {
    setDespesas((atual) => [...atual, linhaInicial()]);
  }

  function removerDespesa(indice: number): void {
    setDespesas((atual) => (atual.length <= 1 ? atual : atual.filter((_, i) => i !== indice)));
  }

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (!Number.isInteger(Number(populacao)) || Number(populacao) <= 0)
      next.populacao = 'Informe a população (inteiro maior que zero).';
    if (!numeroValido(repasse)) next.repasseRecebido = 'Informe o repasse recebido (≥ 0).';
    if (fonte === 'informada') {
      if (!numeroValido(receitaTributaria))
        next.receitaTributaria = 'Informe a receita tributária do exercício anterior (≥ 0).';
      if (!numeroValido(transferencias))
        next.transferencias = 'Informe as transferências do exercício anterior (≥ 0).';
    }
    const algumaInvalida = despesas.some((d) => !numeroValido(d.valor));
    if (algumaInvalida) next.despesas = 'Cada despesa precisa de um valor válido (≥ 0).';
    return next;
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const parcelas: DespesaCamaraInput[] = despesas.map((d) => ({
      natureza: Number(d.natureza),
      valor: Number(d.valor),
      descricao: d.descricao.trim() === '' ? null : d.descricao.trim(),
    }));

    const input: AbrirApuracaoInput = {
      exercicio,
      populacao: Number(populacao),
      repasseRecebido: Number(repasse),
      receitaTributaria: fonte === 'informada' ? Number(receitaTributaria) : null,
      transferencias: fonte === 'informada' ? Number(transferencias) : null,
      despesas: parcelas,
    };

    abrir.mutate(input, {
      onSuccess: () => {
        toast.success(`Apuração do exercício ${exercicio} aberta (rascunho).`, 'Sucesso');
        onClose();
      },
      onError: (error) => {
        setErrors((atual) => ({ ...atual, ...tratarErroCampos(error, CAMPOS) }));
        toast.error(mensagemErro(error, 'Não foi possível abrir a apuração.'));
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      size="large"
      title={`Abrir apuração do art. 29-A — exercício ${exercicio}`}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={abrir.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-art29a" loading={abrir.isPending}>
            Abrir apuração
          </Button>
        </>
      }
    >
      <form id="form-art29a" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="info" title="Estimativa local (prestação)">
          O cálculo do teto e dos semáforos é uma estimativa local a partir dos dados informados. A
          base de receita é a do exercício anterior ({exercicio - 1}). A prestação oficial ao TCE-RS é
          etapa posterior.
        </Alert>

        <div className="row">
          <div className="col-sm-6">
            <FormField label="População do município" required error={errors.populacao}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  type="number"
                  min="1"
                  step="1"
                  inputMode="numeric"
                  value={populacao}
                  onChange={(e) => setPopulacao(e.target.value)}
                  placeholder="Ex.: 5200"
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField
              label="Repasse/duodecimo recebido"
              required
              error={errors.repasseRecebido}
              help="Base do subteto da folha (§1)."
            >
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  type="number"
                  min="0"
                  step="0.01"
                  inputMode="decimal"
                  value={repasse}
                  onChange={(e) => setRepasse(e.target.value)}
                  placeholder="Ex.: 3200000.00"
                />
              )}
            </FormField>
          </div>
        </div>

        <FormField label="Base de receita do exercício anterior" help={`Receita de ${exercicio - 1}.`}>
          {({ id }) => (
            <Select
              id={id}
              value={fonte}
              onChange={(e) => setFonte(e.target.value as FonteBase)}
              options={[
                { value: 'informada', label: 'Informar valores (balanço/oficial)' },
                { value: 'financas', label: 'Obter de Finanças (se este tenant detém a contabilidade)' },
              ]}
            />
          )}
        </FormField>

        {fonte === 'informada' && (
          <div className="row">
            <div className="col-sm-6">
              <FormField label="Receita tributária" required error={errors.receitaTributaria}>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    type="number"
                    min="0"
                    step="0.01"
                    inputMode="decimal"
                    value={receitaTributaria}
                    onChange={(e) => setReceitaTributaria(e.target.value)}
                  />
                )}
              </FormField>
            </div>
            <div className="col-sm-6">
              <FormField label="Transferências" required error={errors.transferencias}>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    type="number"
                    min="0"
                    step="0.01"
                    inputMode="decimal"
                    value={transferencias}
                    onChange={(e) => setTransferencias(e.target.value)}
                  />
                )}
              </FormField>
            </div>
          </div>
        )}

        <fieldset className="mt-3">
          <legend className="text-up-01 text-semi-bold">Despesas realizadas (por natureza)</legend>
          {errors.despesas && (
            <p className="feedback danger" role="alert">
              <i className="fas fa-times-circle" aria-hidden="true" /> {errors.despesas}
            </p>
          )}
          {despesas.map((linha, indice) => (
            <div className="row align-items-end" key={indice}>
              <div className="col-sm-4">
                <FormField label="Natureza">
                  {({ id }) => (
                    <Select
                      id={id}
                      value={linha.natureza}
                      onChange={(e) => atualizarDespesa(indice, 'natureza', e.target.value)}
                      options={NATUREZAS_DESPESA_CAMARA.map((n) => ({
                        value: String(n.value),
                        label: n.label,
                      }))}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-sm-3">
                <FormField label="Valor">
                  {({ id }) => (
                    <Input
                      id={id}
                      type="number"
                      min="0"
                      step="0.01"
                      inputMode="decimal"
                      value={linha.valor}
                      onChange={(e) => atualizarDespesa(indice, 'valor', e.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-sm-4">
                <FormField label="Descrição (opcional)">
                  {({ id }) => (
                    <Input
                      id={id}
                      value={linha.descricao}
                      onChange={(e) => atualizarDespesa(indice, 'descricao', e.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-sm-1 mb-3">
                <Button
                  variant="tertiary"
                  iconOnly
                  aria-label={`Remover despesa ${indice + 1}`}
                  disabled={despesas.length <= 1}
                  onClick={() => removerDespesa(indice)}
                >
                  <i className="fas fa-trash" aria-hidden="true" />
                </Button>
              </div>
            </div>
          ))}
          <Button variant="secondary" size="sm" onClick={adicionarDespesa}>
            <i className="fas fa-plus" aria-hidden="true" /> Adicionar despesa
          </Button>
        </fieldset>
      </form>
    </Modal>
  );
}
