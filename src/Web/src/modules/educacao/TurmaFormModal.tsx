// Formulário de CADASTRO de turma em Modal. Espelha CriarTurmaCommand
// (EscolaId, AnoLetivo, Etapa, Serie, Turno, Vagas). A escola é escolhida via PICKER
// real (GET /educacao/escolas) — fim do GUID digitado.
import { useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import type { SelectOption } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useCriarTurma } from './turma.api';
import type { CriarTurmaInput, Etapa, Turno } from './turma.api';
import { useEscolasDaRede } from './escola.api';
import { opcoesEtapa, opcoesTurno } from './educacao.helpers';

export interface TurmaFormModalProps {
  open: boolean;
  onClose: () => void;
}

interface FormErrors {
  escolaId?: string;
  anoLetivo?: string;
  serie?: string;
  vagas?: string;
}

const ANO_ATUAL = new Date().getFullYear();

export function TurmaFormModal({ open, onClose }: TurmaFormModalProps) {
  const toast = useToast();
  const mutation = useCriarTurma();
  const escolas = useEscolasDaRede();

  const [escolaId, setEscolaId] = useState('');
  const [anoLetivo, setAnoLetivo] = useState(String(ANO_ATUAL));
  const [etapa, setEtapa] = useState<Etapa>(2);
  const [serie, setSerie] = useState('');
  const [turno, setTurno] = useState<Turno>(1);
  const [vagas, setVagas] = useState('25');
  const [errors, setErrors] = useState<FormErrors>({});

  const opcoesEscola = useMemo<SelectOption[]>(
    () => (escolas.data ?? []).map((e) => ({ value: e.id, label: `${e.nome} (INEP ${e.codigoInep})` })),
    [escolas.data],
  );

  function reiniciar(): void {
    setEscolaId('');
    setAnoLetivo(String(ANO_ATUAL));
    setEtapa(2);
    setSerie('');
    setTurno(1);
    setVagas('25');
    setErrors({});
  }

  function fechar(): void {
    reiniciar();
    onClose();
  }

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (escolaId.trim() === '') next.escolaId = 'Selecione a escola.';
    const ano = Number(anoLetivo);
    if (!Number.isInteger(ano) || ano < 1900 || ano > 9999) next.anoLetivo = 'Ano letivo inválido.';
    if (serie.trim() === '') next.serie = 'Série/ano obrigatória.';
    const v = Number(vagas);
    if (!Number.isInteger(v) || v <= 0) next.vagas = 'Vagas deve ser maior que zero.';
    return next;
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: CriarTurmaInput = {
      escolaId,
      anoLetivo: Number(anoLetivo),
      etapa,
      serie: serie.trim(),
      turno,
      vagas: Number(vagas),
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Turma criada com sucesso.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível criar a turma.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Cadastrar turma"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-criar-turma" loading={mutation.isPending}>
            Criar turma
          </Button>
        </>
      }
    >
      <form id="form-criar-turma" className="br-form" onSubmit={submeter} noValidate>
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
          <div className="col-12 col-md-4">
            <FormField label="Ano letivo" required error={errors.anoLetivo}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={anoLetivo}
                  onChange={(e) => setAnoLetivo(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-12 col-md-8">
            <FormField label="Etapa/modalidade" required>
              {({ id }) => (
                <Select id={id} options={opcoesEtapa} value={String(etapa)} onChange={(e) => setEtapa(Number(e.target.value) as Etapa)} />
              )}
            </FormField>
          </div>
        </div>

        <div className="row">
          <div className="col-12 col-md-5">
            <FormField label="Série/ano" required error={errors.serie}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={serie}
                  onChange={(e) => setSerie(e.target.value)}
                  placeholder="Ex.: 1º ano"
                />
              )}
            </FormField>
          </div>
          <div className="col-12 col-md-4">
            <FormField label="Turno" required>
              {({ id }) => (
                <Select id={id} options={opcoesTurno} value={String(turno)} onChange={(e) => setTurno(Number(e.target.value) as Turno)} />
              )}
            </FormField>
          </div>
          <div className="col-12 col-md-3">
            <FormField label="Vagas" required error={errors.vagas}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min={1}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={vagas}
                  onChange={(e) => setVagas(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}
