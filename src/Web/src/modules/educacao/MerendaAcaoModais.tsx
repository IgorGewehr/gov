// Modais de comando do cardápio (PNAE): adicionar item ao cardápio e registrar a
// distribuição/consumo do dia. Espelham AdicionarItemCardapioPayload e
// RegistrarDistribuicaoMerendaCommand (EducacaoEndpoints.cs).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useAdicionarItemCardapio, useRegistrarDistribuicao } from './merenda.api';
import type {
  AdicionarItemCardapioInput,
  DiaSemanaCardapio,
  RegistrarDistribuicaoInput,
  TipoRefeicao,
} from './merenda.api';
import { opcoesDiaSemanaCardapio, opcoesTipoRefeicao } from './educacao.helpers';

// ---------------------------------------------------------------------------
// Adicionar item (gênero) ao cardápio
// ---------------------------------------------------------------------------

export interface AdicionarItemModalProps {
  open: boolean;
  cardapioId: string;
  onClose: () => void;
}

export function AdicionarItemModal({ open, cardapioId, onClose }: AdicionarItemModalProps) {
  const toast = useToast();
  const mutation = useAdicionarItemCardapio(cardapioId);

  const [dia, setDia] = useState<DiaSemanaCardapio>(1);
  const [refeicao, setRefeicao] = useState<TipoRefeicao>(2);
  const [generoEstoqueId, setGeneroEstoqueId] = useState('');
  const [perCapita, setPerCapita] = useState('');
  const [unidade, setUnidade] = useState('g');
  const [erroGenero, setErroGenero] = useState<string | undefined>();
  const [erroPerCapita, setErroPerCapita] = useState<string | undefined>();

  function reiniciar(): void {
    setDia(1);
    setRefeicao(2);
    setGeneroEstoqueId('');
    setPerCapita('');
    setUnidade('g');
    setErroGenero(undefined);
    setErroPerCapita(undefined);
  }

  function fechar(): void {
    reiniciar();
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const qtd = Number(perCapita);
    const erros: boolean[] = [];
    if (generoEstoqueId.trim() === '') {
      setErroGenero('Informe o gênero do estoque (Id).');
      erros.push(true);
    } else {
      setErroGenero(undefined);
    }
    if (!(qtd > 0)) {
      setErroPerCapita('Per capita deve ser maior que zero.');
      erros.push(true);
    } else {
      setErroPerCapita(undefined);
    }
    if (erros.length > 0) return;

    const input: AdicionarItemCardapioInput = {
      dia,
      refeicao,
      generoEstoqueId: generoEstoqueId.trim(),
      quantidadePerCapita: qtd,
      unidadeMedida: unidade.trim(),
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Item adicionado ao cardápio.', 'Sucesso');
        fechar();
      },
      onError: (error) =>
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível adicionar o item.',
        ),
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Adicionar item ao cardápio"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-add-item" loading={mutation.isPending}>
            Adicionar item
          </Button>
        </>
      }
    >
      <form id="form-add-item" className="br-form" onSubmit={submeter} noValidate>
        <div className="row">
          <div className="col-12 col-md-6">
            <FormField label="Dia da semana" required>
              {({ id }) => (
                <Select
                  id={id}
                  options={opcoesDiaSemanaCardapio}
                  value={String(dia)}
                  onChange={(e) => setDia(Number(e.target.value) as DiaSemanaCardapio)}
                />
              )}
            </FormField>
          </div>
          <div className="col-12 col-md-6">
            <FormField label="Refeição" required>
              {({ id }) => (
                <Select
                  id={id}
                  options={opcoesTipoRefeicao}
                  value={String(refeicao)}
                  onChange={(e) => setRefeicao(Number(e.target.value) as TipoRefeicao)}
                />
              )}
            </FormField>
          </div>
        </div>

        <FormField label="Gênero do estoque (Id)" required error={erroGenero} help="Item do almoxarifado (Patrimônio) por Id.">
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={generoEstoqueId}
              onChange={(e) => setGeneroEstoqueId(e.target.value)}
              placeholder="GUID do gênero"
            />
          )}
        </FormField>

        <div className="row">
          <div className="col-12 col-md-7">
            <FormField label="Quantidade per capita" required error={erroPerCapita}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  step="0.001"
                  min={0}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={perCapita}
                  onChange={(e) => setPerCapita(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-12 col-md-5">
            <FormField label="Unidade de medida" required>
              {({ id }) => (
                <Input id={id} value={unidade} onChange={(e) => setUnidade(e.target.value)} placeholder="g, ml, un" />
              )}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------
// Registrar distribuição/consumo do dia
// ---------------------------------------------------------------------------

export interface DistribuicaoModalProps {
  open: boolean;
  cardapioId: string;
  onClose: () => void;
}

export function DistribuicaoModal({ open, cardapioId, onClose }: DistribuicaoModalProps) {
  const toast = useToast();
  const mutation = useRegistrarDistribuicao();

  const [data, setData] = useState('');
  const [dia, setDia] = useState<DiaSemanaCardapio>(1);
  const [refeicao, setRefeicao] = useState<TipoRefeicao>(2);
  const [comensais, setComensais] = useState('');
  const [erroData, setErroData] = useState<string | undefined>();
  const [erroComensais, setErroComensais] = useState<string | undefined>();

  function reiniciar(): void {
    setData('');
    setDia(1);
    setRefeicao(2);
    setComensais('');
    setErroData(undefined);
    setErroComensais(undefined);
  }

  function fechar(): void {
    reiniciar();
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const n = Number(comensais);
    const erros: boolean[] = [];
    if (data.trim() === '') {
      setErroData('Informe a data da distribuição.');
      erros.push(true);
    } else {
      setErroData(undefined);
    }
    if (!Number.isInteger(n) || n <= 0) {
      setErroComensais('Comensais deve ser um inteiro positivo.');
      erros.push(true);
    } else {
      setErroComensais(undefined);
    }
    if (erros.length > 0) return;

    const input: RegistrarDistribuicaoInput = { cardapioId, data, dia, refeicao, comensais: n };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Distribuição registrada (consumo calculado pelo cardápio).', 'Sucesso');
        fechar();
      },
      onError: (error) =>
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível registrar a distribuição.',
        ),
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar distribuição/consumo"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-distribuicao" loading={mutation.isPending}>
            Registrar
          </Button>
        </>
      }
    >
      <form id="form-distribuicao" className="br-form" onSubmit={submeter} noValidate>
        <div className="row">
          <div className="col-12 col-md-6">
            <FormField label="Data da distribuição" required error={erroData}>
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
          <div className="col-12 col-md-6">
            <FormField label="Nº de comensais" required error={erroComensais}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min={1}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={comensais}
                  onChange={(e) => setComensais(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>
        <div className="row">
          <div className="col-12 col-md-6">
            <FormField label="Dia da semana" required>
              {({ id }) => (
                <Select
                  id={id}
                  options={opcoesDiaSemanaCardapio}
                  value={String(dia)}
                  onChange={(e) => setDia(Number(e.target.value) as DiaSemanaCardapio)}
                />
              )}
            </FormField>
          </div>
          <div className="col-12 col-md-6">
            <FormField label="Refeição" required>
              {({ id }) => (
                <Select
                  id={id}
                  options={opcoesTipoRefeicao}
                  value={String(refeicao)}
                  onChange={(e) => setRefeicao(Number(e.target.value) as TipoRefeicao)}
                />
              )}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}
