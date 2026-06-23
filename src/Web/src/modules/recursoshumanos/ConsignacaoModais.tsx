// Modais das CONSIGNAÇÕES (Onda 2 — Lei 14.131/2021): cadastrar consignatária,
// averbar contrato (mostrando a margem disponível por balde e bloqueando se a parcela
// estoura) e ações de ciclo de vida (suspender/quitar(cancelar)/reativar). Wired a
// mutations TanStack Query, validação por campo, Toast e mapeamento de fieldErrors.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, Select, Textarea, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { formatarMoeda } from '../../i18n/format';
import { MOTIVO_MAX, tratarErroCampos } from './acaoModal.helpers';
import { GRUPOS_MARGEM, TIPOS_CONSIGNATARIA, formatarGrupoMargem } from './consignacao.helpers';
import {
  useAverbarConsignacao,
  useCadastrarConsignataria,
  useCancelarConsignacao,
  useReativarConsignacao,
  useSuspenderConsignacao,
} from './consignacao.api';
import type {
  BaldeMargem,
  ConsignatariaResumo,
  GrupoMargem,
  TipoConsignataria,
} from './consignacao.api';

// ---------------------------------------------------------------------------
// CADASTRAR CONSIGNATÁRIA
// ---------------------------------------------------------------------------

export function CadastrarConsignatariaModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  const toast = useToast();
  const mutation = useCadastrarConsignataria();
  const [cnpj, setCnpj] = useState('');
  const [razaoSocial, setRazaoSocial] = useState('');
  const [tipo, setTipo] = useState('');
  const [errors, setErrors] = useState<{ cnpj?: string; razaoSocial?: string; tipo?: string }>({});

  function fechar(): void {
    setErrors({});
    setCnpj('');
    setRazaoSocial('');
    setTipo('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: typeof errors = {};
    if (cnpj.replace(/\D/g, '').length !== 14) next.cnpj = 'Informe um CNPJ com 14 dígitos.';
    if (razaoSocial.trim() === '') next.razaoSocial = 'Informe a razão social.';
    if (tipo === '') next.tipo = 'Selecione a natureza.';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    mutation.mutate(
      { cnpj: cnpj.replace(/\D/g, ''), razaoSocial: razaoSocial.trim(), tipo: tipo as TipoConsignataria },
      {
        onSuccess: () => {
          toast.success('Consignatária cadastrada.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          setErrors(tratarErroCampos(error, { cnpj: 1, razaoSocial: 1, tipo: 1 }));
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível cadastrar a consignatária.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Cadastrar consignatária"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-cadastrar-consignataria" loading={mutation.isPending}>
            Cadastrar
          </Button>
        </>
      }
    >
      <form id="form-cadastrar-consignataria" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="CNPJ" required error={errors.cnpj} help="Banco/entidade habilitada (14 dígitos).">
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="text"
              inputMode="numeric"
              maxLength={18}
              aria-describedby={describedBy}
              invalid={invalid}
              value={cnpj}
              onChange={(e) => setCnpj(e.target.value)}
            />
          )}
        </FormField>
        <FormField label="Razão social" required error={errors.razaoSocial}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="text"
              maxLength={200}
              aria-describedby={describedBy}
              invalid={invalid}
              value={razaoSocial}
              onChange={(e) => setRazaoSocial(e.target.value)}
            />
          )}
        </FormField>
        <FormField label="Natureza" required error={errors.tipo}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              placeholder="Selecione a natureza"
              options={TIPOS_CONSIGNATARIA}
              value={tipo}
              onChange={(e) => setTipo(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------
// AVERBAR CONSIGNAÇÃO — mostra a margem disponível por balde e bloqueia o estouro
// ---------------------------------------------------------------------------

interface AverbarProps {
  open: boolean;
  onClose: () => void;
  servidorId: string;
  consignatarias: ConsignatariaResumo[];
  baldes: BaldeMargem[];
  /** Competência da margem exibida (AAAA-MM). */
  competencia: string;
}

export function AverbarConsignacaoModal({
  open,
  onClose,
  servidorId,
  consignatarias,
  baldes,
  competencia,
}: AverbarProps) {
  const toast = useToast();
  const mutation = useAverbarConsignacao(servidorId);
  const [consignatariaId, setConsignatariaId] = useState('');
  const [codigoRubrica, setCodigoRubrica] = useState('');
  const [numeroContratoExterno, setNumeroContratoExterno] = useState('');
  const [grupo, setGrupo] = useState<GrupoMargem | ''>('');
  const [valorParcela, setValorParcela] = useState('');
  const [quantidadeParcelas, setQuantidadeParcelas] = useState('');
  const [dataAverbacao, setDataAverbacao] = useState('');
  const [errors, setErrors] = useState<Record<string, string>>({});

  const opcoesConsignataria = consignatarias
    .filter((c) => c.situacao === 'Ativa')
    .map((c) => ({ value: c.id, label: c.razaoSocial }));
  const baldeSelecionado = baldes.find((b) => b.grupo === grupo);
  const parcelaNum = Number(valorParcela.replace(',', '.'));
  // Pré-checagem de margem no cliente (UX). A autoridade é o backend (invariante do agregado).
  const estouraMargem =
    baldeSelecionado !== undefined &&
    Number.isFinite(parcelaNum) &&
    parcelaNum > 0 &&
    parcelaNum > baldeSelecionado.disponivel;

  function fechar(): void {
    setErrors({});
    setConsignatariaId('');
    setCodigoRubrica('');
    setNumeroContratoExterno('');
    setGrupo('');
    setValorParcela('');
    setQuantidadeParcelas('');
    setDataAverbacao('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: Record<string, string> = {};
    if (consignatariaId === '') next.consignatariaId = 'Selecione a consignatária.';
    if (codigoRubrica.trim() === '') next.codigoRubrica = 'Informe o código da rubrica consignável.';
    if (!(parcelaNum > 0)) next.valorParcela = 'Valor da parcela deve ser maior que zero.';
    if (!(Number(quantidadeParcelas) > 0)) next.quantidadeParcelas = 'Quantidade de parcelas deve ser maior que zero.';
    if (dataAverbacao.trim() === '') next.dataAverbacao = 'Informe a data de averbação.';
    if (estouraMargem) {
      next.valorParcela = `Parcela excede a margem disponível do balde (${formatarMoeda(baldeSelecionado!.disponivel)}).`;
    }
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    mutation.mutate(
      {
        servidorId,
        consignatariaId,
        codigoRubrica: codigoRubrica.trim(),
        numeroContratoExterno: numeroContratoExterno.trim() === '' ? null : numeroContratoExterno.trim(),
        valorParcela: parcelaNum,
        quantidadeParcelas: Number(quantidadeParcelas),
        dataAverbacao,
      },
      {
        onSuccess: () => {
          toast.success('Consignação averbada.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          setErrors(
            tratarErroCampos(error, {
              consignatariaId: 1,
              codigoRubrica: 1,
              valorParcela: 1,
              quantidadeParcelas: 1,
              dataAverbacao: 1,
            }),
          );
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível averbar a consignação.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Averbar consignação"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button
            variant="primary"
            type="submit"
            form="form-averbar-consignacao"
            loading={mutation.isPending}
            disabled={estouraMargem}
          >
            Averbar
          </Button>
        </>
      }
    >
      <form id="form-averbar-consignacao" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="info" title={`Margem disponível — competência ${competencia}`}>
          A averbação só é aceita se a parcela couber na margem disponível do balde. A rubrica define
          o balde efetivo no backend; selecione abaixo o balde apenas para conferir o limite.
        </Alert>

        <FormField label="Consignatária" required error={errors.consignatariaId}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              placeholder={opcoesConsignataria.length === 0 ? 'Nenhuma consignatária ativa' : 'Selecione'}
              options={opcoesConsignataria}
              value={consignatariaId}
              onChange={(e) => setConsignatariaId(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Código da rubrica consignável" required error={errors.codigoRubrica}
          help="Código parametrizado (define balde e categoria no backend).">
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="text"
              maxLength={30}
              aria-describedby={describedBy}
              invalid={invalid}
              value={codigoRubrica}
              onChange={(e) => setCodigoRubrica(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Balde de margem (conferência)" help="Apenas para exibir o limite disponível abaixo.">
          {({ id, describedBy }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              placeholder="Selecione o balde"
              options={GRUPOS_MARGEM}
              value={grupo}
              onChange={(e) => setGrupo(e.target.value as GrupoMargem | '')}
            />
          )}
        </FormField>

        {baldeSelecionado && (
          <Alert variant={estouraMargem ? 'danger' : 'success'} title={formatarGrupoMargem(baldeSelecionado.grupo)}>
            Limite {formatarMoeda(baldeSelecionado.limite)} · Comprometido{' '}
            {formatarMoeda(baldeSelecionado.comprometido)} · Disponível{' '}
            <strong>{formatarMoeda(baldeSelecionado.disponivel)}</strong>
            {estouraMargem ? ' — a parcela informada estoura este balde.' : ''}
          </Alert>
        )}

        <FormField label="Valor da parcela mensal" required error={errors.valorParcela}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="0"
              step="0.01"
              aria-describedby={describedBy}
              invalid={invalid}
              value={valorParcela}
              onChange={(e) => setValorParcela(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Quantidade de parcelas" required error={errors.quantidadeParcelas}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="1"
              step="1"
              aria-describedby={describedBy}
              invalid={invalid}
              value={quantidadeParcelas}
              onChange={(e) => setQuantidadeParcelas(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Data de averbação" required error={errors.dataAverbacao}
          help="Define a competência base da margem.">
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="date"
              aria-describedby={describedBy}
              invalid={invalid}
              value={dataAverbacao}
              onChange={(e) => setDataAverbacao(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Número do contrato externo" help="Opcional — referência no sistema da consignatária.">
          {({ id, describedBy }) => (
            <Input
              id={id}
              type="text"
              maxLength={60}
              aria-describedby={describedBy}
              value={numeroContratoExterno}
              onChange={(e) => setNumeroContratoExterno(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------
// AÇÕES DE CICLO DE VIDA — suspender / cancelar (motivo) e reativar (data)
// ---------------------------------------------------------------------------

interface AcaoMotivoProps {
  open: boolean;
  onClose: () => void;
  servidorId: string;
  contratoId: string;
  acao: 'suspender' | 'cancelar';
}

export function AcaoConsignacaoMotivoModal({ open, onClose, servidorId, contratoId, acao }: AcaoMotivoProps) {
  const toast = useToast();
  const suspender = useSuspenderConsignacao(servidorId);
  const cancelar = useCancelarConsignacao(servidorId);
  const mutation = acao === 'suspender' ? suspender : cancelar;
  const [motivo, setMotivo] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  const titulo = acao === 'suspender' ? 'Suspender consignação' : 'Cancelar consignação';
  const verbo = acao === 'suspender' ? 'Suspender' : 'Cancelar';

  function fechar(): void {
    setErro(undefined);
    setMotivo('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (motivo.trim() === '') {
      setErro('Informe o motivo.');
      return;
    }
    setErro(undefined);
    mutation.mutate(
      { contratoId, motivo: motivo.trim() },
      {
        onSuccess: () => {
          toast.success(acao === 'suspender' ? 'Consignação suspensa.' : 'Consignação cancelada.', 'Sucesso');
          fechar();
        },
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : `Não foi possível ${verbo.toLowerCase()} a consignação.`,
          ),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title={titulo}
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Voltar
          </Button>
          <Button
            variant={acao === 'cancelar' ? 'danger' : 'primary'}
            type="submit"
            form="form-acao-consignacao"
            loading={mutation.isPending}
          >
            {verbo}
          </Button>
        </>
      }
    >
      <form id="form-acao-consignacao" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant={acao === 'cancelar' ? 'warning' : 'info'} title="Efeito na margem:">
          {acao === 'suspender'
            ? 'A consignação deixa de ser lançada na folha e libera a margem comprometida. Pode ser reativada.'
            : 'O cancelamento é definitivo (terminal) e libera a margem comprometida.'}
        </Alert>
        <FormField label="Motivo" required error={erro} help={`Máx. ${MOTIVO_MAX} caracteres.`}>
          {({ id, describedBy, invalid }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              aria-invalid={invalid || undefined}
              maxLength={MOTIVO_MAX}
              rows={3}
              value={motivo}
              onChange={(e) => setMotivo(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

interface ReativarProps {
  open: boolean;
  onClose: () => void;
  servidorId: string;
  contratoId: string;
}

export function ReativarConsignacaoModal({ open, onClose, servidorId, contratoId }: ReativarProps) {
  const toast = useToast();
  const mutation = useReativarConsignacao(servidorId);
  const [dataReferencia, setDataReferencia] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setErro(undefined);
    setDataReferencia('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (dataReferencia.trim() === '') {
      setErro('Informe a data de referência.');
      return;
    }
    setErro(undefined);
    mutation.mutate(
      { contratoId, dataReferencia },
      {
        onSuccess: () => {
          toast.success('Consignação reativada.', 'Sucesso');
          fechar();
        },
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível reativar a consignação.',
          ),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Reativar consignação"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Voltar
          </Button>
          <Button variant="primary" type="submit" form="form-reativar-consignacao" loading={mutation.isPending}>
            Reativar
          </Button>
        </>
      }
    >
      <form id="form-reativar-consignacao" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="info" title="Re-checagem de margem:">
          A reativação re-checa a margem disponível do balde na competência da data informada. Se a
          parcela não couber mais, o backend recusa a operação.
        </Alert>
        <FormField label="Data de referência" required error={erro} help="Define a competência da re-checagem.">
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="date"
              aria-describedby={describedBy}
              invalid={invalid}
              value={dataReferencia}
              onChange={(e) => setDataReferencia(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}