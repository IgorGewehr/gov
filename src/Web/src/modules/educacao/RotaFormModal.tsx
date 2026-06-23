// Formulário de CRIAÇÃO de rota de transporte (PNATE) em Modal. Espelha
// CriarRotaCommand (EscolaId, Nome, Turno, Modalidade, VeiculoId, Quilometragem).
// A escola é escolhida via PICKER real (GET /educacao/escolas). O veículo (Frota /
// Patrimônio) é obrigatório na modalidade Próprio e proibido no Terceirizado (I-R2).
import { useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import type { SelectOption } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useCriarRota } from './transporte.api';
import type { CriarRotaInput, ModalidadeTransporte } from './transporte.api';
import type { Turno } from './turma.api';
import { useEscolasDaRede } from './escola.api';
import { opcoesModalidadeTransporte, opcoesTurno } from './educacao.helpers';

export interface RotaFormModalProps {
  open: boolean;
  onClose: () => void;
}

interface FormErrors {
  escolaId?: string;
  nome?: string;
  veiculoId?: string;
  quilometragem?: string;
}

export function RotaFormModal({ open, onClose }: RotaFormModalProps) {
  const toast = useToast();
  const mutation = useCriarRota();
  const escolas = useEscolasDaRede();

  const [escolaId, setEscolaId] = useState('');
  const [nome, setNome] = useState('');
  const [turno, setTurno] = useState<Turno>(1);
  const [modalidade, setModalidade] = useState<ModalidadeTransporte>(0);
  const [veiculoId, setVeiculoId] = useState('');
  const [quilometragem, setQuilometragem] = useState('0');
  const [errors, setErrors] = useState<FormErrors>({});

  const proprio = modalidade === 0;

  const opcoesEscola = useMemo<SelectOption[]>(
    () => (escolas.data ?? []).map((e) => ({ value: e.id, label: `${e.nome} (INEP ${e.codigoInep})` })),
    [escolas.data],
  );

  function reiniciar(): void {
    setEscolaId('');
    setNome('');
    setTurno(1);
    setModalidade(0);
    setVeiculoId('');
    setQuilometragem('0');
    setErrors({});
  }

  function fechar(): void {
    reiniciar();
    onClose();
  }

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (escolaId.trim() === '') next.escolaId = 'Selecione a escola.';
    if (nome.trim() === '') next.nome = 'Nome da rota obrigatório.';
    if (proprio && veiculoId.trim() === '') next.veiculoId = 'Modalidade própria exige o veículo da Frota (I-R2).';
    if (!proprio && veiculoId.trim() !== '') next.veiculoId = 'Terceirizado não admite veículo próprio (I-R2).';
    const km = Number(quilometragem);
    if (!Number.isFinite(km) || km < 0) next.quilometragem = 'Quilometragem não pode ser negativa.';
    return next;
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: CriarRotaInput = {
      escolaId,
      nome: nome.trim(),
      turno,
      modalidade,
      veiculoId: proprio ? veiculoId.trim() : null,
      quilometragem: Number(quilometragem),
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Rota criada (situação Planejada).', 'Sucesso');
        fechar();
      },
      onError: (error) =>
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível criar a rota.'),
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Criar rota de transporte"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-criar-rota" loading={mutation.isPending}>
            Criar rota
          </Button>
        </>
      }
    >
      <form id="form-criar-rota" className="br-form" onSubmit={submeter} noValidate>
        <FormField
          label="Escola atendida"
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

        <FormField label="Nome da rota" required error={errors.nome}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={nome}
              maxLength={120}
              onChange={(e) => setNome(e.target.value)}
              placeholder="Ex.: Linha Rural Norte"
            />
          )}
        </FormField>

        <div className="row">
          <div className="col-12 col-md-6">
            <FormField label="Turno" required>
              {({ id }) => (
                <Select id={id} options={opcoesTurno} value={String(turno)} onChange={(e) => setTurno(Number(e.target.value) as Turno)} />
              )}
            </FormField>
          </div>
          <div className="col-12 col-md-6">
            <FormField label="Modalidade" required>
              {({ id }) => (
                <Select
                  id={id}
                  options={opcoesModalidadeTransporte}
                  value={String(modalidade)}
                  onChange={(e) => {
                    const m = Number(e.target.value) as ModalidadeTransporte;
                    setModalidade(m);
                    if (m !== 0) setVeiculoId('');
                  }}
                />
              )}
            </FormField>
          </div>
        </div>

        <div className="row">
          <div className="col-12 col-md-7">
            <FormField
              label="Veículo da Frota (Id)"
              required={proprio}
              error={errors.veiculoId}
              help={proprio ? 'Veículo de Patrimônio por Id.' : 'Indisponível no terceirizado.'}
            >
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={veiculoId}
                  disabled={!proprio}
                  onChange={(e) => setVeiculoId(e.target.value)}
                  placeholder="GUID do veículo"
                />
              )}
            </FormField>
          </div>
          <div className="col-12 col-md-5">
            <FormField label="Quilometragem (km)" required error={errors.quilometragem}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  step="0.1"
                  min={0}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={quilometragem}
                  onChange={(e) => setQuilometragem(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}
