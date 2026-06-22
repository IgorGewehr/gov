// Tela de DETALHE / FLUXO da prestação de contas ao TCE-RS (contrato M4):
//   gerar → validar [mostra críticas/RDI] → empacotar → baixar arquivos →
//   registrar protocolo [gated transparencia.remessa.transmitir] → (Enviada).
// + reconciliar com o SICONFI, tratando 503 ("indisponível") de forma graciosa.
// IMPORTANTE: a TRANSMISSÃO ao TCE-RS é feita FORA do sistema (PAD/e-Protocolo);
// aqui apenas se REGISTRA o protocolo retornado por aquele canal.
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Alert, Button, Card, PageHeader, QueryState, Tag, useToast } from '../../components/ui';
import { Can } from '../../auth/Can';
import { ApiError } from '../../api/problemDetails';
import { formatarData, formatarDataHora } from '../../i18n/format';
import {
  baixarArquivoRemessa,
  useEmpacotarRemessa,
  useReconciliarRemessa,
  useRemessa,
  useValidarRemessa,
} from './api';
import type { RemessaDetalhe } from './api';
import { situacaoRemessaLabel, situacaoRemessaTagVariant } from './transparencia.helpers';
import { RemessaCriticasCard } from './RemessaCriticasCard';
import { RegistrarProtocoloModal } from './RegistrarProtocoloModal';

/** Permissão (gating) do ato humano de registrar o protocolo da transmissão. */
const PERM_TRANSMITIR = 'transparencia.remessa.transmitir';

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

export function RemessaDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const toast = useToast();
  const query = useRemessa(id);

  const validar = useValidarRemessa(id);
  const empacotar = useEmpacotarRemessa(id);
  const reconciliar = useReconciliarRemessa(id);

  const [protocoloAberto, setProtocoloAberto] = useState(false);
  const [baixando, setBaixando] = useState<string | null>(null);

  function executar(
    mutation: typeof validar | typeof empacotar,
    sucesso: string,
    erroPadrao: string,
  ): void {
    mutation.mutate(undefined, {
      onSuccess: () => toast.success(sucesso, 'Sucesso'),
      onError: (error) =>
        toast.error(error instanceof ApiError ? error.userMessage : erroPadrao),
    });
  }

  async function baixar(arquivo: string): Promise<void> {
    setBaixando(arquivo);
    try {
      await baixarArquivoRemessa(id, arquivo);
    } catch (error) {
      toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível baixar o arquivo.');
    } finally {
      setBaixando(null);
    }
  }

  function reconciliarSiconfi(): void {
    reconciliar.mutate(undefined, {
      onSuccess: (resultado) => {
        if (resultado.indisponivel) {
          toast.warning(
            'SICONFI indisponível no momento. Tente a reconciliação novamente mais tarde.',
            'Serviço indisponível',
          );
          return;
        }
        if (resultado.conforme) {
          toast.success('Reconciliação concluída: dados conformes com o SICONFI.', 'Conforme');
        } else {
          toast.warning(
            `Reconciliação concluída com ${resultado.divergencias ?? 0} divergência(s).`,
            'Divergências encontradas',
          );
        }
      },
      onError: (error) =>
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível reconciliar com o SICONFI.'),
    });
  }

  const emAcao = validar.isPending || empacotar.isPending;

  return (
    <>
      <PageHeader
        title="Detalhe da Remessa (TCE-RS)"
        actions={
          <Link className="br-button secondary" to="/transparencia">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<RemessaDetalhe>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
      >
        {(remessa) => {
          const validada = remessa.situacao !== 'Gerada';
          const temErros = remessa.totalErros > 0;
          const podeEmpacotar = remessa.situacao === 'Validada' && !temErros;
          const podeRegistrar = remessa.situacao === 'ProntaParaTransmissao';
          return (
            <>
              <Card
                header={<strong>Remessa {remessa.periodo}</strong>}
                footer={
                  <Can permission="transparencia.gerenciar">
                    <div className="d-flex flex-wrap gap-2">
                      <Button
                        variant="secondary"
                        loading={validar.isPending}
                        disabled={emAcao || remessa.situacao !== 'Gerada'}
                        onClick={() =>
                          executar(validar, 'Remessa validada. Veja o RDI abaixo.', 'Não foi possível validar a remessa.')
                        }
                      >
                        <i className="fas fa-clipboard-check" aria-hidden="true" /> Validar
                      </Button>
                      <Button
                        variant="primary"
                        loading={empacotar.isPending}
                        disabled={emAcao || !podeEmpacotar}
                        onClick={() =>
                          executar(empacotar, 'Pacote ZIP gerado. Pronto para transmissão.', 'Não foi possível empacotar a remessa.')
                        }
                      >
                        <i className="fas fa-box-archive" aria-hidden="true" /> Empacotar
                      </Button>
                      <Can permission={PERM_TRANSMITIR}>
                        <Button
                          variant="primary"
                          disabled={emAcao || !podeRegistrar}
                          onClick={() => setProtocoloAberto(true)}
                        >
                          <i className="fas fa-stamp" aria-hidden="true" /> Registrar protocolo
                        </Button>
                      </Can>
                      <Button
                        variant="secondary"
                        loading={reconciliar.isPending}
                        disabled={!validada}
                        onClick={reconciliarSiconfi}
                      >
                        <i className="fas fa-scale-balanced" aria-hidden="true" /> Reconciliar (SICONFI)
                      </Button>
                    </div>
                  </Can>
                }
              >
                <Alert variant="info" title="A transmissão ao TCE-RS é feita fora do sistema">
                  Após empacotar, baixe o pacote ZIP e transmita-o pelo PAD / e-Protocolo. Em
                  seguida, use “Registrar protocolo” para informar o número retornado — este
                  sistema não transmite, apenas registra o protocolo.
                </Alert>

                {temErros && (
                  <div className="mt-3">
                    <Alert variant="danger" title="Remessa com erros de validação">
                      O RDI apontou {remessa.totalErros} {remessa.totalErros === 1 ? 'erro' : 'erros'}. O
                      empacotamento está bloqueado até a correção e regeração do pacote.
                    </Alert>
                  </div>
                )}

                <dl className="row mt-3">
                  <Campo rotulo="Situação">
                    <Tag variant={situacaoRemessaTagVariant(remessa.situacao)}>
                      {situacaoRemessaLabel[remessa.situacao]}
                    </Tag>
                  </Campo>
                  <Campo rotulo="Exercício">{remessa.exercicio}</Campo>
                  <Campo rotulo="Período">{remessa.periodo}</Campo>
                  <Campo rotulo="Leiaute">{remessa.leiaute}</Campo>
                  <Campo rotulo="Arquivo (ZIP)">{remessa.nomeArquivoZip ?? '—'}</Campo>
                  <Campo rotulo="Protocolo">{remessa.protocolo ?? '—'}</Campo>
                  <Campo rotulo="Hash de integridade">{remessa.hashIntegridade ?? '—'}</Campo>
                  <Campo rotulo="Data de geração">{formatarDataHora(remessa.dataGeracao)}</Campo>
                  <Campo rotulo="Prazo legal">{formatarData(remessa.dataLimite)}</Campo>
                  <Campo rotulo="Data de envio">{formatarDataHora(remessa.dataEnvio)}</Campo>
                  <Campo rotulo="Erros (RDI)">{remessa.totalErros}</Campo>
                  <Campo rotulo="Alertas (RDI)">{remessa.totalAlertas}</Campo>
                </dl>

                {remessa.arquivos.length > 0 && (
                  <div className="mt-2">
                    <h3 className="text-up-01 mb-2">Arquivos do pacote</h3>
                    <ul className="br-list">
                      {remessa.arquivos.map((arq) => (
                        <li key={arq.nome} className="d-flex align-items-center justify-content-between">
                          <span>{arq.nome}</span>
                          <Button
                            variant="tertiary"
                            loading={baixando === arq.nome}
                            onClick={() => baixar(arq.nome)}
                          >
                            <i className="fas fa-download" aria-hidden="true" /> Baixar
                          </Button>
                        </li>
                      ))}
                    </ul>
                  </div>
                )}
              </Card>

              <RemessaCriticasCard id={id} habilitado={validada} />

              <RegistrarProtocoloModal
                id={id}
                open={protocoloAberto}
                onClose={() => setProtocoloAberto(false)}
              />
            </>
          );
        }}
      </QueryState>
    </>
  );
}
