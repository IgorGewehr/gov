// Ações sobre um boletim de medição: [Command AprovarMedicao] (libera liquidação em
// Finanças — Lei 4.320 art. 63 / I-1/I-8/I-10) e [Command RejeitarMedicao] (não compõe
// o medido acumulado). Renderizado a partir da ficha; o modo define qual comando roda.
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Alert,
  Button,
  FormField,
  Input,
  Modal,
  Textarea,
  useToast,
} from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useAuth } from '../../../auth/useAuth';
import { useAprovarMedicao, useRejeitarMedicao } from './obra.api';
import { hojeIso } from './obra.helpers';

export type MedicaoAcao = 'aprovar' | 'rejeitar';

export interface ObraMedicaoAcoesModalProps {
  open: boolean;
  onClose: () => void;
  obraId: string;
  medicaoId: string;
  acao: MedicaoAcao;
}

export function ObraMedicaoAcoesModal({
  open,
  onClose,
  obraId,
  medicaoId,
  acao,
}: ObraMedicaoAcoesModalProps) {
  const toast = useToast();
  const { user } = useAuth();
  const aprovar = useAprovarMedicao(obraId, medicaoId);
  const rejeitar = useRejeitarMedicao(obraId, medicaoId);

  const [fiscalId, setFiscalId] = useState(user?.id ?? '');
  const [dataAprovacao, setDataAprovacao] = useState(hojeIso());
  const [motivo, setMotivo] = useState('');
  const [erro, setErro] = useState<string | null>(null);

  const pendente = aprovar.isPending || rejeitar.isPending;

  function fechar(): void {
    setErro(null);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (acao === 'aprovar') {
      if (fiscalId.trim() === '' || dataAprovacao.trim() === '') {
        setErro('Informe o fiscal e a data de aprovação.');
        return;
      }
      setErro(null);
      aprovar.mutate(
        { fiscalId: fiscalId.trim(), dataAprovacao },
        {
          onSuccess: () => {
            toast.success('Medição aprovada — liberada a liquidação em Finanças.', 'Sucesso');
            fechar();
          },
          onError: (e) =>
            toast.error(
              e instanceof ApiError ? e.userMessage : 'Não foi possível aprovar a medição.',
            ),
        },
      );
      return;
    }

    if (motivo.trim() === '') {
      setErro('Informe o motivo da rejeição.');
      return;
    }
    setErro(null);
    rejeitar.mutate(motivo.trim(), {
      onSuccess: () => {
        toast.success('Medição rejeitada.', 'Sucesso');
        fechar();
      },
      onError: (e) =>
        toast.error(e instanceof ApiError ? e.userMessage : 'Não foi possível rejeitar a medição.'),
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title={acao === 'aprovar' ? 'Aprovar medição' : 'Rejeitar medição'}
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={pendente}>
            Cancelar
          </Button>
          <Button
            variant={acao === 'aprovar' ? 'primary' : 'danger'}
            type="submit"
            form="form-medicao-acao"
            loading={pendente}
          >
            {acao === 'aprovar' ? 'Aprovar' : 'Rejeitar'}
          </Button>
        </>
      }
    >
      <form id="form-medicao-acao" className="br-form" onSubmit={submeter} noValidate>
        {erro && (
          <p className="text-danger text-down-01 mb-3" role="alert">
            {erro}
          </p>
        )}
        {acao === 'aprovar' ? (
          <>
            <Alert variant="info" title="Efeito da aprovação">
              A aprovação do fiscal é o gatilho da liquidação em Finanças (Lei 4.320, art. 63) e
              respeita o teto do valor contratado.
            </Alert>
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
            <FormField label="Data da aprovação" required>
              {({ id, describedBy }) => (
                <Input
                  id={id}
                  type="date"
                  aria-describedby={describedBy}
                  value={dataAprovacao}
                  onChange={(e) => setDataAprovacao(e.target.value)}
                />
              )}
            </FormField>
          </>
        ) : (
          <FormField label="Motivo da rejeição" required>
            {({ id, describedBy }) => (
              <Textarea
                id={id}
                aria-describedby={describedBy}
                value={motivo}
                onChange={(e) => setMotivo(e.target.value)}
              />
            )}
          </FormField>
        )}
      </form>
    </Modal>
  );
}
