// Formulário de PLANEJAMENTO de cardápio semanal (PNAE) em Modal. Espelha
// PlanejarCardapioCommand (EscolaId, FaixaEtaria, Semana). A escola é escolhida
// via PICKER real (GET /educacao/escolas) — fim do GUID digitado.
import { useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import type { SelectOption } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { usePlanejarCardapio } from './merenda.api';
import type { FaixaEtariaPnae, PlanejarCardapioInput } from './merenda.api';
import { useEscolasDaRede } from './escola.api';
import { opcoesFaixaEtariaPnae } from './educacao.helpers';

export interface CardapioFormModalProps {
  open: boolean;
  onClose: () => void;
}

interface FormErrors {
  escolaId?: string;
  semana?: string;
}

export function CardapioFormModal({ open, onClose }: CardapioFormModalProps) {
  const toast = useToast();
  const mutation = usePlanejarCardapio();
  const escolas = useEscolasDaRede();

  const [escolaId, setEscolaId] = useState('');
  const [faixaEtaria, setFaixaEtaria] = useState<FaixaEtariaPnae>(2);
  const [semana, setSemana] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  const opcoesEscola = useMemo<SelectOption[]>(
    () => (escolas.data ?? []).map((e) => ({ value: e.id, label: `${e.nome} (INEP ${e.codigoInep})` })),
    [escolas.data],
  );

  function reiniciar(): void {
    setEscolaId('');
    setFaixaEtaria(2);
    setSemana('');
    setErrors({});
  }

  function fechar(): void {
    reiniciar();
    onClose();
  }

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (escolaId.trim() === '') next.escolaId = 'Selecione a escola.';
    if (semana.trim() === '') next.semana = 'Informe a semana (segunda-feira).';
    return next;
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: PlanejarCardapioInput = { escolaId, faixaEtaria, semana };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Cardápio planejado com sucesso.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível planejar o cardápio.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Planejar cardápio semanal"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-planejar-cardapio" loading={mutation.isPending}>
            Planejar cardápio
          </Button>
        </>
      }
    >
      <form id="form-planejar-cardapio" className="br-form" onSubmit={submeter} noValidate>
        <FormField
          label="Escola"
          required
          error={errors.escolaId}
          help={escolas.isLoading ? 'Carregando escolas...' : undefined}
        >
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              options={opcoesEscola}
              placeholder="Selecione a escola"
              value={escolaId}
              onChange={(e) => setEscolaId(e.target.value)}
            />
          )}
        </FormField>

        <div className="row">
          <div className="col-12 col-md-7">
            <FormField label="Faixa etária (PNAE)" required>
              {({ id }) => (
                <Select
                  id={id}
                  options={opcoesFaixaEtariaPnae}
                  value={String(faixaEtaria)}
                  onChange={(e) => setFaixaEtaria(Number(e.target.value) as FaixaEtariaPnae)}
                />
              )}
            </FormField>
          </div>
          <div className="col-12 col-md-5">
            <FormField label="Semana (segunda-feira)" required error={errors.semana}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="date"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={semana}
                  onChange={(e) => setSemana(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}
