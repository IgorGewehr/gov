// Formulário de CRIAÇÃO de cargo em Modal (foco preso). Padrão-ouro:
// mutation + validação por campo + Toast + mapeamento de ProblemDetails.errors.
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Button,
  FormField,
  Input,
  Modal,
  Select,
  useToast,
} from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useCriarCargo } from './api';
import type { CriarCargoInput } from './api';
import { TIPOS_CARGO } from './recursosHumanos.helpers';

export interface CriarCargoFormModalProps {
  open: boolean;
  onClose: () => void;
}

interface FormErrors {
  denominacao?: string;
  tipo?: string;
  vencimento?: string;
  quantidadeVagas?: string;
  leiCriacao?: string;
  inscricaoEstabelecimento?: string;
  denominacaoUnidade?: string;
}

const CAMPOS: Record<keyof FormErrors, true> = {
  denominacao: true,
  tipo: true,
  vencimento: true,
  quantidadeVagas: true,
  leiCriacao: true,
  inscricaoEstabelecimento: true,
  denominacaoUnidade: true,
};

export function CriarCargoFormModal({ open, onClose }: CriarCargoFormModalProps) {
  const toast = useToast();
  const mutation = useCriarCargo();

  const [denominacao, setDenominacao] = useState('');
  const [tipo, setTipo] = useState('');
  const [vencimento, setVencimento] = useState('');
  const [quantidadeVagas, setQuantidadeVagas] = useState('');
  const [leiCriacao, setLeiCriacao] = useState('');
  const [inscricaoEstabelecimento, setInscricaoEstabelecimento] = useState('');
  const [denominacaoUnidade, setDenominacaoUnidade] = useState('');
  const [codigoLotacaoTributaria, setCodigoLotacaoTributaria] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (denominacao.trim() === '') next.denominacao = 'Informe a denominação do cargo.';
    if (tipo.trim() === '') next.tipo = 'Selecione o tipo do cargo.';
    const venc = Number(vencimento);
    if (vencimento.trim() === '' || Number.isNaN(venc) || venc <= 0)
      next.vencimento = 'Informe um vencimento maior que zero.';
    const vagas = Number(quantidadeVagas);
    if (quantidadeVagas.trim() === '' || !Number.isInteger(vagas) || vagas < 1)
      next.quantidadeVagas = 'Informe ao menos 1 vaga.';
    if (leiCriacao.trim() === '') next.leiCriacao = 'Informe a lei de criação.';
    if (inscricaoEstabelecimento.trim() === '')
      next.inscricaoEstabelecimento = 'Informe a inscrição do estabelecimento.';
    if (denominacaoUnidade.trim() === '')
      next.denominacaoUnidade = 'Informe a denominação da unidade de lotação.';
    return next;
  }

  function fechar(): void {
    setErrors({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: CriarCargoInput = {
      denominacao: denominacao.trim(),
      tipo: Number(tipo),
      vencimento: Number(vencimento),
      quantidadeVagas: Number(quantidadeVagas),
      leiCriacao: leiCriacao.trim(),
      lotacao: {
        inscricaoEstabelecimento: inscricaoEstabelecimento.trim(),
        denominacaoUnidade: denominacaoUnidade.trim(),
        codigoLotacaoTributaria: codigoLotacaoTributaria.trim() || null,
      },
      planoDeCargosId: null,
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success(`Cargo "${input.denominacao}" criado.`, 'Sucesso');
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const mapped: FormErrors = {};
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const key = (field.charAt(0).toLowerCase() + field.slice(1)) as keyof FormErrors;
            if (key in CAMPOS) mapped[key] = messages[0];
          }
          setErrors(mapped);
        }
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível criar o cargo.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Criar cargo"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button
            variant="primary"
            type="submit"
            form="form-criar-cargo"
            loading={mutation.isPending}
          >
            Criar
          </Button>
        </>
      }
    >
      <form id="form-criar-cargo" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Denominação do cargo" required error={errors.denominacao}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={denominacao}
              onChange={(e) => setDenominacao(e.target.value)}
              maxLength={120}
            />
          )}
        </FormField>

        <FormField label="Tipo do cargo" required error={errors.tipo}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={tipo}
              onChange={(e) => setTipo(e.target.value)}
              placeholder="Selecione o tipo"
              options={TIPOS_CARGO}
            />
          )}
        </FormField>

        <FormField label="Vencimento-base (R$)" required error={errors.vencimento}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="0"
              step="0.01"
              inputMode="decimal"
              aria-describedby={describedBy}
              invalid={invalid}
              value={vencimento}
              onChange={(e) => setVencimento(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Quantidade de vagas" required error={errors.quantidadeVagas}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="1"
              step="1"
              inputMode="numeric"
              aria-describedby={describedBy}
              invalid={invalid}
              value={quantidadeVagas}
              onChange={(e) => setQuantidadeVagas(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Lei de criação" required error={errors.leiCriacao}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={leiCriacao}
              onChange={(e) => setLeiCriacao(e.target.value)}
              maxLength={80}
              placeholder="Lei nº 0.000/0000"
            />
          )}
        </FormField>

        <FormField
          label="Inscrição do estabelecimento"
          required
          error={errors.inscricaoEstabelecimento}
          help="Estabelecimento de exercício (eSocial S-1005)."
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={inscricaoEstabelecimento}
              onChange={(e) => setInscricaoEstabelecimento(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Unidade de lotação" required error={errors.denominacaoUnidade}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={denominacaoUnidade}
              onChange={(e) => setDenominacaoUnidade(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Código de lotação tributária" help="Opcional (eSocial S-1020).">
          {({ id, describedBy }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              value={codigoLotacaoTributaria}
              onChange={(e) => setCodigoLotacaoTributaria(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
