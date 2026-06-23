// Modais da IMUNIZAÇÃO (SI-PNI): cadastro de imunobiológico (PNI) e registro de dose na
// carteira do paciente (calcula o aprazamento da próxima dose). Espelham
// CadastrarImunobiologicoCommand e AplicarDosePayload (ver SaudeEndpointsFarmaciaImunizacao.cs).
import { useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import {
  Alert,
  Button,
  FormField,
  Input,
  Modal,
  Select,
  useToast,
} from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useAplicarDose, useCadastrarImunobiologico, useImunobiologicos } from './api';
import { opcoesTipoDose } from './saude.helpers';
import { ProfissionalPicker } from './AgendamentoPickers';
import { EstabelecimentoPicker } from './EstabelecimentoPicker';

interface ModalBaseProps {
  open: boolean;
  onClose: () => void;
}

/** Modal de cadastro de imunobiológico no catálogo (PNI). */
export function CadastrarImunobiologicoModal({ open, onClose }: ModalBaseProps) {
  const toast = useToast();
  const mutation = useCadastrarImunobiologico();

  const [nome, setNome] = useState('');
  const [sigla, setSigla] = useState('');
  const [totalDoses, setTotalDoses] = useState('1');
  const [intervalo, setIntervalo] = useState('0');
  const [doseUnica, setDoseUnica] = useState(false);
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setNome('');
    setSigla('');
    setTotalDoses('1');
    setIntervalo('0');
    setDoseUnica(false);
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (nome.trim() === '' || sigla.trim() === '' || Number(totalDoses) < 1) {
      setErro('Informe nome, sigla e total de doses (≥ 1).');
      return;
    }
    setErro(undefined);
    mutation.mutate(
      {
        nome: nome.trim(),
        sigla: sigla.trim(),
        totalDoses: Number(totalDoses),
        intervaloDiasProximaDose: Number(intervalo) || 0,
        doseUnica,
      },
      {
        onSuccess: () => {
          toast.success('Imunobiológico cadastrado.', 'Sucesso');
          fechar();
        },
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível cadastrar o imunobiológico.',
          ),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Cadastrar imunobiológico (PNI)"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-imuno" loading={mutation.isPending}>
            Cadastrar
          </Button>
        </>
      }
    >
      <form id="form-imuno" className="br-form" onSubmit={submeter} noValidate>
        <div className="row">
          <div className="col-12 col-md-8">
            <FormField label="Nome" required error={erro}>
              {({ id }) => <Input id={id} value={nome} onChange={(e) => setNome(e.target.value)} />}
            </FormField>
          </div>
          <div className="col-12 col-md-4">
            <FormField label="Sigla" required>
              {({ id }) => <Input id={id} value={sigla} onChange={(e) => setSigla(e.target.value)} />}
            </FormField>
          </div>
        </div>
        <div className="row">
          <div className="col-6">
            <FormField label="Total de doses" required>
              {({ id }) => (
                <Input id={id} type="number" min="1" step="1" value={totalDoses} onChange={(e) => setTotalDoses(e.target.value)} />
              )}
            </FormField>
          </div>
          <div className="col-6">
            <FormField label="Intervalo p/ próxima (dias)">
              {({ id }) => (
                <Input id={id} type="number" min="0" step="1" value={intervalo} onChange={(e) => setIntervalo(e.target.value)} />
              )}
            </FormField>
          </div>
        </div>
        <div className="br-checkbox">
          <input id="imuno-dose-unica" type="checkbox" checked={doseUnica} onChange={(e) => setDoseUnica(e.target.checked)} />
          <label htmlFor="imuno-dose-unica">Esquema de dose única</label>
        </div>
      </form>
    </Modal>
  );
}

interface AplicarDoseModalProps extends ModalBaseProps {
  pacienteId: string;
}

/** Modal de registro de dose na carteira do paciente (mostra aprazamento da próxima). */
export function AplicarDoseModal({ open, onClose, pacienteId }: AplicarDoseModalProps) {
  const toast = useToast();
  const mutation = useAplicarDose();
  const catalogo = useImunobiologicos(open);

  const [imunobiologicoId, setImunobiologicoId] = useState('');
  const [tipoDose, setTipoDose] = useState('1');
  const [numeroDose, setNumeroDose] = useState('1');
  const [lote, setLote] = useState('');
  const [aplicadorId, setAplicadorId] = useState('');
  const [dataAplicacao, setDataAplicacao] = useState('');
  const [estabelecimentoId, setEstabelecimentoId] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  const opcoesImuno = useMemo(
    () => (catalogo.data ?? []).map((i) => ({ value: i.id, label: `${i.sigla} — ${i.nome}` })),
    [catalogo.data],
  );

  function fechar(): void {
    setImunobiologicoId('');
    setTipoDose('1');
    setNumeroDose('1');
    setLote('');
    setAplicadorId('');
    setDataAplicacao('');
    setEstabelecimentoId('');
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (imunobiologicoId === '' || lote.trim() === '' || aplicadorId === '' || dataAplicacao === '') {
      setErro('Informe imunobiológico, lote, aplicador e data de aplicação.');
      return;
    }
    setErro(undefined);
    mutation.mutate(
      {
        pacienteId,
        payload: {
          imunobiologicoId,
          tipoDose: Number(tipoDose),
          numeroDose: Number(numeroDose) || 1,
          lote: lote.trim(),
          aplicadorId,
          dataAplicacao,
          estabelecimentoId: estabelecimentoId || null,
        },
      },
      {
        onSuccess: () => {
          toast.success('Dose registrada (aprazamento calculado).', 'Sucesso');
          fechar();
        },
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível registrar a dose.',
          ),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar dose na carteira"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-dose" loading={mutation.isPending}>
            Registrar dose
          </Button>
        </>
      }
    >
      <form id="form-dose" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="info">
          O aprazamento da próxima dose é calculado pelo intervalo do esquema. Acesso registrado em
          trilha de auditoria (LGPD).
        </Alert>
        <FormField label="Imunobiológico" required error={erro}>
          {({ id }) => (
            <Select
              id={id}
              options={opcoesImuno}
              placeholder={catalogo.isLoading ? 'Carregando…' : 'Selecione'}
              value={imunobiologicoId}
              onChange={(e) => setImunobiologicoId(e.target.value)}
            />
          )}
        </FormField>
        <div className="row">
          <div className="col-6">
            <FormField label="Tipo de dose" required>
              {({ id }) => (
                <Select id={id} options={opcoesTipoDose} value={tipoDose} onChange={(e) => setTipoDose(e.target.value)} />
              )}
            </FormField>
          </div>
          <div className="col-6">
            <FormField label="Nº da dose" required>
              {({ id }) => (
                <Input id={id} type="number" min="1" step="1" value={numeroDose} onChange={(e) => setNumeroDose(e.target.value)} />
              )}
            </FormField>
          </div>
        </div>
        <div className="row">
          <div className="col-12 col-md-6">
            <FormField label="Lote" required>
              {({ id }) => <Input id={id} value={lote} onChange={(e) => setLote(e.target.value)} />}
            </FormField>
          </div>
          <div className="col-12 col-md-6">
            <FormField label="Data de aplicação" required>
              {({ id }) => (
                <Input id={id} type="date" value={dataAplicacao} onChange={(e) => setDataAplicacao(e.target.value)} />
              )}
            </FormField>
          </div>
        </div>
        <ProfissionalPicker label="Aplicador" value={aplicadorId} onChange={setAplicadorId} />
        <EstabelecimentoPicker
          label="Estabelecimento (opcional — habilita baixa de estoque)"
          value={estabelecimentoId}
          onChange={setEstabelecimentoId}
        />
      </form>
    </Modal>
  );
}
