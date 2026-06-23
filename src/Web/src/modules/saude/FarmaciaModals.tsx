// Modais da FARMÁCIA: cadastro de medicamento (REMUME), entrada de lote no estoque de
// uma UBS e estorno de dispensação. Espelham CadastrarMedicamentoCommand,
// EntradaEstoquePayload e EstornarDispensacaoCommand (ver SaudeEndpointsFarmaciaImunizacao.cs).
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Button,
  FormField,
  Input,
  Modal,
  Select,
  Textarea,
  useToast,
} from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useCadastrarMedicamento, useRegistrarEntrada, useEstornarDispensacao } from './api';
import type { MedicamentoItemLista } from './api';
import {
  opcoesControleSngpc,
  opcoesFormaFarmaceutica,
  opcoesUnidadeMedida,
} from './saude.helpers';
import { EstabelecimentoPicker } from './EstabelecimentoPicker';

interface ModalBaseProps {
  open: boolean;
  onClose: () => void;
}

/** Modal de cadastro de medicamento no catálogo (REMUME). */
export function CadastrarMedicamentoModal({ open, onClose }: ModalBaseProps) {
  const toast = useToast();
  const mutation = useCadastrarMedicamento();

  const [principioAtivo, setPrincipioAtivo] = useState('');
  const [apresentacao, setApresentacao] = useState('');
  const [concentracao, setConcentracao] = useState('');
  const [forma, setForma] = useState('1');
  const [unidade, setUnidade] = useState('1');
  const [controle, setControle] = useState('0');
  const [codigoCatmat, setCodigoCatmat] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setPrincipioAtivo('');
    setApresentacao('');
    setConcentracao('');
    setForma('1');
    setUnidade('1');
    setControle('0');
    setCodigoCatmat('');
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (principioAtivo.trim() === '' || apresentacao.trim() === '' || concentracao.trim() === '') {
      setErro('Preencha princípio ativo, apresentação e concentração.');
      return;
    }
    setErro(undefined);
    mutation.mutate(
      {
        principioAtivo: principioAtivo.trim(),
        apresentacao: apresentacao.trim(),
        concentracao: concentracao.trim(),
        forma: Number(forma),
        unidade: Number(unidade),
        controle: Number(controle),
        codigoCatmat: codigoCatmat.trim() || null,
      },
      {
        onSuccess: () => {
          toast.success('Medicamento cadastrado no catálogo.', 'Sucesso');
          fechar();
        },
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível cadastrar o medicamento.',
          ),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Cadastrar medicamento (REMUME)"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-medicamento" loading={mutation.isPending}>
            Cadastrar
          </Button>
        </>
      }
    >
      <form id="form-medicamento" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Princípio ativo (DCB/DCI)" required error={erro}>
          {({ id }) => (
            <Input id={id} value={principioAtivo} onChange={(e) => setPrincipioAtivo(e.target.value)} />
          )}
        </FormField>
        <FormField label="Apresentação" required>
          {({ id }) => (
            <Input id={id} value={apresentacao} onChange={(e) => setApresentacao(e.target.value)} />
          )}
        </FormField>
        <FormField label="Concentração / dosagem" required>
          {({ id }) => (
            <Input id={id} value={concentracao} onChange={(e) => setConcentracao(e.target.value)} />
          )}
        </FormField>
        <div className="row">
          <div className="col-12 col-md-6">
            <FormField label="Forma farmacêutica" required>
              {({ id }) => (
                <Select id={id} options={opcoesFormaFarmaceutica} value={forma} onChange={(e) => setForma(e.target.value)} />
              )}
            </FormField>
          </div>
          <div className="col-12 col-md-6">
            <FormField label="Unidade de medida" required>
              {({ id }) => (
                <Select id={id} options={opcoesUnidadeMedida} value={unidade} onChange={(e) => setUnidade(e.target.value)} />
              )}
            </FormField>
          </div>
        </div>
        <FormField label="Controle especial (Portaria 344/1998)" required>
          {({ id }) => (
            <Select id={id} options={opcoesControleSngpc} value={controle} onChange={(e) => setControle(e.target.value)} />
          )}
        </FormField>
        <FormField label="Código CATMAT (opcional)">
          {({ id }) => (
            <Input id={id} value={codigoCatmat} onChange={(e) => setCodigoCatmat(e.target.value)} />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

interface EntradaModalProps extends ModalBaseProps {
  medicamento: MedicamentoItemLista | null;
}

/** Modal de entrada de lote (com validade) no estoque de uma UBS. */
export function EntradaEstoqueModal({ open, onClose, medicamento }: EntradaModalProps) {
  const toast = useToast();
  const mutation = useRegistrarEntrada();

  const [estabId, setEstabId] = useState('');
  const [numeroLote, setNumeroLote] = useState('');
  const [validade, setValidade] = useState('');
  const [quantidade, setQuantidade] = useState('');
  const [ponto, setPonto] = useState('0');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setEstabId('');
    setNumeroLote('');
    setValidade('');
    setQuantidade('');
    setPonto('0');
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (!medicamento) return;
    if (estabId === '' || numeroLote.trim() === '' || validade === '' || Number(quantidade) <= 0) {
      setErro('Informe estabelecimento, lote, validade e quantidade (> 0).');
      return;
    }
    setErro(undefined);
    mutation.mutate(
      {
        estabId,
        medId: medicamento.id,
        payload: {
          numeroLote: numeroLote.trim(),
          validade,
          quantidade: Number(quantidade),
          pontoDeRessuprimento: Number(ponto) || 0,
        },
      },
      {
        onSuccess: () => {
          toast.success('Entrada de lote registrada.', 'Sucesso');
          fechar();
        },
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível registrar a entrada.',
          ),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Entrada de lote no estoque"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-entrada" loading={mutation.isPending}>
            Registrar entrada
          </Button>
        </>
      }
    >
      <form id="form-entrada" className="br-form" onSubmit={submeter} noValidate>
        <p className="text-secondary">
          Medicamento: <strong>{medicamento?.principioAtivo}</strong> — {medicamento?.apresentacao}
        </p>
        <EstabelecimentoPicker
          label="Estabelecimento (UBS/farmácia)"
          required
          value={estabId}
          onChange={setEstabId}
          error={erro}
        />
        <div className="row">
          <div className="col-12 col-md-6">
            <FormField label="Número do lote" required>
              {({ id }) => (
                <Input id={id} value={numeroLote} onChange={(e) => setNumeroLote(e.target.value)} />
              )}
            </FormField>
          </div>
          <div className="col-12 col-md-6">
            <FormField label="Validade" required>
              {({ id }) => (
                <Input id={id} type="date" value={validade} onChange={(e) => setValidade(e.target.value)} />
              )}
            </FormField>
          </div>
        </div>
        <div className="row">
          <div className="col-6">
            <FormField label="Quantidade" required>
              {({ id }) => (
                <Input id={id} type="number" min="0" step="0.01" value={quantidade} onChange={(e) => setQuantidade(e.target.value)} />
              )}
            </FormField>
          </div>
          <div className="col-6">
            <FormField label="Ponto de ressuprimento">
              {({ id }) => (
                <Input id={id} type="number" min="0" step="1" value={ponto} onChange={(e) => setPonto(e.target.value)} />
              )}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}

interface EstornoModalProps extends ModalBaseProps {
  dispensacaoId: string;
}

/** Modal de estorno de dispensação (devolve o saldo aos lotes). */
export function EstornarDispensacaoModal({ open, onClose, dispensacaoId }: EstornoModalProps) {
  const toast = useToast();
  const mutation = useEstornarDispensacao();
  const [motivo, setMotivo] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setMotivo('');
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (motivo.trim() === '') {
      setErro('Informe o motivo do estorno.');
      return;
    }
    setErro(undefined);
    mutation.mutate(
      { dispensacaoId, motivo: motivo.trim() },
      {
        onSuccess: () => {
          toast.success('Dispensação estornada (saldo devolvido).', 'Sucesso');
          fechar();
        },
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível estornar a dispensação.',
          ),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Estornar dispensação"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-estorno" loading={mutation.isPending}>
            Estornar
          </Button>
        </>
      }
    >
      <form id="form-estorno" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Motivo do estorno" required error={erro}>
          {({ id }) => (
            <Textarea id={id} rows={3} value={motivo} onChange={(e) => setMotivo(e.target.value)} />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
