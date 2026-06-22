// Formulário de cadastro da TABELA RPPS municipal (lei previdenciária do ente), em Modal.
// Padrão-ouro: mutation + validação + Toast. As faixas são progressivas e contíguas (a
// primeira inicia em zero). Sem esta tabela, o motor recusa o cálculo do servidor efetivo.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useCriarTabelaRpps } from './tabelaLegal.api';
import type { CriarTabelaRppsInput, FaixaProgressivaInput } from './tabelaLegal.api';
import { MESES } from './recursosHumanos.helpers';

export interface CriarTabelaRppsFormModalProps {
  open: boolean;
  onClose: () => void;
  anoInicial: number;
  mesInicial: number;
}

/** Linha editável de faixa (strings — convertidas ao submeter). */
interface FaixaForm {
  limiteInferior: string;
  limiteSuperior: string;
  /** Alíquota digitada em percentual (ex.: "14"). */
  aliquota: string;
}

const FAIXA_VAZIA: FaixaForm = { limiteInferior: '', limiteSuperior: '', aliquota: '' };

const ANO_ATUAL = new Date().getFullYear();

export function CriarTabelaRppsFormModal({
  open,
  onClose,
  anoInicial,
  mesInicial,
}: CriarTabelaRppsFormModalProps) {
  const toast = useToast();
  const mutation = useCriarTabelaRpps();

  const [ano, setAno] = useState(String(anoInicial || ANO_ATUAL));
  const [mes, setMes] = useState(String(mesInicial || new Date().getMonth() + 1));
  const [baseLegal, setBaseLegal] = useState('');
  const [teto, setTeto] = useState('');
  const [faixas, setFaixas] = useState<FaixaForm[]>([{ ...FAIXA_VAZIA, limiteInferior: '0' }]);
  const [erro, setErro] = useState<string | undefined>();

  function atualizarFaixa(indice: number, campo: keyof FaixaForm, valor: string): void {
    setFaixas((prev) =>
      prev.map((f, i) => (i === indice ? { ...f, [campo]: valor } : f)),
    );
  }

  function adicionarFaixa(): void {
    setFaixas((prev) => [...prev, { ...FAIXA_VAZIA }]);
  }

  function removerFaixa(indice: number): void {
    setFaixas((prev) => prev.filter((_, i) => i !== indice));
  }

  function validar(): string | undefined {
    if (baseLegal.trim() === '') return 'Informe a base legal (lei municipal).';
    if (faixas.length === 0) return 'Informe ao menos uma faixa.';
    for (const [i, f] of faixas.entries()) {
      const inf = Number(f.limiteInferior);
      const sup = Number(f.limiteSuperior);
      const aliq = Number(f.aliquota);
      if (Number.isNaN(inf) || inf < 0) return `Faixa ${i + 1}: limite inferior inválido.`;
      if (Number.isNaN(sup) || sup <= inf)
        return `Faixa ${i + 1}: limite superior deve ser maior que o inferior.`;
      if (Number.isNaN(aliq) || aliq <= 0 || aliq > 100)
        return `Faixa ${i + 1}: alíquota deve estar entre 0 e 100%.`;
    }
    return undefined;
  }

  function fechar(): void {
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const mensagem = validar();
    setErro(mensagem);
    if (mensagem) return;

    const faixasInput: FaixaProgressivaInput[] = faixas.map((f) => ({
      limiteInferior: Number(f.limiteInferior),
      limiteSuperior: Number(f.limiteSuperior),
      // Alíquota em fração decimal (0..1): 14% -> 0,14.
      aliquota: Number(f.aliquota) / 100,
    }));

    const input: CriarTabelaRppsInput = {
      anoVigencia: Number(ano),
      mesVigencia: Number(mes),
      faixas: faixasInput,
      teto: teto.trim() === '' ? null : Number(teto),
      baseLegal: baseLegal.trim(),
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Tabela RPPS municipal cadastrada.', 'Sucesso');
        fechar();
      },
      onError: (error) =>
        toast.error(
          error instanceof ApiError
            ? error.userMessage
            : 'Não foi possível cadastrar a tabela RPPS.',
        ),
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Cadastrar tabela RPPS municipal"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button
            variant="primary"
            type="submit"
            form="form-criar-rpps"
            loading={mutation.isPending}
          >
            Cadastrar
          </Button>
        </>
      }
    >
      <form id="form-criar-rpps" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="warning" title="Depende de lei municipal.">
          O RPPS é parametrizado pela lei previdenciária do ente — não há padrão federal.
          Sem esta tabela, o cálculo de servidores efetivos é recusado (fail-closed).
        </Alert>

        <div className="row">
          <div className="col-sm-6">
            <FormField label="Mês de vigência" required>
              {({ id, describedBy }) => (
                <Select
                  id={id}
                  aria-describedby={describedBy}
                  value={mes}
                  onChange={(e) => setMes(e.target.value)}
                  options={MESES}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="Ano de vigência" required>
              {({ id, describedBy }) => (
                <Input
                  id={id}
                  type="number"
                  min="2000"
                  max="2100"
                  step="1"
                  inputMode="numeric"
                  aria-describedby={describedBy}
                  value={ano}
                  onChange={(e) => setAno(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>

        <FormField label="Base legal (lei municipal)" required>
          {({ id, describedBy }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              value={baseLegal}
              onChange={(e) => setBaseLegal(e.target.value)}
              maxLength={200}
              placeholder="Ex.: Lei Municipal nº 1.234/2024"
            />
          )}
        </FormField>

        <FormField label="Teto da base (R$, opcional)">
          {({ id, describedBy }) => (
            <Input
              id={id}
              type="number"
              min="0"
              step="0.01"
              inputMode="decimal"
              aria-describedby={describedBy}
              value={teto}
              onChange={(e) => setTeto(e.target.value)}
            />
          )}
        </FormField>

        <fieldset className="mt-2">
          <legend className="text-down-01 text-gray-60">
            Faixas progressivas (contíguas; a primeira inicia em zero)
          </legend>
          {faixas.map((f, i) => (
            <div className="row align-items-end" key={i}>
              <div className="col-4">
                <FormField label={`Faixa ${i + 1} — de (R$)`}>
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      type="number"
                      min="0"
                      step="0.01"
                      inputMode="decimal"
                      aria-describedby={describedBy}
                      value={f.limiteInferior}
                      onChange={(e) => atualizarFaixa(i, 'limiteInferior', e.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-4">
                <FormField label="até (R$)">
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      type="number"
                      min="0"
                      step="0.01"
                      inputMode="decimal"
                      aria-describedby={describedBy}
                      value={f.limiteSuperior}
                      onChange={(e) => atualizarFaixa(i, 'limiteSuperior', e.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-3">
                <FormField label="Alíquota (%)">
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      type="number"
                      min="0"
                      max="100"
                      step="0.01"
                      inputMode="decimal"
                      aria-describedby={describedBy}
                      value={f.aliquota}
                      onChange={(e) => atualizarFaixa(i, 'aliquota', e.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-1 mb-3">
                <Button
                  variant="tertiary"
                  onClick={() => removerFaixa(i)}
                  disabled={faixas.length === 1}
                  aria-label={`Remover faixa ${i + 1}`}
                >
                  <i className="fas fa-trash" aria-hidden="true" />
                </Button>
              </div>
            </div>
          ))}
          <Button variant="secondary" onClick={adicionarFaixa}>
            <i className="fas fa-plus" aria-hidden="true" /> Adicionar faixa
          </Button>
        </fieldset>

        {erro && (
          <p className="feedback danger mt-3" role="alert">
            <i className="fas fa-times-circle" aria-hidden="true" /> {erro}
          </p>
        )}
      </form>
    </Modal>
  );
}
