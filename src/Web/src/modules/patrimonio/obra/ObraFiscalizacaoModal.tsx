// Fiscalização da obra (art. 117): [Command DesignarFiscal] (fiscal designado + ato)
// e [Command RegistrarOcorrencia] (notificação/advertência/registro técnico). O modo
// define qual comando roda; renderizado a partir da ficha.
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Button,
  FormField,
  Input,
  Modal,
  Select,
  Textarea,
  useToast,
} from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useAuth } from '../../../auth/useAuth';
import { useDesignarFiscal, useRegistrarOcorrencia } from './obra.api';
import type { TipoOcorrenciaValor } from './obra.api';
import { OPCOES_TIPO_OCORRENCIA, hojeIso } from './obra.helpers';

export type FiscalizacaoAcao = 'fiscal' | 'ocorrencia';

export interface ObraFiscalizacaoModalProps {
  open: boolean;
  onClose: () => void;
  obraId: string;
  acao: FiscalizacaoAcao;
}

export function ObraFiscalizacaoModal({
  open,
  onClose,
  obraId,
  acao,
}: ObraFiscalizacaoModalProps) {
  const toast = useToast();
  const { user } = useAuth();
  const designar = useDesignarFiscal(obraId);
  const registrar = useRegistrarOcorrencia(obraId);

  const [fiscalId, setFiscalId] = useState(user?.id ?? '');
  const [desde, setDesde] = useState(hojeIso());
  const [atoDesignacao, setAtoDesignacao] = useState('');

  const [data, setData] = useState(hojeIso());
  const [tipo, setTipo] = useState(String(OPCOES_TIPO_OCORRENCIA[0]?.value ?? ''));
  const [descricao, setDescricao] = useState('');
  const [registradaPorId, setRegistradaPorId] = useState(user?.id ?? '');

  const [erro, setErro] = useState<string | null>(null);
  const pendente = designar.isPending || registrar.isPending;

  function fechar(): void {
    setErro(null);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (acao === 'fiscal') {
      if (fiscalId.trim() === '' || desde.trim() === '' || atoDesignacao.trim() === '') {
        setErro('Informe o fiscal, a data inicial e o ato de designação.');
        return;
      }
      setErro(null);
      designar.mutate(
        { fiscalId: fiscalId.trim(), desde, atoDesignacao: atoDesignacao.trim() },
        {
          onSuccess: () => {
            toast.success('Fiscal designado.', 'Sucesso');
            fechar();
          },
          onError: (e) =>
            toast.error(e instanceof ApiError ? e.userMessage : 'Não foi possível designar o fiscal.'),
        },
      );
      return;
    }

    if (data.trim() === '' || descricao.trim() === '' || registradaPorId.trim() === '') {
      setErro('Informe data, descrição e quem registra a ocorrência.');
      return;
    }
    setErro(null);
    registrar.mutate(
      {
        data,
        tipo: Number(tipo) as TipoOcorrenciaValor,
        descricao: descricao.trim(),
        registradaPorId: registradaPorId.trim(),
      },
      {
        onSuccess: () => {
          toast.success('Ocorrência registrada.', 'Sucesso');
          fechar();
        },
        onError: (e) =>
          toast.error(
            e instanceof ApiError ? e.userMessage : 'Não foi possível registrar a ocorrência.',
          ),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title={acao === 'fiscal' ? 'Designar fiscal' : 'Registrar ocorrência'}
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={pendente}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-fiscalizacao" loading={pendente}>
            {acao === 'fiscal' ? 'Designar' : 'Registrar'}
          </Button>
        </>
      }
    >
      <form id="form-fiscalizacao" className="br-form" onSubmit={submeter} noValidate>
        {erro && (
          <p className="text-danger text-down-01 mb-3" role="alert">
            {erro}
          </p>
        )}
        {acao === 'fiscal' ? (
          <>
            <FormField label="Fiscal (identificador)" required help="Pré-preenchido com o usuário atual.">
              {({ id, describedBy }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  value={fiscalId}
                  onChange={(e) => setFiscalId(e.target.value)}
                />
              )}
            </FormField>
            <div className="row">
              <div className="col-sm-5">
                <FormField label="Designado desde" required>
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      type="date"
                      aria-describedby={describedBy}
                      value={desde}
                      onChange={(e) => setDesde(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-sm-7">
                <FormField label="Ato de designação" required help="Portaria/ato administrativo.">
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      aria-describedby={describedBy}
                      value={atoDesignacao}
                      onChange={(e) => setAtoDesignacao(e.target.value)}
                      placeholder="Ex.: Portaria 123/2026"
                    />
                  )}
                </FormField>
              </div>
            </div>
          </>
        ) : (
          <>
            <div className="row">
              <div className="col-sm-5">
                <FormField label="Data" required>
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      type="date"
                      aria-describedby={describedBy}
                      value={data}
                      onChange={(e) => setData(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-sm-7">
                <FormField label="Tipo" required>
                  {({ id, describedBy }) => (
                    <Select
                      id={id}
                      aria-describedby={describedBy}
                      options={OPCOES_TIPO_OCORRENCIA}
                      value={tipo}
                      onChange={(e) => setTipo(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
            </div>
            <FormField label="Descrição" required>
              {({ id, describedBy }) => (
                <Textarea
                  id={id}
                  aria-describedby={describedBy}
                  value={descricao}
                  onChange={(e) => setDescricao(e.target.value)}
                />
              )}
            </FormField>
            <FormField
              label="Registrada por (identificador)"
              required
              help="Pré-preenchido com o usuário atual."
            >
              {({ id, describedBy }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  value={registradaPorId}
                  onChange={(e) => setRegistradaPorId(e.target.value)}
                />
              )}
            </FormField>
          </>
        )}
      </form>
    </Modal>
  );
}
