// Modais de EVOLUÇÃO clínica do Atendimento: nota SOAP (atendimento editável) e adendo
// assinado (atendimento já assinado — append-only). WIRED a mutations TanStack Query,
// validação por campo e Toast. Ações gated por "saude.gerenciar" na DetailPage.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, Textarea, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useAdicionarAdendo, useAdicionarEvolucao } from './api';
import type { EvolucaoSOAP } from './api';

interface AcaoAtendimentoProps {
  open: boolean;
  onClose: () => void;
  atendimentoId: string;
}

// --- ADICIONAR EVOLUÇÃO SOAP --------------------------------------------------

interface EvolucaoErrors {
  subjetivo?: string;
  objetivo?: string;
  avaliacao?: string;
  plano?: string;
}

export function AdicionarEvolucaoModal({ open, onClose, atendimentoId }: AcaoAtendimentoProps) {
  const toast = useToast();
  const mutation = useAdicionarEvolucao(atendimentoId);
  const [subjetivo, setSubjetivo] = useState('');
  const [objetivo, setObjetivo] = useState('');
  const [avaliacao, setAvaliacao] = useState('');
  const [plano, setPlano] = useState('');
  const [cid, setCid] = useState('');
  const [ciap, setCiap] = useState('');
  const [errors, setErrors] = useState<EvolucaoErrors>({});

  function fechar(): void {
    setErrors({});
    setSubjetivo('');
    setObjetivo('');
    setAvaliacao('');
    setPlano('');
    setCid('');
    setCiap('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: EvolucaoErrors = {};
    if (subjetivo.trim() === '') next.subjetivo = 'Informe o componente Subjetivo.';
    if (objetivo.trim() === '') next.objetivo = 'Informe o componente Objetivo.';
    if (avaliacao.trim() === '') next.avaliacao = 'Informe o componente Avaliação.';
    if (plano.trim() === '') next.plano = 'Informe o componente Plano.';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    mutation.mutate(
      {
        subjetivo: subjetivo.trim(),
        objetivo: objetivo.trim(),
        avaliacao: avaliacao.trim(),
        plano: plano.trim(),
        cid: cid.trim() || null,
        ciap: ciap.trim() || null,
      },
      {
        onSuccess: () => {
          toast.success('Evolução registrada.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível registrar a evolução.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Adicionar evolução (SOAP)"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-evolucao" loading={mutation.isPending}>
            Registrar
          </Button>
        </>
      }
    >
      <form id="form-evolucao" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Subjetivo" required error={errors.subjetivo}>
          {({ id, describedBy, invalid }) => (
            <Textarea id={id} aria-describedby={describedBy} aria-invalid={invalid || undefined} rows={3} value={subjetivo} onChange={(e) => setSubjetivo(e.target.value)} />
          )}
        </FormField>
        <FormField label="Objetivo" required error={errors.objetivo}>
          {({ id, describedBy, invalid }) => (
            <Textarea id={id} aria-describedby={describedBy} aria-invalid={invalid || undefined} rows={3} value={objetivo} onChange={(e) => setObjetivo(e.target.value)} />
          )}
        </FormField>
        <FormField label="Avaliação" required error={errors.avaliacao}>
          {({ id, describedBy, invalid }) => (
            <Textarea id={id} aria-describedby={describedBy} aria-invalid={invalid || undefined} rows={3} value={avaliacao} onChange={(e) => setAvaliacao(e.target.value)} />
          )}
        </FormField>
        <FormField label="Plano" required error={errors.plano}>
          {({ id, describedBy, invalid }) => (
            <Textarea id={id} aria-describedby={describedBy} aria-invalid={invalid || undefined} rows={3} value={plano} onChange={(e) => setPlano(e.target.value)} />
          )}
        </FormField>
        <div className="row">
          <div className="col-sm-6">
            <FormField label="CID-10" help="Opcional.">
              {({ id, describedBy }) => (
                <Input id={id} aria-describedby={describedBy} value={cid} onChange={(e) => setCid(e.target.value)} placeholder="Ex.: J11" />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="CIAP-2" help="Opcional.">
              {({ id, describedBy }) => (
                <Input id={id} aria-describedby={describedBy} value={ciap} onChange={(e) => setCiap(e.target.value)} placeholder="Ex.: R74" />
              )}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}

// --- ADICIONAR ADENDO (assinado, sobre atendimento já assinado) ---------------

interface AdendoModalProps extends AcaoAtendimentoProps {
  /** Evoluções assinadas disponíveis para referência do adendo. */
  evolucoes: EvolucaoSOAP[];
}

interface AdendoErrors {
  evolucaoReferenciadaId?: string;
  texto?: string;
  certificadoIcpBrasil?: string;
  hash?: string;
}

export function AdicionarAdendoModal({ open, onClose, atendimentoId, evolucoes }: AdendoModalProps) {
  const toast = useToast();
  const mutation = useAdicionarAdendo(atendimentoId);
  const [evolucaoReferenciadaId, setEvolucaoReferenciadaId] = useState('');
  const [texto, setTexto] = useState('');
  const [certificado, setCertificado] = useState('');
  const [hash, setHash] = useState('');
  const [errors, setErrors] = useState<AdendoErrors>({});

  const opcoesEvolucao = evolucoes
    .filter((e) => !e.ehAdendo)
    .map((e) => ({ value: e.id, label: `${e.id.slice(0, 8)} — ${e.avaliacao || 'evolução'}` }));

  function fechar(): void {
    setErrors({});
    setEvolucaoReferenciadaId('');
    setTexto('');
    setCertificado('');
    setHash('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: AdendoErrors = {};
    if (evolucaoReferenciadaId.trim() === '') next.evolucaoReferenciadaId = 'Selecione a evolução referenciada.';
    if (texto.trim() === '') next.texto = 'Informe o texto do adendo.';
    if (certificado.trim() === '') next.certificadoIcpBrasil = 'Informe o certificado ICP-Brasil.';
    if (hash.trim() === '') next.hash = 'Informe o hash da assinatura.';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    mutation.mutate(
      {
        evolucaoReferenciadaId: evolucaoReferenciadaId.trim(),
        texto: texto.trim(),
        certificadoIcpBrasil: certificado.trim(),
        hash: hash.trim(),
      },
      {
        onSuccess: () => {
          toast.success('Adendo registrado.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível registrar o adendo.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Adicionar adendo"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-adendo" loading={mutation.isPending}>
            Registrar adendo
          </Button>
        </>
      }
    >
      <form id="form-adendo" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="info" title="Atendimento assinado:">
          Após a assinatura, o registro é imutável. Correções/complementos são feitos por adendo
          assinado, referenciando a evolução original.
        </Alert>
        <FormField label="Evolução referenciada" required error={errors.evolucaoReferenciadaId}>
          {({ id, describedBy, invalid }) => (
            <select
              id={id}
              className="br-select"
              aria-describedby={describedBy}
              aria-invalid={invalid || undefined}
              value={evolucaoReferenciadaId}
              onChange={(e) => setEvolucaoReferenciadaId(e.target.value)}
            >
              <option value="">Selecione…</option>
              {opcoesEvolucao.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </select>
          )}
        </FormField>
        <FormField label="Texto do adendo" required error={errors.texto}>
          {({ id, describedBy, invalid }) => (
            <Textarea id={id} aria-describedby={describedBy} aria-invalid={invalid || undefined} rows={4} value={texto} onChange={(e) => setTexto(e.target.value)} />
          )}
        </FormField>
        <FormField label="Certificado ICP-Brasil" required error={errors.certificadoIcpBrasil}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} aria-describedby={describedBy} invalid={invalid} value={certificado} onChange={(e) => setCertificado(e.target.value)} />
          )}
        </FormField>
        <FormField label="Hash da assinatura" required error={errors.hash}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} aria-describedby={describedBy} invalid={invalid} value={hash} onChange={(e) => setHash(e.target.value)} />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
