// Modais de ação das INSPEÇÕES/AUTOS da VISA: abrir inspeção, registrar item do checklist,
// concluir inspeção (resultado derivado) e lavrar auto (intimação/infração). Padrão mutation +
// validação por campo + Toast + ProblemDetails. Gating é feito nas páginas que os abrem.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, Textarea, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { EstabelecimentoVisaPicker } from './EstabelecimentoVisaPicker';
import { useAbrirInspecao, useConcluirInspecao, useRegistrarItem } from './vigilancia.api';
import type { AbrirInspecaoInput, ConformidadeItem } from './vigilancia.api';
import { opcoesConformidade, resultadoLabel } from './vigilancia.helpers';

function hoje(): string {
  return new Date().toISOString().slice(0, 10);
}

// ---------- Abrir inspeção ----------

export function AbrirInspecaoModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  const toast = useToast();
  const abrir = useAbrirInspecao();
  const [estabelecimentoId, setEstabelecimentoId] = useState('');
  const [dataInspecao, setDataInspecao] = useState(hoje());
  const [roteiro, setRoteiro] = useState('');
  const [erroEstab, setErroEstab] = useState<string | undefined>();

  function fechar(): void {
    setEstabelecimentoId('');
    setRoteiro('');
    setErroEstab(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (estabelecimentoId === '') {
      setErroEstab('Selecione o estabelecimento.');
      return;
    }
    const input: AbrirInspecaoInput = {
      estabelecimentoId,
      dataInspecao,
      roteiro: roteiro.trim() || null,
    };
    abrir.mutate(input, {
      onSuccess: () => {
        toast.success('Inspeção aberta com sucesso.', 'Sucesso');
        fechar();
      },
      onError: (error: unknown) =>
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível abrir a inspeção.'),
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Abrir inspeção sanitária"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={abrir.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-abrir-inspecao" loading={abrir.isPending}>
            Abrir inspeção
          </Button>
        </>
      }
    >
      <form id="form-abrir-inspecao" className="br-form" onSubmit={submeter} noValidate>
        <EstabelecimentoVisaPicker
          label="Estabelecimento"
          required
          value={estabelecimentoId}
          onChange={(id) => {
            setEstabelecimentoId(id);
            if (id) setErroEstab(undefined);
          }}
          error={erroEstab}
        />
        <FormField label="Data da vistoria" required>
          {({ id }) => (
            <Input id={id} type="date" value={dataInspecao} onChange={(e) => setDataInspecao(e.target.value)} />
          )}
        </FormField>
        <FormField label="Roteiro/checklist" help="Identificação do roteiro aplicado (opcional).">
          {({ id, describedBy }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              maxLength={120}
              value={roteiro}
              onChange={(e) => setRoteiro(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

// ---------- Registrar item do checklist ----------

export function RegistrarItemModal({
  open,
  onClose,
  inspecaoId,
}: {
  open: boolean;
  onClose: () => void;
  inspecaoId: string;
}) {
  const toast = useToast();
  const registrar = useRegistrarItem(inspecaoId);
  const [requisito, setRequisito] = useState('');
  const [conformidade, setConformidade] = useState<ConformidadeItem>('Conforme');
  const [observacao, setObservacao] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setRequisito('');
    setConformidade('Conforme');
    setObservacao('');
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (requisito.trim() === '') {
      setErro('Informe o requisito sanitário.');
      return;
    }
    registrar.mutate(
      { requisito: requisito.trim(), conformidade, observacao: observacao.trim() || null },
      {
        onSuccess: () => {
          toast.success('Item registrado.', 'Sucesso');
          fechar();
        },
        onError: (error: unknown) =>
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível registrar o item.'),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar item do checklist"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={registrar.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-item-inspecao" loading={registrar.isPending}>
            Registrar item
          </Button>
        </>
      }
    >
      <form id="form-item-inspecao" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Requisito sanitário" required error={erro}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={200}
              value={requisito}
              onChange={(e) => setRequisito(e.target.value)}
            />
          )}
        </FormField>
        <FormField label="Conformidade" required>
          {({ id }) => (
            <Select
              id={id}
              options={opcoesConformidade}
              value={conformidade}
              onChange={(e) => setConformidade(e.target.value as ConformidadeItem)}
            />
          )}
        </FormField>
        <FormField label="Observação">
          {({ id }) => (
            <Textarea id={id} rows={3} maxLength={1000} value={observacao} onChange={(e) => setObservacao(e.target.value)} />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

// ---------- Concluir inspeção ----------

export function ConcluirInspecaoModal({
  open,
  onClose,
  inspecaoId,
}: {
  open: boolean;
  onClose: () => void;
  inspecaoId: string;
}) {
  const toast = useToast();
  const concluir = useConcluirInspecao(inspecaoId);
  const [houveInfracaoGrave, setHouveInfracaoGrave] = useState(false);

  function fechar(): void {
    setHouveInfracaoGrave(false);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    concluir.mutate(houveInfracaoGrave, {
      onSuccess: (resultado) => {
        toast.success(`Inspeção concluída: ${resultadoLabel[resultado]}.`, 'Sucesso');
        fechar();
      },
      onError: (error: unknown) =>
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível concluir a inspeção.'),
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Concluir inspeção"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={concluir.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-concluir-inspecao" loading={concluir.isPending}>
            Concluir
          </Button>
        </>
      }
    >
      <form id="form-concluir-inspecao" className="br-form" onSubmit={submeter} noValidate>
        <p className="text-secondary">
          O resultado é derivado dos itens: sem não conformidades, Aprovado; com pendências sanáveis,
          Aprovado com pendências. Marque abaixo se houve infração grave (resulta em Reprovado).
        </p>
        <div className="br-checkbox">
          <input
            id="chk-infracao-grave"
            type="checkbox"
            checked={houveInfracaoGrave}
            onChange={(e) => setHouveInfracaoGrave(e.target.checked)}
          />
          <label htmlFor="chk-infracao-grave">Houve infração grave</label>
        </div>
      </form>
    </Modal>
  );
}
