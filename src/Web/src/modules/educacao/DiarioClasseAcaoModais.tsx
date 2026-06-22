// Modais de AÇÃO do agregado Diário de Classe (parte 1), abertos da DiarioClasseDetailPage:
//   - Registrar frequência (presença/falta + carga horária) -> POST .../frequencias
//   - Lançar nota (componente + período + valor 0..10)       -> POST .../notas
// Registrar aula e apurar resultado ficam em DiarioClasseAulaModais (re-exportados
// abaixo) para manter os arquivos < 300 linhas. Cada modal é WIRED a uma mutation.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useLancarNota, useRegistrarFrequencia } from './api';
import type { DiarioAcaoBaseProps as AcaoBaseProps } from './diario.acoes.types';

export { ApurarResultadoModal, RegistrarAulaModal } from './DiarioClasseAulaModais';

// ---------------------------------------------------------------------------
// REGISTRAR FREQUÊNCIA
// ---------------------------------------------------------------------------

export function RegistrarFrequenciaModal({ open, onClose, matriculaId, diarioId }: AcaoBaseProps) {
  const toast = useToast();
  const mutation = useRegistrarFrequencia(matriculaId, diarioId);
  const [data, setData] = useState('');
  const [presente, setPresente] = useState('true');
  const [cargaHorariaAula, setCargaHorariaAula] = useState('1');
  const [errors, setErrors] = useState<{ data?: string; cargaHorariaAula?: string }>({});

  function fechar(): void {
    setErrors({});
    setData('');
    setPresente('true');
    setCargaHorariaAula('1');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: { data?: string; cargaHorariaAula?: string } = {};
    if (data.trim() === '') next.data = 'Informe a data.';
    const carga = Number(cargaHorariaAula);
    if (!Number.isInteger(carga) || carga <= 0)
      next.cargaHorariaAula = 'Informe a carga horária da aula (maior que zero).';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    mutation.mutate(
      { data, presente: presente === 'true', cargaHorariaAula: carga },
      {
        onSuccess: () => {
          toast.success('Frequência registrada.', 'Sucesso');
          fechar();
        },
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível registrar a frequência.',
          ),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar frequência"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-frequencia" loading={mutation.isPending}>
            Registrar
          </Button>
        </>
      }
    >
      <form id="form-frequencia" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Data" required error={errors.data}>
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
        <FormField label="Presença" required>
          {({ id, describedBy }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              options={[
                { value: 'true', label: 'Presente' },
                { value: 'false', label: 'Ausente' },
              ]}
              value={presente}
              onChange={(e) => setPresente(e.target.value)}
            />
          )}
        </FormField>
        <FormField label="Carga horária da aula (h)" required error={errors.cargaHorariaAula}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="1"
              step="1"
              inputMode="numeric"
              aria-describedby={describedBy}
              invalid={invalid}
              value={cargaHorariaAula}
              onChange={(e) => setCargaHorariaAula(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------
// LANÇAR NOTA
// ---------------------------------------------------------------------------

export function LancarNotaModal({ open, onClose, matriculaId, diarioId }: AcaoBaseProps) {
  const toast = useToast();
  const mutation = useLancarNota(matriculaId, diarioId);
  const [componenteCurricularId, setComponente] = useState('');
  const [periodo, setPeriodo] = useState('');
  const [valor, setValor] = useState('');
  const [errors, setErrors] = useState<{ componenteCurricularId?: string; periodo?: string; valor?: string }>({});

  function fechar(): void {
    setErrors({});
    setComponente('');
    setPeriodo('');
    setValor('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: { componenteCurricularId?: string; periodo?: string; valor?: string } = {};
    if (componenteCurricularId.trim() === '') next.componenteCurricularId = 'Informe o componente curricular.';
    if (periodo.trim() === '') next.periodo = 'Informe o período.';
    const nota = Number(valor);
    if (valor.trim() === '' || Number.isNaN(nota) || nota < 0 || nota > 10)
      next.valor = 'A nota deve estar entre 0 e 10.';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    mutation.mutate(
      { componenteCurricularId: componenteCurricularId.trim(), periodo: periodo.trim(), valor: nota },
      {
        onSuccess: () => {
          toast.success('Nota lançada.', 'Sucesso');
          fechar();
        },
        onError: (error) =>
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível lançar a nota.'),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Lançar nota"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-nota" loading={mutation.isPending}>
            Lançar
          </Button>
        </>
      }
    >
      <form id="form-nota" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Componente curricular (identificador)" required error={errors.componenteCurricularId}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={componenteCurricularId}
              onChange={(e) => setComponente(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>
        <div className="row">
          <div className="col-sm-6">
            <FormField label="Período" required error={errors.periodo}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  maxLength={20}
                  value={periodo}
                  onChange={(e) => setPeriodo(e.target.value)}
                  placeholder="1º bimestre"
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="Valor (0 a 10)" required error={errors.valor}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="0"
                  max="10"
                  step="any"
                  inputMode="decimal"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={valor}
                  onChange={(e) => setValor(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}
