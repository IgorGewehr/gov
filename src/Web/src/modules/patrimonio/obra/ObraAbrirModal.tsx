// [Command AbrirObra] Abre uma obra a partir de um contrato NLLC (Lei 14.133/2021).
// Vínculo por contrato + contratada, localização (município/UF/logradouro), regime
// de execução (art. 46), valor contratado (teto da medição — I-1) e data de
// assinatura (base do relógio do art. 94 §3). Validação espelha o backend.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useAbrirObra } from './obra.api';
import type { AbrirObraInput, RegimeExecucaoValor } from './obra.api';
import { OPCOES_REGIME_EXECUCAO, hojeIso } from './obra.helpers';

export interface ObraAbrirModalProps {
  open: boolean;
  onClose: () => void;
  /** Chamado com o Id da obra aberta (navega à ficha). */
  onAberta: (id: string) => void;
}

export function ObraAbrirModal({ open, onClose, onAberta }: ObraAbrirModalProps) {
  const toast = useToast();
  const mutation = useAbrirObra();

  const [contratoId, setContratoId] = useState('');
  const [fornecedorId, setFornecedorId] = useState('');
  const [objeto, setObjeto] = useState('');
  const [municipio, setMunicipio] = useState('');
  const [uf, setUf] = useState('');
  const [logradouro, setLogradouro] = useState('');
  const [regime, setRegime] = useState('');
  const [valor, setValor] = useState('');
  const [dataAssinatura, setDataAssinatura] = useState(hojeIso());
  const [erro, setErro] = useState<string | null>(null);

  function fechar(): void {
    setErro(null);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const valorNum = Number(valor.replace(',', '.'));
    if (contratoId.trim() === '' || fornecedorId.trim() === '') {
      setErro('Informe o contrato de origem e a contratada.');
      return;
    }
    if (objeto.trim() === '' || municipio.trim() === '' || uf.trim().length !== 2) {
      setErro('Informe objeto, município e UF (2 letras).');
      return;
    }
    if (regime === '') {
      setErro('Selecione o regime de execução.');
      return;
    }
    if (!Number.isFinite(valorNum) || valorNum <= 0) {
      setErro('Valor contratado deve ser maior que zero.');
      return;
    }
    if (dataAssinatura.trim() === '') {
      setErro('Informe a data de assinatura do contrato.');
      return;
    }
    setErro(null);

    const input: AbrirObraInput = {
      contratoId: contratoId.trim(),
      fornecedorId: fornecedorId.trim(),
      objeto: objeto.trim(),
      municipio: municipio.trim(),
      uf: uf.trim().toUpperCase(),
      logradouro: logradouro.trim() || null,
      regimeExecucao: Number(regime) as RegimeExecucaoValor,
      valorContratado: valorNum,
      dataAssinaturaContrato: dataAssinatura,
    };

    mutation.mutate(input, {
      onSuccess: (resposta) => {
        toast.success('Obra aberta (planejada).', 'Sucesso');
        fechar();
        onAberta(resposta.id);
      },
      onError: (error) => {
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível abrir a obra.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Abrir obra"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-abrir-obra" loading={mutation.isPending}>
            Abrir obra
          </Button>
        </>
      }
    >
      <form id="form-abrir-obra" className="br-form" onSubmit={submeter} noValidate>
        {erro && (
          <p className="text-danger text-down-01 mb-3" role="alert">
            {erro}
          </p>
        )}
        <div className="row">
          <div className="col-sm-6">
            <FormField label="Contrato de origem (identificador)" required>
              {({ id, describedBy }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  value={contratoId}
                  onChange={(e) => setContratoId(e.target.value)}
                  placeholder="00000000-0000-0000-0000-000000000000"
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="Contratada (identificador)" required>
              {({ id, describedBy }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  value={fornecedorId}
                  onChange={(e) => setFornecedorId(e.target.value)}
                  placeholder="00000000-0000-0000-0000-000000000000"
                />
              )}
            </FormField>
          </div>
        </div>

        <FormField label="Objeto da obra/serviço de engenharia" required>
          {({ id, describedBy }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              value={objeto}
              onChange={(e) => setObjeto(e.target.value)}
              maxLength={500}
              placeholder="Ex.: Construção da UBS do bairro Centro"
            />
          )}
        </FormField>

        <div className="row">
          <div className="col-sm-7">
            <FormField label="Município" required>
              {({ id, describedBy }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  value={municipio}
                  onChange={(e) => setMunicipio(e.target.value)}
                  maxLength={120}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-2">
            <FormField label="UF" required>
              {({ id, describedBy }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  value={uf}
                  onChange={(e) => setUf(e.target.value.toUpperCase())}
                  maxLength={2}
                  placeholder="RS"
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-3">
            <FormField label="Valor contratado (R$)" required>
              {({ id, describedBy }) => (
                <Input
                  id={id}
                  type="text"
                  inputMode="decimal"
                  aria-describedby={describedBy}
                  value={valor}
                  onChange={(e) => setValor(e.target.value)}
                  placeholder="0,00"
                />
              )}
            </FormField>
          </div>
        </div>

        <FormField label="Logradouro/endereço" help="Opcional.">
          {({ id, describedBy }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              value={logradouro}
              onChange={(e) => setLogradouro(e.target.value)}
            />
          )}
        </FormField>

        <div className="row">
          <div className="col-sm-7">
            <FormField label="Regime de execução (art. 46)" required>
              {({ id, describedBy }) => (
                <Select
                  id={id}
                  aria-describedby={describedBy}
                  options={OPCOES_REGIME_EXECUCAO}
                  placeholder="Selecione o regime"
                  value={regime}
                  onChange={(e) => setRegime(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-5">
            <FormField
              label="Data de assinatura do contrato"
              required
              help="Base do relógio do art. 94 §3 (25 dias úteis)."
            >
              {({ id, describedBy }) => (
                <Input
                  id={id}
                  type="date"
                  aria-describedby={describedBy}
                  value={dataAssinatura}
                  onChange={(e) => setDataAssinatura(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}
