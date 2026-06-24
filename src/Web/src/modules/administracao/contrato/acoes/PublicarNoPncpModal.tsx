// PublicarContratoNoPncp — divulgação no PNCP (condição de EFICÁCIA; Lei 14.133/2021, art. 94).
// W9.1: o número de controle PNCP NÃO é digitado — a ACL transmite ao PNCP e o backend grava o número
// devolvido. O formulário coleta apenas os dados de identificação da transmissão.
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

export function PublicarNoPncpModal({ contratoId, open, onClose }: AcaoModalProps) {
  const toast = useToast();
  const mutation = usePublicarNoPncp(contratoId);
  const [cnpjOrgao, setCnpjOrgao] = useState('');
  const [codigoUnidade, setCodigoUnidade] = useState('');
  const [numeroContratoInterno, setNumeroContratoInterno] = useState('');
  const [documentoFornecedor, setDocumentoFornecedor] = useState('');
  const [erros, setErros] = useState<Partial<Record<Campos, string>>>({});

  function fechar(): void {
    setErros({});
    onClose();
  }

  function validar(input: PublicarContratoNoPncpInput): Partial<Record<Campos, string>> {
    const e: Partial<Record<Campos, string>> = {};
    if (input.cnpjOrgao.length !== 14) e.cnpjOrgao = 'Informe os 14 dígitos do CNPJ do órgão.';
    if (input.codigoUnidade === '') e.codigoUnidade = 'Informe o código da unidade compradora.';
    if (input.numeroContratoInterno === '') e.numeroContratoInterno = 'Informe o número do contrato no ente.';
    if (input.documentoFornecedor.length !== 11 && input.documentoFornecedor.length !== 14)
      e.documentoFornecedor = 'Informe um CPF (11 dígitos) ou CNPJ (14 dígitos) válido.';
    return e;
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const input: PublicarContratoNoPncpInput = {
      cnpjOrgao,
      codigoUnidade: codigoUnidade.trim(),
      numeroContratoInterno: numeroContratoInterno.trim(),
      documentoFornecedor,
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
            documentoFornecedor: true,
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
              maxLength={30}
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
              maxLength={30}
              value={numeroContratoInterno}
              onChange={(e) => setNumeroContratoInterno(e.target.value)}
            />
          )}
        </FormField>
        <FormField
          label="CPF/CNPJ do fornecedor"
          required
          error={erros.documentoFornecedor}
          help="11 dígitos (CPF) ou 14 (CNPJ), somente números."
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              inputMode="numeric"
              maxLength={14}
              value={documentoFornecedor}
              onChange={(e) => setDocumentoFornecedor(apenasDigitos(e.target.value).slice(0, 14))}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
