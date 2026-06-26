// PublicarContratoNoPncp — divulgação no PNCP (condição de EFICÁCIA; Lei 14.133/2021, art. 94).
// W9.1/L2: o número de controle PNCP NÃO é digitado — a ACL transmite ao PNCP e o backend grava o número
// devolvido. O formulário coleta os campos obrigatórios do schema "Inserir Contrato/Empenho" (Manual de
// Integração PNCP 2.3.5) que não vivem no agregado.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../../../components/ui';
import { ApiError } from '../../../../api/problemDetails';
import { usePublicarNoPncp } from '../contrato.api';
import type { PublicarContratoNoPncpInput } from '../contrato.api';
import { mapearFieldErrors } from './acaoModals.shared';
import type { AcaoModalProps } from './acaoModals.shared';

type Campos = keyof PublicarContratoNoPncpInput;

const apenasDigitos = (valor: string): string => valor.replace(/\D/g, '');
const anoCorrente = new Date().getFullYear();

export function PublicarNoPncpModal({ contratoId, open, onClose }: AcaoModalProps) {
  const toast = useToast();
  const mutation = usePublicarNoPncp(contratoId);
  const [cnpjOrgao, setCnpjOrgao] = useState('');
  const [codigoUnidade, setCodigoUnidade] = useState('');
  const [numeroContratoInterno, setNumeroContratoInterno] = useState('');
  const [anoContrato, setAnoContrato] = useState(String(anoCorrente));
  const [processo, setProcesso] = useState('');
  const [niFornecedor, setNiFornecedor] = useState('');
  const [nomeRazaoSocialFornecedor, setNomeRazaoSocialFornecedor] = useState('');
  const [tipoContratoId, setTipoContratoId] = useState('1');
  const [categoriaProcessoId, setCategoriaProcessoId] = useState('2');
  const [erros, setErros] = useState<Partial<Record<Campos, string>>>({});

  function fechar(): void {
    setErros({});
    onClose();
  }

  // tipoPessoaFornecedor derivado do número de identificação: 11 díg = CPF (PF), 14 = CNPJ (PJ).
  function tipoPessoa(ni: string): PublicarContratoNoPncpInput['tipoPessoaFornecedor'] {
    return ni.length === 11 ? 'PessoaFisica' : 'PessoaJuridica';
  }

  function validar(input: PublicarContratoNoPncpInput): Partial<Record<Campos, string>> {
    const e: Partial<Record<Campos, string>> = {};
    if (input.cnpjOrgao.length !== 14) e.cnpjOrgao = 'Informe os 14 dígitos do CNPJ do órgão.';
    if (input.codigoUnidade === '') e.codigoUnidade = 'Informe o código da unidade compradora.';
    if (input.numeroContratoInterno === '') e.numeroContratoInterno = 'Informe o número do contrato no ente.';
    if (!Number.isInteger(input.anoContrato) || input.anoContrato < 2021) e.anoContrato = 'Informe o ano do contrato.';
    if (input.processo === '') e.processo = 'Informe o número do processo administrativo.';
    if (input.niFornecedor.length !== 11 && input.niFornecedor.length !== 14)
      e.niFornecedor = 'Informe um CPF (11 dígitos) ou CNPJ (14 dígitos) válido.';
    if (input.nomeRazaoSocialFornecedor === '') e.nomeRazaoSocialFornecedor = 'Informe o nome/razão social do fornecedor.';
    if (!Number.isInteger(input.tipoContratoId) || input.tipoContratoId <= 0)
      e.tipoContratoId = 'Informe o código do tipo de contrato (tabela PNCP).';
    if (!Number.isInteger(input.categoriaProcessoId) || input.categoriaProcessoId <= 0)
      e.categoriaProcessoId = 'Informe o código da categoria do processo (tabela PNCP).';
    return e;
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const input: PublicarContratoNoPncpInput = {
      cnpjOrgao,
      codigoUnidade: codigoUnidade.trim(),
      numeroContratoInterno: numeroContratoInterno.trim(),
      anoContrato: Number(anoContrato),
      processo: processo.trim(),
      niFornecedor,
      tipoPessoaFornecedor: tipoPessoa(niFornecedor),
      nomeRazaoSocialFornecedor: nomeRazaoSocialFornecedor.trim(),
      tipoContratoId: Number(tipoContratoId),
      categoriaProcessoId: Number(categoriaProcessoId),
    };
    const locais = validar(input);
    if (Object.keys(locais).length > 0) {
      setErros(locais);
      return;
    }
    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Contrato divulgado no PNCP — eficácia obtida (art. 94).', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        setErros(
          mapearFieldErrors(error, {
            cnpjOrgao: true,
            codigoUnidade: true,
            numeroContratoInterno: true,
            anoContrato: true,
            processo: true,
            niFornecedor: true,
            nomeRazaoSocialFornecedor: true,
            tipoContratoId: true,
            categoriaProcessoId: true,
          }) as Partial<Record<Campos, string>>,
        );
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível divulgar no PNCP.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Publicar no PNCP"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-publicar-pncp" loading={mutation.isPending}>
            Publicar
          </Button>
        </>
      }
    >
      <form id="form-publicar-pncp" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="info" className="mb-3">
          A divulgação no PNCP é <strong>condição de eficácia</strong> do contrato (Lei 14.133/2021, art. 94).
          O <strong>número de controle PNCP</strong> é atribuído pelo próprio Portal e gravado automaticamente —
          não é digitado aqui.
        </Alert>
        <FormField label="CNPJ do órgão" required error={erros.cnpjOrgao} help="14 dígitos, somente números.">
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              inputMode="numeric"
              maxLength={14}
              value={cnpjOrgao}
              onChange={(e) => setCnpjOrgao(apenasDigitos(e.target.value).slice(0, 14))}
            />
          )}
        </FormField>
        <FormField label="Código da unidade compradora" required error={erros.codigoUnidade}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={20}
              value={codigoUnidade}
              onChange={(e) => setCodigoUnidade(e.target.value)}
            />
          )}
        </FormField>
        <FormField
          label="Número do contrato no ente"
          required
          error={erros.numeroContratoInterno}
          help='Ex.: "0012/2026".'
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={50}
              value={numeroContratoInterno}
              onChange={(e) => setNumeroContratoInterno(e.target.value)}
            />
          )}
        </FormField>
        <FormField label="Ano do contrato" required error={erros.anoContrato}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              inputMode="numeric"
              maxLength={4}
              value={anoContrato}
              onChange={(e) => setAnoContrato(apenasDigitos(e.target.value).slice(0, 4))}
            />
          )}
        </FormField>
        <FormField label="Número do processo administrativo" required error={erros.processo} help='Ex.: "0012/2026".'>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={50}
              value={processo}
              onChange={(e) => setProcesso(e.target.value)}
            />
          )}
        </FormField>
        <FormField
          label="CPF/CNPJ do fornecedor"
          required
          error={erros.niFornecedor}
          help="11 dígitos (CPF) ou 14 (CNPJ), somente números."
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              inputMode="numeric"
              maxLength={14}
              value={niFornecedor}
              onChange={(e) => setNiFornecedor(apenasDigitos(e.target.value).slice(0, 14))}
            />
          )}
        </FormField>
        <FormField label="Nome/razão social do fornecedor" required error={erros.nomeRazaoSocialFornecedor}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={100}
              value={nomeRazaoSocialFornecedor}
              onChange={(e) => setNomeRazaoSocialFornecedor(e.target.value)}
            />
          )}
        </FormField>
        <FormField
          label="Tipo de contrato (PNCP)"
          required
          error={erros.tipoContratoId}
          help="Código da tabela de domínio do PNCP (ex.: 1 = Contrato)."
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              inputMode="numeric"
              maxLength={3}
              value={tipoContratoId}
              onChange={(e) => setTipoContratoId(apenasDigitos(e.target.value).slice(0, 3))}
            />
          )}
        </FormField>
        <FormField
          label="Categoria do processo (PNCP)"
          required
          error={erros.categoriaProcessoId}
          help="Código da tabela de domínio do PNCP."
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              inputMode="numeric"
              maxLength={3}
              value={categoriaProcessoId}
              onChange={(e) => setCategoriaProcessoId(apenasDigitos(e.target.value).slice(0, 3))}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
