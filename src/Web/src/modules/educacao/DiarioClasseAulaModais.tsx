// Modais de AÇÃO do Diário de Classe (parte 2): registrar aula e apurar resultado.
// Complementam DiarioClasseAcaoModais (frequência/nota). Mantém os arquivos < 300 linhas.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, Textarea, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useApurarResultado, useRegistrarAula } from './api';
import type { DiarioAcaoBaseProps } from './diario.acoes.types';

// ---------------------------------------------------------------------------
// REGISTRAR AULA (POST /diarios/{id}/aulas)
// ---------------------------------------------------------------------------

export function RegistrarAulaModal({ open, onClose, matriculaId, diarioId }: DiarioAcaoBaseProps) {
  const toast = useToast();
  const mutation = useRegistrarAula(matriculaId, diarioId);
  const [data, setData] = useState('');
  const [conteudo, setConteudo] = useState('');
  const [diaLetivo, setDiaLetivo] = useState(true);
  const [errors, setErrors] = useState<{ data?: string }>({});

  function fechar(): void {
    setErrors({});
    setData('');
    setConteudo('');
    setDiaLetivo(true);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (data.trim() === '') {
      setErrors({ data: 'Informe a data da aula.' });
      return;
    }
    setErrors({});
    mutation.mutate(
      { data, conteudo: conteudo.trim(), diaLetivo },
      {
        onSuccess: () => {
          toast.success('Aula registrada.', 'Sucesso');
          fechar();
        },
        onError: (error) =>
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível registrar a aula.'),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar aula"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-aula" loading={mutation.isPending}>
            Registrar
          </Button>
        </>
      }
    >
      <form id="form-aula" className="br-form" onSubmit={submeter} noValidate>
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
        <FormField label="Conteúdo ministrado" help="Opcional.">
          {({ id, describedBy }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              rows={3}
              value={conteudo}
              onChange={(e) => setConteudo(e.target.value)}
            />
          )}
        </FormField>
        <div className="br-checkbox mb-3">
          <input
            id="aula-dia-letivo"
            type="checkbox"
            checked={diaLetivo}
            onChange={(e) => setDiaLetivo(e.target.checked)}
          />
          <label htmlFor="aula-dia-letivo">Conta como dia letivo (200 dias)</label>
        </div>
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------
// APURAR RESULTADO (POST /diarios/{id}/apuracao) — terminal
// ---------------------------------------------------------------------------

export function ApurarResultadoModal({ open, onClose, matriculaId, diarioId }: DiarioAcaoBaseProps) {
  const toast = useToast();
  const mutation = useApurarResultado(matriculaId, diarioId);

  function confirmar(event: FormEvent): void {
    event.preventDefault();
    mutation.mutate(undefined, {
      onSuccess: () => {
        toast.success('Resultado apurado.', 'Sucesso');
        onClose();
      },
      onError: (error) =>
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível apurar o resultado.'),
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Apurar resultado"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-apurar" loading={mutation.isPending}>
            Apurar resultado
          </Button>
        </>
      }
    >
      <form id="form-apurar" className="br-form" onSubmit={confirmar} noValidate>
        <Alert variant="warning" title="Atenção:">
          A apuração fecha o diário em Apurado (estado terminal) e calcula o resultado anual. Após a
          apuração não é possível registrar novas aulas, frequências ou notas.
        </Alert>
      </form>
    </Modal>
  );
}
