// Formulário de consolidação de Declaração Fiscal em Modal (foco preso). Demonstra o
// padrão de mutation + validação por campo + linhas contábeis dinâmicas (MSC) + Toast.
// O campo de período exibido depende do tipo: MSC=mês, RREO=bimestre, RGF=quadrimestre, DCA=nenhum.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useConsolidarDeclaracaoFiscal } from './api';
import type {
  ConsolidarDeclaracaoFiscalInput,
  LinhaContabilInput,
  TipoDeclaracaoFiscal,
} from './api';
import { tipoDeclaracaoOptions } from './transparencia.helpers';
import { DeclaracaoLinhasFieldset, linhaVazia } from './DeclaracaoLinhasFieldset';
import type { LinhaForm } from './DeclaracaoLinhasFieldset';

export interface DeclaracaoFiscalFormModalProps {
  open: boolean;
  onClose: () => void;
  exercicioInicial: number;
}

interface FormErrors {
  exercicio?: string;
  periodo?: string;
  linhas?: string;
}

export function DeclaracaoFiscalFormModal({ open, onClose, exercicioInicial }: DeclaracaoFiscalFormModalProps) {
  const toast = useToast();
  const mutation = useConsolidarDeclaracaoFiscal();

  const [tipo, setTipo] = useState<TipoDeclaracaoFiscal>('Msc');
  const [exercicio, setExercicio] = useState(String(exercicioInicial));
  const [periodo, setPeriodo] = useState('1');
  const [linhas, setLinhas] = useState<LinhaForm[]>([linhaVazia()]);
  const [errors, setErrors] = useState<FormErrors>({});

  const periodoConfig = obterPeriodoConfig(tipo);

  function atualizarLinha(index: number, patch: Partial<LinhaForm>): void {
    setLinhas((atuais) => atuais.map((linha, i) => (i === index ? { ...linha, ...patch } : linha)));
  }

  function adicionarLinha(): void {
    setLinhas((atuais) => [...atuais, linhaVazia()]);
  }

  function removerLinha(index: number): void {
    setLinhas((atuais) => (atuais.length > 1 ? atuais.filter((_, i) => i !== index) : atuais));
  }

  function validar(): { errors: FormErrors; linhas: LinhaContabilInput[] } {
    const next: FormErrors = {};
    const ano = Number(exercicio);
    if (exercicio.trim() === '' || !Number.isInteger(ano) || ano < 1900)
      next.exercicio = 'Informe um exercício válido (>= 1900).';

    if (periodoConfig) {
      const numero = Number(periodo);
      if (periodo.trim() === '' || !Number.isInteger(numero) || numero < periodoConfig.min || numero > periodoConfig.max)
        next.periodo = `Informe um valor entre ${periodoConfig.min} e ${periodoConfig.max}.`;
    }

    const linhasValidas: LinhaContabilInput[] = [];
    for (const linha of linhas) {
      const valor = Number(linha.valor);
      if (linha.contaPcasp.trim() === '' || linha.valor.trim() === '' || Number.isNaN(valor) || valor < 0) {
        next.linhas = 'Cada linha exige conta PCASP e valor >= 0.';
        continue;
      }
      linhasValidas.push({
        contaPcasp: linha.contaPcasp.trim(),
        naturezaSaldo: linha.naturezaSaldo,
        valor,
        informacaoComplementar: linha.informacaoComplementar.trim() || null,
      });
    }
    if (linhasValidas.length === 0 && !next.linhas)
      next.linhas = 'Informe ao menos uma linha contábil.';

    return { errors: next, linhas: linhasValidas };
  }

  function fechar(): void {
    setErrors({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const { errors: validacao, linhas: linhasValidas } = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const numero = periodoConfig ? Number(periodo) : null;
    const input: ConsolidarDeclaracaoFiscalInput = {
      tipoDeclaracao: tipo,
      exercicio: Number(exercicio),
      mes: tipo === 'Msc' ? numero : null,
      numeroBimestre: tipo === 'Rreo' ? numero : null,
      numeroQuadrimestre: tipo === 'Rgf' ? numero : null,
      linhas: linhasValidas,
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Declaração consolidada e pronta para transmissão.', 'Sucesso');
        fechar();
      },
      onError: (error) =>
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível consolidar a declaração.'),
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Consolidar declaração fiscal"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-consolidar-declaracao" loading={mutation.isPending}>
            Consolidar
          </Button>
        </>
      }
    >
      <form id="form-consolidar-declaracao" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Tipo de declaração" required>
          {({ id, describedBy }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              options={tipoDeclaracaoOptions}
              value={tipo}
              onChange={(e) => setTipo(e.target.value as TipoDeclaracaoFiscal)}
            />
          )}
        </FormField>

        <div className="row">
          <div className="col-12 col-sm-6">
            <FormField label="Exercício" required error={errors.exercicio}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="1900"
                  inputMode="numeric"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={exercicio}
                  onChange={(e) => setExercicio(e.target.value)}
                />
              )}
            </FormField>
          </div>
          {periodoConfig && (
            <div className="col-12 col-sm-6">
              <FormField
                label={periodoConfig.label}
                required
                help={`Valor entre ${periodoConfig.min} e ${periodoConfig.max}.`}
                error={errors.periodo}
              >
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    type="number"
                    min={periodoConfig.min}
                    max={periodoConfig.max}
                    inputMode="numeric"
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={periodo}
                    onChange={(e) => setPeriodo(e.target.value)}
                  />
                )}
              </FormField>
            </div>
          )}
        </div>

        <DeclaracaoLinhasFieldset
          linhas={linhas}
          erro={errors.linhas}
          onAtualizar={atualizarLinha}
          onAdicionar={adicionarLinha}
          onRemover={removerLinha}
        />
      </form>
    </Modal>
  );
}

/** Define rótulo e faixa do número de período conforme o tipo de declaração. DCA não tem período. */
function obterPeriodoConfig(
  tipo: TipoDeclaracaoFiscal,
): { label: string; min: number; max: number } | null {
  switch (tipo) {
    case 'Msc':
      return { label: 'Mês', min: 1, max: 12 };
    case 'Rreo':
      return { label: 'Bimestre', min: 1, max: 6 };
    case 'Rgf':
      return { label: 'Quadrimestre', min: 1, max: 3 };
    case 'Dca':
    default:
      return null;
  }
}
