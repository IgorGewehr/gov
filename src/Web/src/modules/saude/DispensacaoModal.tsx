// Modal de DISPENSAÇÃO ao paciente: seleciona paciente + profissional + estabelecimento,
// adiciona itens (medicamento + quantidade + posologia) e dispensa. A baixa segue FEFO
// (lote não vencido, primeiro a vencer) — informativo exibido ao operador. Espelha
// DispensarMedicamentoCommand(PacienteId, EstabelecimentoId, ProfissionalId, PrescricaoId?, Itens[]).
import { useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import {
  Alert,
  Button,
  FormField,
  Input,
  Modal,
  Spinner,
  Tag,
  useToast,
} from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useBuscarMedicamentos, useDispensar } from './api';
import type { DispensarMedicamentoInput, ItemDispensacaoInput, MedicamentoItemLista } from './api';
import { PacientePicker, ProfissionalPicker } from './AgendamentoPickers';
import { EstabelecimentoPicker } from './EstabelecimentoPicker';

export interface DispensacaoModalProps {
  open: boolean;
  onClose: () => void;
}

interface ItemEditavel extends ItemDispensacaoInput {
  rotulo: string;
}

interface FormErrors {
  pacienteId?: string;
  estabelecimentoId?: string;
  profissionalId?: string;
  itens?: string;
}

export function DispensacaoModal({ open, onClose }: DispensacaoModalProps) {
  const toast = useToast();
  const mutation = useDispensar();

  const [pacienteId, setPacienteId] = useState('');
  const [profissionalId, setProfissionalId] = useState('');
  const [estabelecimentoId, setEstabelecimentoId] = useState('');
  const [itens, setItens] = useState<ItemEditavel[]>([]);
  const [errors, setErrors] = useState<FormErrors>({});

  // Seleção de medicamento (busca no catálogo) + quantidade/posologia.
  const [termoMed, setTermoMed] = useState('');
  const [medSelecionado, setMedSelecionado] = useState<MedicamentoItemLista | null>(null);
  const [quantidade, setQuantidade] = useState('');
  const [posologia, setPosologia] = useState('');

  const filtroMed = useMemo(
    () => ({ termo: termoMed.trim() || undefined, apenasAtivos: true, pagina: 1, tamanho: 8 }),
    [termoMed],
  );
  const habilitadoMed = open && termoMed.trim().length >= 2 && medSelecionado === null;
  const queryMed = useBuscarMedicamentos(filtroMed, habilitadoMed);
  const medicamentos = queryMed.data?.itens ?? [];

  function limpar(): void {
    setPacienteId('');
    setProfissionalId('');
    setEstabelecimentoId('');
    setItens([]);
    setErrors({});
    setTermoMed('');
    setMedSelecionado(null);
    setQuantidade('');
    setPosologia('');
  }

  function fechar(): void {
    limpar();
    onClose();
  }

  function adicionarItem(): void {
    if (!medSelecionado || Number(quantidade) <= 0 || posologia.trim() === '') return;
    setItens((atual) => [
      ...atual,
      {
        medicamentoId: medSelecionado.id,
        quantidade: Number(quantidade),
        posologia: posologia.trim(),
        rotulo: `${medSelecionado.principioAtivo} — ${medSelecionado.apresentacao}`,
      },
    ]);
    setMedSelecionado(null);
    setTermoMed('');
    setQuantidade('');
    setPosologia('');
  }

  function removerItem(indice: number): void {
    setItens((atual) => atual.filter((_, i) => i !== indice));
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: FormErrors = {};
    if (pacienteId.trim() === '') next.pacienteId = 'Selecione o paciente.';
    if (estabelecimentoId.trim() === '') next.estabelecimentoId = 'Selecione o estabelecimento.';
    if (profissionalId.trim() === '') next.profissionalId = 'Selecione o profissional.';
    if (itens.length === 0) next.itens = 'Adicione ao menos um item.';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    const input: DispensarMedicamentoInput = {
      pacienteId,
      estabelecimentoId,
      profissionalId,
      itens: itens.map(({ medicamentoId, quantidade: q, posologia: p }) => ({
        medicamentoId,
        quantidade: q,
        posologia: p,
      })),
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Dispensação efetivada (baixa FEFO realizada).', 'Sucesso');
        fechar();
      },
      onError: (error) =>
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível efetivar a dispensação.',
        ),
    });
  }

  const podeAdicionar = medSelecionado !== null && Number(quantidade) > 0 && posologia.trim() !== '';

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Dispensar ao paciente"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-dispensacao" loading={mutation.isPending}>
            Dispensar
          </Button>
        </>
      }
    >
      <form id="form-dispensacao" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="info">
          A baixa de estoque segue <strong>FEFO</strong> (lote válido, primeiro a vencer). Acesso
          registrado em trilha de auditoria (LGPD).
        </Alert>

        <PacientePicker
          label="Paciente"
          value={pacienteId}
          onChange={setPacienteId}
          error={errors.pacienteId}
        />
        <EstabelecimentoPicker
          label="Estabelecimento (farmácia/UBS)"
          required
          value={estabelecimentoId}
          onChange={setEstabelecimentoId}
          error={errors.estabelecimentoId}
        />
        <ProfissionalPicker
          label="Profissional responsável"
          value={profissionalId}
          onChange={setProfissionalId}
          error={errors.profissionalId}
        />

        <fieldset className="mt-3">
          <legend className="text-up-01 text-secondary mb-2">Itens a dispensar</legend>
          <FormField label="Medicamento">
            {({ id, describedBy }) => (
              <div aria-describedby={describedBy}>
                <Input
                  id={id}
                  value={medSelecionado ? `${medSelecionado.principioAtivo}` : termoMed}
                  disabled={medSelecionado !== null}
                  onChange={(e) => setTermoMed(e.target.value)}
                  placeholder="Buscar por princípio ativo (mín. 2 caracteres)"
                />
                {medSelecionado ? (
                  <p className="mt-1 mb-0" aria-live="polite">
                    <Tag variant="success">Selecionado</Tag>{' '}
                    <Button variant="secondary" className="small ml-2" onClick={() => setMedSelecionado(null)}>
                      Trocar
                    </Button>
                  </p>
                ) : (
                  habilitadoMed && (
                    <div className="br-list mt-1" role="listbox" aria-label="Resultados de medicamentos">
                      {queryMed.isFetching && (
                        <span className="d-inline-flex align-items-center p-2">
                          <Spinner /> <span className="ml-2">Buscando…</span>
                        </span>
                      )}
                      {!queryMed.isFetching && medicamentos.length === 0 && (
                        <span className="d-block p-2 text-secondary">Nenhum medicamento encontrado.</span>
                      )}
                      {medicamentos.map((m) => (
                        <button
                          key={m.id}
                          type="button"
                          className="br-item d-flex align-items-center justify-content-between w-100 text-left"
                          onClick={() => setMedSelecionado(m)}
                        >
                          <span>{m.principioAtivo}</span>
                          <span className="text-down-01 text-secondary">{m.apresentacao}</span>
                        </button>
                      ))}
                    </div>
                  )
                )}
              </div>
            )}
          </FormField>
          <div className="row">
            <div className="col-4">
              <FormField label="Quantidade">
                {({ id }) => (
                  <Input id={id} type="number" min="0" step="0.01" value={quantidade} onChange={(e) => setQuantidade(e.target.value)} />
                )}
              </FormField>
            </div>
            <div className="col-8">
              <FormField label="Posologia">
                {({ id }) => (
                  <Input id={id} value={posologia} onChange={(e) => setPosologia(e.target.value)} />
                )}
              </FormField>
            </div>
          </div>
          <Button variant="secondary" className="small" disabled={!podeAdicionar} onClick={adicionarItem}>
            <i className="fas fa-plus" aria-hidden="true" /> Adicionar item
          </Button>

          {errors.itens && (
            <p className="feedback danger mt-2" role="alert">
              {errors.itens}
            </p>
          )}

          {itens.length > 0 && (
            <ul className="br-list mt-2" aria-label="Itens da dispensação">
              {itens.map((item, i) => (
                <li key={`${item.medicamentoId}-${i}`} className="br-item d-flex align-items-center justify-content-between">
                  <span>
                    {item.rotulo} — <strong>{item.quantidade}</strong> · {item.posologia}
                  </span>
                  <Button variant="secondary" className="small" onClick={() => removerItem(i)} aria-label="Remover item">
                    <i className="fas fa-trash" aria-hidden="true" /> Remover
                  </Button>
                </li>
              ))}
            </ul>
          )}
        </fieldset>
      </form>
    </Modal>
  );
}
