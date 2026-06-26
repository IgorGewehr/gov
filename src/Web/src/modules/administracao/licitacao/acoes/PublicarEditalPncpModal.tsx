// PublicarEditalNoPncp (Aberta) — divulga o edital/compra no PNCP (art. 54/174).
// L4: o número de controle PNCP NÃO é digitado — a ACL (IPncpGateway) transmite o edital/compra (pré-cadastro
// órgão/unidade/compra) e o backend grava o número de controle devolvido. O formulário coleta o pré-cadastro.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../../../components/ui';
import { ApiError } from '../../../../api/problemDetails';
import { usePublicarEditalPncp } from '../licitacao.api';
import type { PublicarEditalPncpInput } from '../licitacao.api';
import { primeiraMensagem } from './licitacaoModais.shared';
import type { AcaoModalProps } from './licitacaoModais.shared';

const apenasDigitos = (valor: string): string => valor.replace(/\D/g, '');
const anoCorrente = new Date().getFullYear();

export function PublicarEditalPncpModal({ open, onClose, licitacaoId }: AcaoModalProps) {
  const toast = useToast();
  const mutation = usePublicarEditalPncp(licitacaoId);
  const [cnpjOrgao, setCnpjOrgao] = useState('');
  const [codigoUnidade, setCodigoUnidade] = useState('');
  const [anoCompra, setAnoCompra] = useState(String(anoCorrente));
  const [numeroCompra, setNumeroCompra] = useState('');
  const [modalidadeId, setModalidadeId] = useState('');
  const [modoDisputaId, setModoDisputaId] = useState('');
  const [amparoLegalCodigo, setAmparoLegalCodigo] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setCnpjOrgao('');
    setCodigoUnidade('');
    setAnoCompra(String(anoCorrente));
    setNumeroCompra('');
    setModalidadeId('');
    setModoDisputaId('');
    setAmparoLegalCodigo('');
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (cnpjOrgao.length !== 14) {
      setErro('Informe os 14 dígitos do CNPJ do órgão.');
      return;
    }
    if (codigoUnidade.trim() === '') {
      setErro('Informe o código da unidade compradora.');
      return;
    }
    if (numeroCompra.trim() === '') {
      setErro('Informe o número da contratação.');
      return;
    }
    if (Number(modalidadeId) <= 0) {
      setErro('Informe o código da modalidade (tabela PNCP).');
      return;
    }
    if (amparoLegalCodigo.trim() === '') {
      setErro('Informe o código do amparo legal (tabela PNCP).');
      return;
    }
    setErro(undefined);
    const input: PublicarEditalPncpInput = {
      cnpjOrgao,
      codigoUnidade: codigoUnidade.trim(),
      anoCompra: Number(anoCompra),
      numeroCompra: numeroCompra.trim(),
      modalidadeId: Number(modalidadeId),
      modoDisputaId: Number(modoDisputaId) || 0,
      amparoLegalCodigo: amparoLegalCodigo.trim(),
    };
    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Edital divulgado no PNCP.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError) setErro(primeiraMensagem(error, 'cnpjOrgao'));
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível publicar no PNCP.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Publicar edital no PNCP"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-pncp" loading={mutation.isPending}>
            Publicar
          </Button>
        </>
      }
    >
      <form id="form-pncp" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="info" className="mb-3">
          A divulgação do edital no Portal Nacional de Contratações Públicas é condição de regularidade do certame
          (art. 54/174 da NLLC). O <strong>número de controle</strong> é atribuído pelo PNCP e gravado
          automaticamente — não é digitado aqui.
        </Alert>
        {erro ? (
          <Alert variant="danger" className="mb-3">
            {erro}
          </Alert>
        ) : null}
        <FormField label="CNPJ do órgão" required help="14 dígitos, somente números.">
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
        <FormField label="Código da unidade compradora" required>
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
        <FormField label="Ano da contratação" required>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              inputMode="numeric"
              maxLength={4}
              value={anoCompra}
              onChange={(e) => setAnoCompra(apenasDigitos(e.target.value).slice(0, 4))}
            />
          )}
        </FormField>
        <FormField label="Número da contratação" required help='Ex.: "0007/2026".'>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={50}
              value={numeroCompra}
              onChange={(e) => setNumeroCompra(e.target.value)}
            />
          )}
        </FormField>
        <FormField label="Modalidade (PNCP)" required help="Código da tabela de domínio do PNCP.">
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              inputMode="numeric"
              maxLength={3}
              value={modalidadeId}
              onChange={(e) => setModalidadeId(apenasDigitos(e.target.value).slice(0, 3))}
            />
          )}
        </FormField>
        <FormField label="Modo de disputa (PNCP)" help="Código da tabela de domínio do PNCP.">
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              inputMode="numeric"
              maxLength={3}
              value={modoDisputaId}
              onChange={(e) => setModoDisputaId(apenasDigitos(e.target.value).slice(0, 3))}
            />
          )}
        </FormField>
        <FormField label="Amparo legal (PNCP)" required help="Código da tabela de domínio do PNCP.">
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={20}
              value={amparoLegalCodigo}
              onChange={(e) => setAmparoLegalCodigo(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
