// Modais de ACAO do agregado Proposicao com corpo de formulario (Emenda e Parecer).
// As deliberacoes (Aprovar/Rejeitar/Autografo) vivem em ProposicaoDeliberacaoModais
// e sao reexportadas aqui (import unico). As transicoes simples (Distribuir, Ordem
// do Dia, Arquivar) usam ConfirmarAcaoModal diretamente na DetailPage.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, Textarea, useToast } from '../../components/ui';
import { useApresentarEmenda, useRegistrarParecer } from './proposicao.api';
import { mensagemErro, tratarErroCampos } from './legislativoAcao.shared';
import type { AcaoModalBaseProps } from './legislativoAcao.shared';

export { DeliberacaoModal, AutografoModal } from './ProposicaoDeliberacaoModais';

// ---------------------------------------------------------------------------
// EMENDA
// ---------------------------------------------------------------------------

export function EmendaModal({ open, onClose, id }: AcaoModalBaseProps) {
  const toast = useToast();
  const mutation = useApresentarEmenda(id);
  const [texto, setTexto] = useState('');
  const [autoria, setAutoria] = useState('');
  const [errors, setErrors] = useState<{ texto?: string; autoria?: string }>({});

  function fechar(): void {
    setErrors({});
    setTexto('');
    setAutoria('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: { texto?: string; autoria?: string } = {};
    if (texto.trim() === '') next.texto = 'Informe o texto da emenda.';
    if (autoria.trim() === '') next.autoria = 'Informe a autoria da emenda.';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    mutation.mutate(
      { texto: texto.trim(), autoria: autoria.trim() },
      {
        onSuccess: () => {
          toast.success('Emenda apresentada.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          setErrors(tratarErroCampos(error, { texto: 1, autoria: 1 }));
          toast.error(mensagemErro(error, 'Não foi possível apresentar a emenda.'));
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Apresentar emenda"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-emenda" loading={mutation.isPending}>
            Apresentar
          </Button>
        </>
      }
    >
      <form id="form-emenda" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Texto da emenda" required error={errors.texto}>
          {({ id: fid, describedBy, invalid }) => (
            <Textarea
              id={fid}
              aria-describedby={describedBy}
              aria-invalid={invalid || undefined}
              rows={4}
              value={texto}
              onChange={(e) => setTexto(e.target.value)}
            />
          )}
        </FormField>
        <FormField label="Autoria" required error={errors.autoria}>
          {({ id: fid, describedBy, invalid }) => (
            <Input
              id={fid}
              aria-describedby={describedBy}
              invalid={invalid}
              value={autoria}
              onChange={(e) => setAutoria(e.target.value)}
              placeholder="Vereador(a) Fulano de Tal"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------
// PARECER
// ---------------------------------------------------------------------------

export function ParecerModal({ open, onClose, id }: AcaoModalBaseProps) {
  const toast = useToast();
  const mutation = useRegistrarParecer(id);
  const [comissao, setComissao] = useState('');
  const [favoravel, setFavoravel] = useState('true');
  const [errors, setErrors] = useState<{ comissao?: string }>({});

  function fechar(): void {
    setErrors({});
    setComissao('');
    setFavoravel('true');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (comissao.trim() === '') {
      setErrors({ comissao: 'Informe a comissão emitente.' });
      return;
    }
    setErrors({});
    mutation.mutate(
      { comissao: comissao.trim(), favoravel: favoravel === 'true' },
      {
        onSuccess: () => {
          toast.success('Parecer registrado.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          setErrors(tratarErroCampos(error, { comissao: 1 }));
          toast.error(mensagemErro(error, 'Não foi possível registrar o parecer.'));
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar parecer"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-parecer" loading={mutation.isPending}>
            Registrar
          </Button>
        </>
      }
    >
      <form id="form-parecer" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Comissão" required error={errors.comissao}>
          {({ id: fid, describedBy, invalid }) => (
            <Input
              id={fid}
              aria-describedby={describedBy}
              invalid={invalid}
              value={comissao}
              onChange={(e) => setComissao(e.target.value)}
              placeholder="Comissão de Constituição e Justiça"
            />
          )}
        </FormField>
        <FormField label="Sentido do parecer" required>
          {({ id: fid, describedBy }) => (
            <Select
              id={fid}
              aria-describedby={describedBy}
              value={favoravel}
              onChange={(e) => setFavoravel(e.target.value)}
              options={[
                { value: 'true', label: 'Favorável' },
                { value: 'false', label: 'Contrário' },
              ]}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
