// Geração da remessa bancária CNAB240 (FEBRABAN) de uma ordem de pagamento. Os dados do convênio/DVs
// vêm do acordo bancário do ente (não há no cadastro genérico) e são informados aqui. A transmissão
// real ao banco é diferida (M10) — aqui produz-se e baixa-se o arquivo .rem.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { gerarCnab240 } from './cnab.api';

export interface CnabRemessaModalProps {
  open: boolean;
  onClose: () => void;
  ordemId: string;
}

const FORMAS = [
  { value: '1', label: 'Crédito em conta corrente' },
  { value: '3', label: 'DOC/TED' },
  { value: '5', label: 'Crédito em poupança' },
  { value: '45', label: 'PIX transferência' },
];

const INSCRICOES = [
  { value: '2', label: 'CNPJ' },
  { value: '1', label: 'CPF' },
];

export function CnabRemessaModal({ open, onClose, ordemId }: CnabRemessaModalProps) {
  const toast = useToast();
  const [gerando, setGerando] = useState(false);
  const [codigoBanco, setCodigoBanco] = useState('');
  const [tipoInscricao, setTipoInscricao] = useState('2');
  const [numeroInscricao, setNumeroInscricao] = useState('');
  const [convenio, setConvenio] = useState('');
  const [dvAgencia, setDvAgencia] = useState('');
  const [dvConta, setDvConta] = useState('');
  const [dvAgenciaConta, setDvAgenciaConta] = useState('');
  const [nomeEmpresa, setNomeEmpresa] = useState('');
  const [formaLancamento, setFormaLancamento] = useState('1');
  const [sequencial, setSequencial] = useState('1');

  async function enviar(event: FormEvent): Promise<void> {
    event.preventDefault();
    setGerando(true);
    try {
      await gerarCnab240(ordemId, {
        codigoBanco,
        tipoInscricao: Number(tipoInscricao),
        numeroInscricao,
        convenio,
        dvAgencia,
        dvConta,
        dvAgenciaConta,
        nomeEmpresa,
        formaLancamento: Number(formaLancamento),
        sequencialArquivo: Number(sequencial) || 1,
      });
      toast.success('Remessa CNAB240 gerada.', 'Sucesso');
      onClose();
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Não foi possível gerar a remessa CNAB240.');
    } finally {
      setGerando(false);
    }
  }

  return (
    <Modal open={open} onClose={onClose} title="Gerar remessa CNAB240 (FEBRABAN)">
      <form className="br-form" onSubmit={enviar}>
        <div className="row">
          <div className="col-sm-6">
            <FormField label="Código do banco (COMPE)" required>
              {({ id }) => <Input id={id} value={codigoBanco} onChange={(e) => setCodigoBanco(e.target.value)} placeholder="001" />}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="Forma de lançamento" required>
              {({ id }) => (
                <Select id={id} value={formaLancamento} onChange={(e) => setFormaLancamento(e.target.value)} options={FORMAS} />
              )}
            </FormField>
          </div>
        </div>

        <FormField label="Nome do ente pagador" required>
          {({ id }) => <Input id={id} value={nomeEmpresa} onChange={(e) => setNomeEmpresa(e.target.value)} />}
        </FormField>

        <div className="row">
          <div className="col-sm-4">
            <FormField label="Tipo de inscrição" required>
              {({ id }) => (
                <Select id={id} value={tipoInscricao} onChange={(e) => setTipoInscricao(e.target.value)} options={INSCRICOES} />
              )}
            </FormField>
          </div>
          <div className="col-sm-8">
            <FormField label="CNPJ/CPF do ente" required>
              {({ id }) => <Input id={id} value={numeroInscricao} onChange={(e) => setNumeroInscricao(e.target.value)} />}
            </FormField>
          </div>
        </div>

        <FormField label="Código do convênio" required>
          {({ id }) => <Input id={id} value={convenio} onChange={(e) => setConvenio(e.target.value)} />}
        </FormField>

        <div className="row">
          <div className="col-sm-4">
            <FormField label="DV agência">
              {({ id }) => <Input id={id} value={dvAgencia} onChange={(e) => setDvAgencia(e.target.value)} maxLength={1} />}
            </FormField>
          </div>
          <div className="col-sm-4">
            <FormField label="DV conta">
              {({ id }) => <Input id={id} value={dvConta} onChange={(e) => setDvConta(e.target.value)} maxLength={1} />}
            </FormField>
          </div>
          <div className="col-sm-4">
            <FormField label="DV ag./conta">
              {({ id }) => <Input id={id} value={dvAgenciaConta} onChange={(e) => setDvAgenciaConta(e.target.value)} maxLength={1} />}
            </FormField>
          </div>
        </div>

        <FormField label="Sequencial do arquivo">
          {({ id }) => <Input id={id} type="number" min="1" value={sequencial} onChange={(e) => setSequencial(e.target.value)} />}
        </FormField>

        <div className="d-flex justify-content-end mt-3" style={{ gap: '0.5rem' }}>
          <Button type="button" variant="secondary" onClick={onClose} disabled={gerando}>
            Cancelar
          </Button>
          <Button type="submit" variant="primary" loading={gerando}>
            Gerar e baixar
          </Button>
        </div>
      </form>
    </Modal>
  );
}
