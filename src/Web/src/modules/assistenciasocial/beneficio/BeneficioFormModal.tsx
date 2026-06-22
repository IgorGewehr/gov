// Formulario de AVALIACAO DE ELEGIBILIDADE em Modal (command AvaliarElegibilidadeBeneficio).
// Padrao-ouro: useMutation + validacao por campo (FormField/aria-describedby) + Toast.
// A decisao (Concedida/Indeferida) e do dominio (criterio vigente na competencia); o
// formulario apenas coleta os dados faticos do requerente (DadosElegibilidadeDto) e o valor.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, Select, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useAvaliarElegibilidade } from './beneficio.api';
import type { AvaliarElegibilidadeInput, TipoBeneficio } from './beneficio.api';
import { TIPO_OPTIONS, MES_OPTIONS, tipoLabel } from './beneficio.helpers';
import { CondicoesRequerenteFieldset } from './CondicoesRequerenteFieldset';
import type { CondicoesRequerente } from './CondicoesRequerenteFieldset';

export interface BeneficioFormModalProps {
  open: boolean;
  onClose: () => void;
  /** Pre-preenche a familia quando aberto a partir de uma consulta. */
  familiaIdInicial?: string;
  /** Callback apos sucesso (ex.: id do beneficio criado). */
  onAvaliado?: (beneficioId: string) => void;
}

interface FormErrors {
  familiaId?: string;
  tipo?: string;
  ano?: string;
  mes?: string;
  idade?: string;
  valor?: string;
}

const hoje = new Date();

export function BeneficioFormModal({
  open,
  onClose,
  familiaIdInicial = '',
  onAvaliado,
}: BeneficioFormModalProps) {
  const toast = useToast();
  const mutation = useAvaliarElegibilidade();

  const [familiaId, setFamiliaId] = useState(familiaIdInicial);
  const [tipo, setTipo] = useState<TipoBeneficio | ''>('');
  const [ano, setAno] = useState(String(hoje.getFullYear()));
  const [mes, setMes] = useState(String(hoje.getMonth() + 1));
  const [idade, setIdade] = useState('');
  const [condicoes, setCondicoes] = useState<CondicoesRequerente>({
    possuiDeficiencia: false,
    possuiAvaliacao: false,
    acumulaSeguridade: false,
    inscritoCadUnico: false,
  });
  const [valor, setValor] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  // Cesta basica e provisao em especie: Valor pode ser nulo (I-9).
  const ehEventual = tipo === 'Eventual';

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (familiaId.trim() === '') next.familiaId = 'Informe o identificador da família.';
    if (tipo === '') next.tipo = 'Selecione o tipo de benefício.';
    const anoNum = Number(ano);
    if (ano.trim() === '' || Number.isNaN(anoNum) || anoNum < 1900 || anoNum > 9999)
      next.ano = 'Informe um ano válido.';
    const mesNum = Number(mes);
    if (mes.trim() === '' || Number.isNaN(mesNum) || mesNum < 1 || mesNum > 12)
      next.mes = 'Selecione um mês válido.';
    const idadeNum = Number(idade);
    if (idade.trim() === '' || Number.isNaN(idadeNum) || idadeNum < 0 || idadeNum > 130)
      next.idade = 'Informe uma idade válida.';
    if (valor.trim() !== '') {
      const valorNum = Number(valor);
      if (Number.isNaN(valorNum) || valorNum < 0)
        next.valor = 'O valor, quando informado, não pode ser negativo.';
    }
    return next;
  }

  function resetar(): void {
    setErrors({});
  }

  function fechar(): void {
    resetar();
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0 || tipo === '') return;

    const input: AvaliarElegibilidadeInput = {
      familiaId: familiaId.trim(),
      tipo,
      competencia: { ano: Number(ano), mes: Number(mes) },
      dados: {
        idade: Number(idade),
        possuiDeficiencia: condicoes.possuiDeficiencia,
        possuiAvaliacaoBiopsicossocial: condicoes.possuiAvaliacao,
        acumulaSeguridadeSocial: condicoes.acumulaSeguridade,
        inscritoCadUnico: condicoes.inscritoCadUnico,
      },
      valor: valor.trim() === '' ? null : Number(valor),
    };

    mutation.mutate(input, {
      onSuccess: (resposta) => {
        toast.success(
          `Elegibilidade de ${tipoLabel(input.tipo)} avaliada. A decisão segue o critério vigente na competência.`,
          'Avaliação registrada',
        );
        onAvaliado?.(resposta.id);
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const mapped: FormErrors = {};
          const conhecidos: Record<string, keyof FormErrors> = {
            familiaId: 'familiaId',
            tipo: 'tipo',
            competencia: 'mes',
            dados: 'idade',
            valor: 'valor',
          };
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const key = field.charAt(0).toLowerCase() + field.slice(1);
            const alvo = conhecidos[key];
            if (alvo) mapped[alvo] = messages[0];
          }
          setErrors(mapped);
        }
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível avaliar a elegibilidade.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Avaliar elegibilidade de benefício"
      size="large"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-avaliar-elegibilidade" loading={mutation.isPending}>
            Avaliar
          </Button>
        </>
      }
    >
      <form id="form-avaliar-elegibilidade" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="info">
          A concessão ou o indeferimento é decidido pelo critério legal vigente na competência
          (renda per capita, idade, deficiência e não acumulação). Informe os dados fáticos do requerente.
        </Alert>

        <FormField label="Identificador da família" required error={errors.familiaId}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={familiaId}
              onChange={(e) => setFamiliaId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>

        <FormField label="Tipo de benefício" required error={errors.tipo}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              placeholder="Selecione…"
              options={TIPO_OPTIONS}
              value={tipo}
              onChange={(e) => setTipo(e.target.value as TipoBeneficio)}
            />
          )}
        </FormField>

        <div className="row">
          <div className="col-sm-6">
            <FormField label="Ano da competência" required error={errors.ano}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="1900"
                  max="9999"
                  step="1"
                  inputMode="numeric"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={ano}
                  onChange={(e) => setAno(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="Mês da competência" required error={errors.mes}>
              {({ id, describedBy, invalid }) => (
                <Select
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  options={MES_OPTIONS}
                  value={mes}
                  onChange={(e) => setMes(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>

        <FormField
          label="Idade do requerente (anos)"
          required
          error={errors.idade}
          help="Critério BPC idoso: idade ≥ 65 anos."
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="0"
              max="130"
              step="1"
              inputMode="numeric"
              aria-describedby={describedBy}
              invalid={invalid}
              value={idade}
              onChange={(e) => setIdade(e.target.value)}
            />
          )}
        </FormField>

        <CondicoesRequerenteFieldset
          valores={condicoes}
          onChange={(parcial) => setCondicoes((atual) => ({ ...atual, ...parcial }))}
        />

        <FormField
          label="Valor a conceder (R$)"
          error={errors.valor}
          help={
            ehEventual
              ? 'Opcional. Cesta básica é provisão em espécie — deixe vazio.'
              : 'Opcional. Informe quando elegível; deixe vazio se o valor for definido pela regra.'
          }
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="0"
              step="0.01"
              inputMode="decimal"
              aria-describedby={describedBy}
              invalid={invalid}
              value={valor}
              onChange={(e) => setValor(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
