// Modal de ATRIBUIÇÕES de papel com escopo organizacional (M1 / Identidade).
//   - Lista as atribuições atuais do usuário (papel + UO + escopo + vigência);
//   - ATRIBUIR: escolhe papel (GET /papeis) + UO (GET /unidades, árvore) +
//     incluiSubunidades + vigência (opcional) -> POST /usuarios/{id}/atribuicoes;
//   - REVOGAR: DELETE /usuarios/{id}/atribuicoes com a atribuição selecionada.
// O backend NEGA conceder/revogar além do próprio escopo (regra "não delega o que
// não tem"): o 403 é tratado com mensagem amigável (mensagemErroAtribuicao). Segue o
// PADRÃO-OURO dos modais de ação (UsuarioPapeisModal): controlado, toast, invalidação.
import { useEffect, useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, Select, Tag, useToast } from '../../../components/ui';
import { usePapeis } from './usuario.api';
import type { UsuarioResumo } from './usuario.api';
import {
  useAtribuicoes,
  useAtribuirPapel,
  useRevogarPapel,
  useUnidades,
} from './atribuicao.api';
import type { AtribuicaoInput, AtribuicaoResumo } from './atribuicao.api';
import {
  atribuicaoKey,
  escopoLabel,
  mapaUnidadesNome,
  mensagemErroAtribuicao,
  unidadesParaOpcoes,
  vigenciaLabel,
} from './atribuicao.helpers';

export interface UsuarioAtribuicoesModalProps {
  open: boolean;
  onClose: () => void;
  usuario: UsuarioResumo | null;
}

interface FormState {
  papelId: string;
  unidadeId: string;
  incluiSubunidades: boolean;
  vigenciaFim: string;
}

const ESTADO_INICIAL: FormState = {
  papelId: '',
  unidadeId: '',
  incluiSubunidades: false,
  vigenciaFim: '',
};

export function UsuarioAtribuicoesModal({ open, onClose, usuario }: UsuarioAtribuicoesModalProps) {
  const toast = useToast();
  const usuarioId = usuario?.id ?? null;

  const atribuicoesQuery = useAtribuicoes(usuarioId, open);
  const papeisQuery = usePapeis(open);
  const unidadesQuery = useUnidades(open);
  const atribuir = useAtribuirPapel();
  const revogar = useRevogarPapel();

  const [form, setForm] = useState<FormState>(ESTADO_INICIAL);
  const [erro, setErro] = useState<string | undefined>(undefined);

  // Reinicia o formulário ao abrir / trocar de usuário.
  useEffect(() => {
    if (!open) return;
    setForm(ESTADO_INICIAL);
    setErro(undefined);
  }, [open, usuarioId]);

  const opcoesUnidades = useMemo(() => unidadesParaOpcoes(unidadesQuery.data), [unidadesQuery.data]);
  const nomesUnidades = useMemo(() => mapaUnidadesNome(unidadesQuery.data), [unidadesQuery.data]);
  const opcoesPapeis = useMemo(
    () => (papeisQuery.data ?? []).map((p) => ({ value: p.id, label: p.nome })),
    [papeisQuery.data],
  );

  function nomeUnidade(a: AtribuicaoResumo): string {
    return a.unidadeNome || nomesUnidades.get(a.unidadeId) || a.unidadeId;
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (!usuarioId) return;
    if (form.papelId === '') {
      setErro('Selecione o papel a atribuir.');
      return;
    }
    if (form.unidadeId === '') {
      setErro('Selecione a unidade organizacional (escopo).');
      return;
    }
    setErro(undefined);

    const input: AtribuicaoInput = {
      usuarioId,
      papelId: form.papelId,
      unidadeId: form.unidadeId,
      incluiSubunidades: form.incluiSubunidades,
      ...(form.vigenciaFim ? { vigenciaFim: form.vigenciaFim } : {}),
    };

    atribuir.mutate(input, {
      onSuccess: () => {
        toast.success('Papel atribuído.', 'Sucesso');
        setForm(ESTADO_INICIAL);
      },
      onError: (error) =>
        toast.error(mensagemErroAtribuicao(error, 'Não foi possível atribuir o papel.')),
    });
  }

  function aoRevogar(a: AtribuicaoResumo): void {
    if (!usuarioId) return;
    const input: AtribuicaoInput = {
      usuarioId,
      papelId: a.papelId,
      unidadeId: a.unidadeId,
      incluiSubunidades: a.incluiSubunidades,
    };
    revogar.mutate(input, {
      onSuccess: () => toast.success('Atribuição revogada.', 'Sucesso'),
      onError: (error) =>
        toast.error(mensagemErroAtribuicao(error, 'Não foi possível revogar a atribuição.')),
    });
  }

  const revogandoKey =
    revogar.isPending && revogar.variables ? atribuicaoKey(revogar.variables) : null;

  return (
    <Modal
      open={open}
      onClose={onClose}
      size="large"
      title={usuario ? `Atribuições — ${usuario.nome}` : 'Atribuições'}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={atribuir.isPending}>
            Fechar
          </Button>
          <Button
            variant="primary"
            type="submit"
            form="form-atribuir"
            loading={atribuir.isPending}
          >
            Atribuir papel
          </Button>
        </>
      }
    >
      <Alert variant="info" title="Regra de escopo:">
        Você só pode conceder ou revogar papéis dentro do seu próprio escopo
        organizacional. O sistema não delega o que você não tem.
      </Alert>

      {/* -------------------- Atribuições atuais -------------------- */}
      <section aria-labelledby="titulo-atuais" className="mt-3">
        <h3 id="titulo-atuais" className="text-up-01 text-weight-semi-bold">
          Atribuições atuais
        </h3>
        {atribuicoesQuery.isLoading && <p>Carregando atribuições…</p>}
        {atribuicoesQuery.isError && (
          <Alert variant="warning">Não foi possível carregar as atribuições.</Alert>
        )}
        {atribuicoesQuery.data?.length === 0 && (
          <p className="text-down-01">Nenhuma atribuição registrada para este usuário.</p>
        )}
        {atribuicoesQuery.data && atribuicoesQuery.data.length > 0 && (
          <ul className="br-list">
            {atribuicoesQuery.data.map((a) => {
              const chave = atribuicaoKey(a);
              return (
                <li
                  className="br-item d-flex align-items-center justify-content-between"
                  key={chave}
                  style={{ gap: '1rem', flexWrap: 'wrap' }}
                >
                  <div>
                    <span className="text-weight-semi-bold">{a.papelNome}</span>{' '}
                    <span className="text-down-01">em {nomeUnidade(a)}</span>
                    <div className="d-flex mt-1" style={{ gap: '0.5rem', flexWrap: 'wrap' }}>
                      <Tag variant="info">{escopoLabel(a.incluiSubunidades)}</Tag>
                      <Tag variant="default">{vigenciaLabel(a.vigenciaFim)}</Tag>
                    </div>
                  </div>
                  <Button
                    variant="tertiary"
                    className="small"
                    loading={revogandoKey === chave}
                    onClick={() => aoRevogar(a)}
                    aria-label={`Revogar ${a.papelNome} em ${nomeUnidade(a)}`}
                  >
                    Revogar
                  </Button>
                </li>
              );
            })}
          </ul>
        )}
      </section>

      {/* -------------------- Atribuir novo papel -------------------- */}
      <section aria-labelledby="titulo-atribuir" className="mt-4">
        <h3 id="titulo-atribuir" className="text-up-01 text-weight-semi-bold">
          Atribuir papel
        </h3>
        {(papeisQuery.isError || unidadesQuery.isError) && (
          <Alert variant="warning">
            Não foi possível carregar papéis e/ou unidades para a atribuição.
          </Alert>
        )}
        <form id="form-atribuir" className="br-form" onSubmit={submeter} noValidate>
          {erro && (
            <Alert variant="danger" title="Verifique:">
              {erro}
            </Alert>
          )}

          <FormField label="Papel" required>
            {({ id, describedBy, invalid }) => (
              <Select
                id={id}
                aria-describedby={describedBy}
                invalid={invalid}
                placeholder={papeisQuery.isLoading ? 'Carregando papéis…' : 'Selecione o papel'}
                options={opcoesPapeis}
                value={form.papelId}
                onChange={(e) => setForm((f) => ({ ...f, papelId: e.target.value }))}
              />
            )}
          </FormField>

          <FormField label="Unidade organizacional (escopo)" required>
            {({ id, describedBy, invalid }) => (
              <Select
                id={id}
                aria-describedby={describedBy}
                invalid={invalid}
                placeholder={
                  unidadesQuery.isLoading ? 'Carregando unidades…' : 'Selecione a unidade'
                }
                options={opcoesUnidades}
                value={form.unidadeId}
                onChange={(e) => setForm((f) => ({ ...f, unidadeId: e.target.value }))}
              />
            )}
          </FormField>

          <div className="br-checkbox mt-2">
            <input
              id="inclui-subunidades"
              type="checkbox"
              checked={form.incluiSubunidades}
              onChange={(e) => setForm((f) => ({ ...f, incluiSubunidades: e.target.checked }))}
            />
            <label htmlFor="inclui-subunidades">
              Incluir subunidades (escopo abrange as UOs filhas)
            </label>
          </div>

          <FormField
            label="Vigência (fim)"
            help="Opcional. Deixe em branco para vigência indeterminada."
          >
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                type="date"
                aria-describedby={describedBy}
                invalid={invalid}
                value={form.vigenciaFim}
                onChange={(e) => setForm((f) => ({ ...f, vigenciaFim: e.target.value }))}
              />
            )}
          </FormField>
        </form>
      </section>
    </Modal>
  );
}
