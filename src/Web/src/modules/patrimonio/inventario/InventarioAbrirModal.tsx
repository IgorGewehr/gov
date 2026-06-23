// [Command AbrirInventario] Abre um inventário patrimonial (Lei 4.320 art. 96) com a
// comissão designada por portaria, em Modal. Comissão com membros dinâmicos (mínimo 1).
// Validação por campo + mapeamento de ApiError.fieldErrors e Toast.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useAbrirInventario } from './inventario.api';
import type { AbrirInventarioInput, MembroComissaoEntradaInput } from './inventario.api';
import { TIPO_INVENTARIO } from './inventario.api';
import { OPCOES_TIPO_INVENTARIO, hojeIso, mapearFieldErrors } from './inventario.helpers';

export interface InventarioAbrirModalProps {
  open: boolean;
  onClose: () => void;
  /** Callback opcional após abrir (ex.: navegar ao detalhe). */
  onAberto?: (id: string) => void;
}

const CAMPOS = ['exercicio', 'tipo', 'setor', 'portaria', 'dataAbertura', 'membros'] as const;
type Campo = (typeof CAMPOS)[number];
type FormErrors = Partial<Record<Campo, string>>;

interface MembroForm {
  responsavelId: string;
  nome: string;
  presidente: boolean;
}

function membroVazio(): MembroForm {
  return { responsavelId: '', nome: '', presidente: false };
}

export function InventarioAbrirModal({ open, onClose, onAberto }: InventarioAbrirModalProps) {
  const toast = useToast();
  const mutation = useAbrirInventario();

  const [exercicio, setExercicio] = useState(String(new Date().getFullYear()));
  const [tipo, setTipo] = useState(String(TIPO_INVENTARIO.Anual));
  const [setor, setSetor] = useState('');
  const [portaria, setPortaria] = useState('');
  const [dataAbertura, setDataAbertura] = useState(hojeIso());
  const [membros, setMembros] = useState<MembroForm[]>([{ ...membroVazio(), presidente: true }]);
  const [errors, setErrors] = useState<FormErrors>({});

  function atualizarMembro(indice: number, campo: keyof MembroForm, valor: string | boolean): void {
    setMembros((atuais) =>
      atuais.map((m, i) => {
        if (i !== indice) {
          // Presidente é exclusivo: ao marcar um, desmarca os demais.
          return campo === 'presidente' && valor === true ? { ...m, presidente: false } : m;
        }
        return { ...m, [campo]: valor };
      }),
    );
  }

  function adicionarMembro(): void {
    setMembros((atuais) => [...atuais, membroVazio()]);
  }

  function removerMembro(indice: number): void {
    setMembros((atuais) => atuais.filter((_, i) => i !== indice));
  }

  function validar(): FormErrors {
    const next: FormErrors = {};
    const ano = Number(exercicio);
    if (exercicio.trim() === '' || Number.isNaN(ano) || ano <= 0)
      next.exercicio = 'Informe o exercício (ano-base).';
    if (portaria.trim() === '') next.portaria = 'Informe a portaria de designação.';
    else if (portaria.trim().length > 100) next.portaria = 'Portaria deve ter no máximo 100 caracteres.';
    if (setor.trim().length > 200) next.setor = 'Setor deve ter no máximo 200 caracteres.';
    if (dataAbertura.trim() === '') next.dataAbertura = 'Informe a data de abertura.';
    if (membros.length === 0) next.membros = 'Designe ao menos um membro da comissão.';
    else if (membros.some((m) => m.nome.trim() === '' || m.responsavelId.trim() === ''))
      next.membros = 'Preencha vínculo e nome de todos os membros.';
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

    const membrosEntrada: MembroComissaoEntradaInput[] = membros.map((m) => ({
      responsavelId: m.responsavelId.trim(),
      nome: m.nome.trim(),
      presidente: m.presidente,
    }));

    const input: AbrirInventarioInput = {
      exercicio: Number(exercicio),
      tipo: Number(tipo),
      setor: setor.trim() || null,
      portaria: portaria.trim(),
      dataAbertura,
      membros: membrosEntrada,
      minimoMembrosComissao: null,
    };

    mutation.mutate(input, {
      onSuccess: (resposta) => {
        toast.success(`Inventário ${input.exercicio} aberto.`, 'Sucesso');
        onAberto?.(resposta.id);
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          setErrors(mapearFieldErrors(error.fieldErrors, CAMPOS));
        }
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível abrir o inventário.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Abrir inventário"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-abrir-inventario" loading={mutation.isPending}>
            Abrir inventário
          </Button>
        </>
      }
    >
      <form id="form-abrir-inventario" className="br-form" onSubmit={submeter} noValidate>
        <div className="row">
          <div className="col-sm-6">
            <FormField label="Exercício" required error={errors.exercicio} help="Ano-base do levantamento.">
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="2000"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={exercicio}
                  onChange={(e) => setExercicio(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="Tipo" required error={errors.tipo}>
              {({ id, describedBy, invalid }) => (
                <Select
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  options={OPCOES_TIPO_INVENTARIO}
                  value={tipo}
                  onChange={(e) => setTipo(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>

        <FormField label="Setor/UO" error={errors.setor} help="Deixe em branco para inventário geral.">
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={200}
              value={setor}
              onChange={(e) => setSetor(e.target.value)}
              placeholder="Ex.: Secretaria de Saúde"
            />
          )}
        </FormField>

        <div className="row">
          <div className="col-sm-7">
            <FormField label="Portaria" required error={errors.portaria} help="Portaria de designação da comissão.">
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  maxLength={100}
                  value={portaria}
                  onChange={(e) => setPortaria(e.target.value)}
                  placeholder="Portaria 123/2026"
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-5">
            <FormField label="Data de abertura" required error={errors.dataAbertura}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="date"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={dataAbertura}
                  onChange={(e) => setDataAbertura(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>

        <fieldset className="mt-2">
          <legend className="text-semi-bold">Comissão de inventário</legend>
          {errors.membros && (
            <p className="feedback danger" role="alert">
              <i className="fas fa-times-circle" aria-hidden="true" /> {errors.membros}
            </p>
          )}
          {membros.map((membro, indice) => (
            <div className="row align-items-end mb-2" key={indice}>
              <div className="col-sm-5">
                <FormField label={`Vínculo (Id) — membro ${indice + 1}`}>
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      aria-describedby={describedBy}
                      value={membro.responsavelId}
                      onChange={(e) => atualizarMembro(indice, 'responsavelId', e.target.value)}
                      placeholder="Id do servidor"
                    />
                  )}
                </FormField>
              </div>
              <div className="col-sm-5">
                <FormField label="Nome">
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      aria-describedby={describedBy}
                      maxLength={200}
                      value={membro.nome}
                      onChange={(e) => atualizarMembro(indice, 'nome', e.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-sm-2 mb-3 d-flex flex-column" style={{ gap: 'var(--spacing-scale-half)' }}>
                <div className="br-checkbox">
                  <input
                    id={`presidente-${indice}`}
                    type="checkbox"
                    checked={membro.presidente}
                    onChange={(e) => atualizarMembro(indice, 'presidente', e.target.checked)}
                  />
                  <label htmlFor={`presidente-${indice}`}>Presidente</label>
                </div>
                {membros.length > 1 && (
                  <Button
                    variant="tertiary"
                    onClick={() => removerMembro(indice)}
                    aria-label={`Remover membro ${indice + 1}`}
                  >
                    <i className="fas fa-trash" aria-hidden="true" /> Remover
                  </Button>
                )}
              </div>
            </div>
          ))}
          <Button variant="secondary" onClick={adicionarMembro}>
            <i className="fas fa-plus" aria-hidden="true" /> Adicionar membro
          </Button>
        </fieldset>
      </form>
    </Modal>
  );
}
