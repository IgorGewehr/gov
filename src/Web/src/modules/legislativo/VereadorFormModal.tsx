// Formulario de cadastro/edicao de Vereador em Modal. Cobre nome civil, nome
// parlamentar, partido, legislatura, cargo na Mesa e situacao. Mutation +
// validacao por campo + Toast. Edita quando `vereador` e informado.
import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { CARGOS_MESA, SITUACOES_VEREADOR } from './legislativo.shared';
import {
  useAtualizarVereador,
  useCriarVereador,
  type VereadorDetalhe,
  type VereadorInput,
} from './vereadores.api';

export interface VereadorFormModalProps {
  open: boolean;
  onClose: () => void;
  /** Quando informado, o modal entra em modo edicao. */
  vereador?: VereadorDetalhe;
}

interface FormErrors {
  nomeCivil?: string;
  nomeParlamentar?: string;
  partido?: string;
  legislatura?: string;
}

const CAMPOS: ReadonlyArray<keyof FormErrors> = [
  'nomeCivil',
  'nomeParlamentar',
  'partido',
  'legislatura',
];

export function VereadorFormModal({ open, onClose, vereador }: VereadorFormModalProps) {
  const toast = useToast();
  const edicao = vereador !== undefined;
  const criar = useCriarVereador();
  const atualizar = useAtualizarVereador(vereador?.id ?? '');
  const mutation = edicao ? atualizar : criar;

  const [nomeCivil, setNomeCivil] = useState('');
  const [nomeParlamentar, setNomeParlamentar] = useState('');
  const [partido, setPartido] = useState('');
  const [legislatura, setLegislatura] = useState('');
  const [cargoMesa, setCargoMesa] = useState<string>(String(CARGOS_MESA[0].value));
  const [situacao, setSituacao] = useState<string>(String(SITUACOES_VEREADOR[0].value));
  const [errors, setErrors] = useState<FormErrors>({});

  // Sincroniza os campos quando o modal abre (edicao carrega o vereador alvo).
  useEffect(() => {
    if (!open) return;
    setNomeCivil(vereador?.nomeCivil ?? '');
    setNomeParlamentar(vereador?.nomeParlamentar ?? '');
    setPartido(vereador?.partido ?? '');
    setLegislatura(vereador?.legislatura ?? '');
    setCargoMesa(String(CARGOS_MESA[0].value));
    setSituacao(String(SITUACOES_VEREADOR[0].value));
    setErrors({});
  }, [open, vereador]);

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (nomeCivil.trim() === '') next.nomeCivil = 'Informe o nome civil.';
    if (nomeParlamentar.trim() === '') next.nomeParlamentar = 'Informe o nome parlamentar.';
    if (partido.trim() === '') next.partido = 'Informe o partido.';
    if (legislatura.trim() === '') next.legislatura = 'Informe a legislatura (ex.: 2021-2024).';
    return next;
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: VereadorInput = {
      nomeCivil: nomeCivil.trim(),
      nomeParlamentar: nomeParlamentar.trim(),
      partido: partido.trim(),
      legislatura: legislatura.trim(),
      cargoMesa: Number(cargoMesa),
      situacao: Number(situacao),
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success(edicao ? 'Vereador atualizado.' : 'Vereador cadastrado.', 'Sucesso');
        onClose();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const mapped: FormErrors = {};
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const key = (field.charAt(0).toLowerCase() + field.slice(1)) as keyof FormErrors;
            if (CAMPOS.includes(key)) mapped[key] = messages[0];
          }
          setErrors(mapped);
        }
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível salvar o vereador.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={edicao ? 'Editar vereador' : 'Cadastrar vereador'}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-vereador" loading={mutation.isPending}>
            {edicao ? 'Salvar' : 'Cadastrar'}
          </Button>
        </>
      }
    >
      <form id="form-vereador" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Nome civil" required error={errors.nomeCivil}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={nomeCivil}
              onChange={(e) => setNomeCivil(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Nome parlamentar" required error={errors.nomeParlamentar}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={nomeParlamentar}
              onChange={(e) => setNomeParlamentar(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Partido" required error={errors.partido}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={partido}
              onChange={(e) => setPartido(e.target.value)}
              placeholder="Sigla (ex.: PT, PSDB, MDB)"
            />
          )}
        </FormField>

        <FormField
          label="Legislatura"
          required
          error={errors.legislatura}
          help="Período do mandato (ex.: 2021-2024)."
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={legislatura}
              onChange={(e) => setLegislatura(e.target.value)}
              placeholder="2021-2024"
            />
          )}
        </FormField>

        <FormField label="Cargo na Mesa">
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={cargoMesa}
              onChange={(e) => setCargoMesa(e.target.value)}
              options={CARGOS_MESA.map((c) => ({ value: String(c.value), label: c.label }))}
            />
          )}
        </FormField>

        <FormField label="Situação">
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={situacao}
              onChange={(e) => setSituacao(e.target.value)}
              options={SITUACOES_VEREADOR.map((s) => ({ value: String(s.value), label: s.label }))}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
