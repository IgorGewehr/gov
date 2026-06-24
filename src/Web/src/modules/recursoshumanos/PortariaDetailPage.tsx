// Tela de DETALHE de uma portaria (param de rota -> useQuery). QueryState para loading/erro,
// Card com pares rótulo/valor + texto integral, e ação de revogação (gated por gerenciar).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  Button,
  Card,
  FormField,
  Input,
  Modal,
  PageHeader,
  QueryState,
  Tag,
  useToast,
} from '../../components/ui';
import { Can } from '../../auth/Can';
import { ApiError } from '../../api/problemDetails';
import { formatarData } from '../../i18n/format';
import { usePortaria, useRevogarPortaria } from './api';
import type { PortariaDetalhe } from './api';
import { PERM_RH_GERENCIAR, situacaoPortariaTagVariant } from './recursosHumanos.helpers';

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

function RevogarPortariaModal({
  open,
  onClose,
  portariaId,
}: {
  open: boolean;
  onClose: () => void;
  portariaId: string;
}) {
  const toast = useToast();
  const mutation = useRevogarPortaria(portariaId);
  const [motivo, setMotivo] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (motivo.trim() === '') {
      setErro('Informe o motivo da revogação.');
      return;
    }
    mutation.mutate(motivo.trim(), {
      onSuccess: () => {
        toast.success('Portaria revogada.', 'Sucesso');
        setMotivo('');
        setErro(undefined);
        onClose();
      },
      onError: (error) =>
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível revogar a portaria.',
        ),
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Revogar portaria"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="danger" type="submit" form="form-revogar-portaria" loading={mutation.isPending}>
            Revogar
          </Button>
        </>
      }
    >
      <form id="form-revogar-portaria" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Motivo da revogação" required error={erro}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={motivo}
              onChange={(e) => setMotivo(e.target.value)}
              maxLength={500}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

export function PortariaDetailPage() {
  const { portariaId = '' } = useParams<{ portariaId: string }>();
  const query = usePortaria(portariaId);
  const [revogando, setRevogando] = useState(false);

  return (
    <>
      <PageHeader
        title="Detalhe da Portaria"
        actions={
          <Link className="br-button secondary" to="/recursoshumanos/portarias">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<PortariaDetalhe>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
      >
        {(portaria) => (
          <>
            <Card className="mb-4" header={<strong>Portaria nº {portaria.numero}</strong>}>
              <dl className="row">
                <Campo rotulo="Situação">
                  <Tag variant={situacaoPortariaTagVariant(portaria.situacao)}>
                    {portaria.situacao}
                  </Tag>
                </Campo>
                <Campo rotulo="Natureza">{portaria.tipo}</Campo>
                <Campo rotulo="Exercício">{portaria.exercicio}</Campo>
                <Campo rotulo="Data do ato">{formatarData(portaria.dataAto)}</Campo>
                <Campo rotulo="Ementa">{portaria.ementa}</Campo>
                {portaria.motivoRevogacao ? (
                  <Campo rotulo="Motivo da revogação">{portaria.motivoRevogacao}</Campo>
                ) : null}
              </dl>
              <h3 className="text-up-01 mb-2">Texto do ato</h3>
              <p style={{ whiteSpace: 'pre-wrap' }}>{portaria.texto}</p>
            </Card>

            {portaria.situacao === 'Emitida' && (
              <Can permission={PERM_RH_GERENCIAR}>
                <Card header={<strong>Ações</strong>}>
                  <Button variant="danger" onClick={() => setRevogando(true)}>
                    <i className="fas fa-ban" aria-hidden="true" /> Revogar
                  </Button>
                </Card>
                <RevogarPortariaModal
                  open={revogando}
                  onClose={() => setRevogando(false)}
                  portariaId={portaria.id}
                />
              </Can>
            )}
          </>
        )}
      </QueryState>
    </>
  );
}
