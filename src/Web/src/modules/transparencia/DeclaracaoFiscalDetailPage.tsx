// Tela de DETALHE de uma Declaração Fiscal. Param de rota -> useQuery, QueryState para
// loading/erro, layout de Card com pares rótulo/valor e ações de transição de estado
// (transmitir/homologar/rejeitar) via mutation + Toast. Rejeição exige motivo (modal).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  Button,
  Card,
  FormField,
  Modal,
  PageHeader,
  QueryState,
  Tag,
  Textarea,
  useToast,
} from '../../components/ui';
import { Can } from '../../auth/Can';
import { ApiError } from '../../api/problemDetails';
import { formatarData, formatarMoeda } from '../../i18n/format';
import { useDeclaracaoFiscal, useTransicaoDeclaracaoFiscal } from './api';
import type { DeclaracaoFiscalDetalhe } from './api';
import { situacaoDeclaracaoTagVariant, tipoDeclaracaoLabel } from './transparencia.helpers';

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

export function DeclaracaoFiscalDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const toast = useToast();
  const query = useDeclaracaoFiscal(id);
  const transicao = useTransicaoDeclaracaoFiscal();

  const [rejeicaoAberta, setRejeicaoAberta] = useState(false);
  const [motivo, setMotivo] = useState('');
  const [motivoErro, setMotivoErro] = useState<string | undefined>();

  function transmitir(): void {
    transicao.mutate(
      { id, acao: 'transmitir' },
      {
        onSuccess: () => toast.success('Declaração transmitida ao SICONFI.', 'Sucesso'),
        onError: (error) =>
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível transmitir.'),
      },
    );
  }

  function homologar(): void {
    transicao.mutate(
      { id, acao: 'homologar' },
      {
        onSuccess: () => toast.success('Declaração homologada.', 'Sucesso'),
        onError: (error) =>
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível homologar.'),
      },
    );
  }

  function confirmarRejeicao(event: FormEvent): void {
    event.preventDefault();
    if (motivo.trim() === '') {
      setMotivoErro('Informe o motivo da rejeição.');
      return;
    }
    transicao.mutate(
      { id, acao: 'rejeitar', motivo: motivo.trim() },
      {
        onSuccess: () => {
          toast.success('Declaração rejeitada.', 'Sucesso');
          setRejeicaoAberta(false);
          setMotivo('');
          setMotivoErro(undefined);
        },
        onError: (error) =>
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível rejeitar.'),
      },
    );
  }

  return (
    <>
      <PageHeader
        title="Detalhe da Declaração Fiscal"
        actions={
          <Link className="br-button secondary" to="/transparencia/declaracoes-fiscais">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<DeclaracaoFiscalDetalhe>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
      >
        {(declaracao) => {
          const podeTransmitir = declaracao.situacao === 'Consolidada';
          const podeDecidir = declaracao.situacao === 'Transmitida';
          return (
            <Card
              header={<strong>{tipoDeclaracaoLabel[declaracao.tipoDeclaracao]}</strong>}
              footer={
                <Can permission="transparencia.gerenciar">
                  <div className="d-flex flex-wrap gap-2">
                    <Button
                      variant="primary"
                      loading={transicao.isPending}
                      disabled={transicao.isPending || !podeTransmitir}
                      onClick={transmitir}
                    >
                      <i className="fas fa-paper-plane" aria-hidden="true" /> Transmitir
                    </Button>
                    <Button
                      variant="secondary"
                      disabled={transicao.isPending || !podeDecidir}
                      onClick={homologar}
                    >
                      <i className="fas fa-circle-check" aria-hidden="true" /> Homologar
                    </Button>
                    <Button
                      variant="secondary"
                      disabled={transicao.isPending || !podeDecidir}
                      onClick={() => setRejeicaoAberta(true)}
                    >
                      <i className="fas fa-circle-xmark" aria-hidden="true" /> Rejeitar
                    </Button>
                  </div>
                </Can>
              }
            >
              <dl className="row">
                <Campo rotulo="Situação">
                  <Tag variant={situacaoDeclaracaoTagVariant(declaracao.situacao)}>
                    {declaracao.situacao}
                  </Tag>
                </Campo>
                <Campo rotulo="Exercício">{declaracao.exercicio}</Campo>
                <Campo rotulo="Competência">{declaracao.competencia ?? '—'}</Campo>
                <Campo rotulo="Bimestre">{declaracao.bimestre ?? '—'}</Campo>
                <Campo rotulo="Quadrimestre">{declaracao.quadrimestre ?? '—'}</Campo>
                <Campo rotulo="Protocolo SICONFI">{declaracao.protocoloSiconfi ?? '—'}</Campo>
                <Campo rotulo="Prazo legal">{formatarData(declaracao.dataLimite)}</Campo>
                <Campo rotulo="Data de transmissão">{formatarData(declaracao.dataTransmissao)}</Campo>
                <Campo rotulo="Total de débitos">{formatarMoeda(declaracao.totalDebitos)}</Campo>
                <Campo rotulo="Total de créditos">{formatarMoeda(declaracao.totalCreditos)}</Campo>
              </dl>
            </Card>
          );
        }}
      </QueryState>

      <Modal
        open={rejeicaoAberta}
        onClose={() => setRejeicaoAberta(false)}
        title="Rejeitar declaração"
        footer={
          <>
            <Button
              variant="secondary"
              onClick={() => setRejeicaoAberta(false)}
              disabled={transicao.isPending}
            >
              Cancelar
            </Button>
            <Button variant="primary" type="submit" form="form-rejeitar-declaracao" loading={transicao.isPending}>
              Confirmar rejeição
            </Button>
          </>
        }
      >
        <form id="form-rejeitar-declaracao" className="br-form" onSubmit={confirmarRejeicao} noValidate>
          <FormField label="Motivo da rejeição" required error={motivoErro}>
            {({ id, describedBy, invalid }) => (
              <Textarea
                id={id}
                aria-describedby={describedBy}
                invalid={invalid}
                value={motivo}
                onChange={(e) => setMotivo(e.target.value)}
                placeholder="Descreva o motivo da rejeição informado pelo SICONFI."
              />
            )}
          </FormField>
        </form>
      </Modal>
    </>
  );
}
