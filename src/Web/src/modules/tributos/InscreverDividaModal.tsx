// Modal de AÇÃO "Inscrever em Dívida Ativa" (command InscreverEmDividaAtiva) a partir de um
// LANÇAMENTO vencido e não pago. Espelha o InscreverDividaPayload real: fundamento legal +
// encargos PARAMETRIZÁVEIS por tenant (multa/juros/correção — lei municipal) + fundamento dos
// encargos. Datas (constituição definitiva, inscrição) e anos de prescrição são opcionais —
// ausentes, o backend usa o vencimento do lançamento e 5 anos (CTN art. 174). WIRED a mutation.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useInscreverEmDividaAtiva } from './api';

export interface InscreverDividaModalProps {
  open: boolean;
  onClose: () => void;
  /** Contribuinte cuja lista de dívidas deve ser invalidada após a inscrição. */
  contribuinteIdParaInvalidar?: string;
}

interface CampoErro {
  [campo: string]: string | undefined;
}

export function InscreverDividaModal({
  open,
  onClose,
  contribuinteIdParaInvalidar,
}: InscreverDividaModalProps) {
  const toast = useToast();
  const mutation = useInscreverEmDividaAtiva(contribuinteIdParaInvalidar);
  const [lancamentoId, setLancamentoId] = useState('');
  const [fundamentoLegal, setFundamentoLegal] = useState('');
  const [fundamentoEncargos, setFundamentoEncargos] = useState('');
  const [multa, setMulta] = useState('');
  const [juros, setJuros] = useState('');
  const [correcao, setCorrecao] = useState('');
  const [constituicao, setConstituicao] = useState('');
  const [inscricao, setInscricao] = useState('');
  const [erros, setErros] = useState<CampoErro>({});

  function fechar(): void {
    setLancamentoId('');
    setFundamentoLegal('');
    setFundamentoEncargos('');
    setMulta('');
    setJuros('');
    setCorrecao('');
    setConstituicao('');
    setInscricao('');
    setErros({});
    onClose();
  }

  function validar(): CampoErro | null {
    const e: CampoErro = {};
    if (lancamentoId.trim() === '') e.lancamentoId = 'Informe o identificador do lançamento.';
    if (fundamentoLegal.trim() === '') e.fundamentoLegal = 'Informe o fundamento legal.';
    if (fundamentoEncargos.trim() === '') e.fundamentoEncargos = 'Informe o fundamento dos encargos.';
    for (const [campo, valor] of [
      ['multa', multa],
      ['juros', juros],
      ['correcao', correcao],
    ] as const) {
      const num = Number(valor);
      if (valor.trim() === '' || Number.isNaN(num) || num < 0) {
        e[campo] = 'Informe um percentual ≥ 0.';
      }
    }
    return Object.keys(e).length > 0 ? e : null;
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const e = validar();
    if (e) {
      setErros(e);
      return;
    }
    setErros({});
    mutation.mutate(
      {
        lancamentoId: lancamentoId.trim(),
        input: {
          fundamentoLegal: fundamentoLegal.trim(),
          multaMoraPercentual: Number(multa),
          jurosMoraPercentualMensal: Number(juros),
          correcaoPercentualMensal: Number(correcao),
          fundamentoEncargos: fundamentoEncargos.trim(),
          dataConstituicaoDefinitiva: constituicao || null,
          dataInscricao: inscricao || null,
        },
      },
      {
        onSuccess: () => {
          toast.success('Lançamento inscrito em Dívida Ativa.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          toast.error(
            error instanceof ApiError
              ? error.userMessage
              : 'Não foi possível inscrever em Dívida Ativa.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Inscrever em Dívida Ativa"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-inscrever-divida" loading={mutation.isPending}>
            Inscrever
          </Button>
        </>
      }
    >
      <form id="form-inscrever-divida" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="info" title="Inscrição de crédito vencido">
          Só é possível inscrever um lançamento VENCIDO e em aberto. Os encargos (multa, juros e
          correção) são definidos pela lei municipal. A prescrição (CTN art. 174) corre da
          constituição definitiva — se em branco, o sistema usa o vencimento do lançamento.
        </Alert>
        <FormField label="Identificador do lançamento" required error={erros.lancamentoId}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={lancamentoId}
              onChange={(e) => setLancamentoId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>
        <FormField
          label="Fundamento legal da dívida (inc. III)"
          required
          error={erros.fundamentoLegal}
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={300}
              value={fundamentoLegal}
              onChange={(e) => setFundamentoLegal(e.target.value)}
              placeholder="Ex.: Lei Municipal nº 1.234/2010, art. 5º (CTM)"
            />
          )}
        </FormField>
        <div className="row">
          <div className="col-12 col-sm-4">
            <FormField label="Multa de mora (%)" required error={erros.multa}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min={0}
                  step="0.01"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={multa}
                  onChange={(e) => setMulta(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-12 col-sm-4">
            <FormField label="Juros de mora (% a.m.)" required error={erros.juros}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min={0}
                  step="0.01"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={juros}
                  onChange={(e) => setJuros(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-12 col-sm-4">
            <FormField label="Correção (% a.m.)" required error={erros.correcao}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min={0}
                  step="0.01"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={correcao}
                  onChange={(e) => setCorrecao(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>
        <FormField
          label="Fundamento legal dos encargos"
          required
          error={erros.fundamentoEncargos}
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={300}
              value={fundamentoEncargos}
              onChange={(e) => setFundamentoEncargos(e.target.value)}
              placeholder="Ex.: CTM, arts. 20-22 (multa/juros/correção)"
            />
          )}
        </FormField>
        <div className="row">
          <div className="col-12 col-sm-6">
            <FormField
              label="Constituição definitiva"
              help="Início da prescrição. Em branco: usa o vencimento."
            >
              {({ id, describedBy }) => (
                <Input
                  id={id}
                  type="date"
                  aria-describedby={describedBy}
                  value={constituicao}
                  onChange={(e) => setConstituicao(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-12 col-sm-6">
            <FormField label="Data da inscrição" help="Em branco: usa a constituição definitiva.">
              {({ id, describedBy }) => (
                <Input
                  id={id}
                  type="date"
                  aria-describedby={describedBy}
                  value={inscricao}
                  onChange={(e) => setInscricao(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}
