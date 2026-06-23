// Painel das AÇÕES de atendimento de um pedido e-SIC (LAI) — gated transparencia.esic.responder.
// Decide quais transições estão disponíveis pela situação atual (atender → responder/
// indeferir/prorrogar → decidir recurso) e orquestra os modais textuais. Extraído da
// página de detalhe para mantê-la enxuta (CLAUDE.md §13: < 300 linhas/arquivo).
import { useState } from 'react';
import { Button, CardSecao, FormField, Select, Toolbar, useToast } from '../../components/ui';
import { Can } from '../../auth/Can';
import {
  useDecidirRecurso,
  useIndeferirPedido,
  useIniciarAtendimento,
  useProrrogarPedido,
  useResponderPedido,
} from './esic.api';
import type { PedidoSicDetalheInterno, ResultadoRecurso } from './esic.api';
import { resultadoRecursoOptions } from './esic.helpers';
import { EsicAcaoModal, useEsicErroToast } from './EsicAcaoModal';

type ModalAtivo = 'responder' | 'prorrogar' | 'indeferir' | 'recurso' | null;

export function EsicAcoesPanel({ pedido }: { pedido: PedidoSicDetalheInterno }) {
  const toast = useToast();
  const erroToast = useEsicErroToast();
  const [modal, setModal] = useState<ModalAtivo>(null);
  const [resultado, setResultado] = useState<ResultadoRecurso | ''>('');

  const iniciar = useIniciarAtendimento(pedido.pedidoId);
  const responder = useResponderPedido(pedido.pedidoId);
  const prorrogar = useProrrogarPedido(pedido.pedidoId);
  const indeferir = useIndeferirPedido(pedido.pedidoId);
  const decidir = useDecidirRecurso(pedido.pedidoId);

  function fechar(): void {
    setModal(null);
    setResultado('');
  }

  const podeAtender = pedido.situacao === 'Aberto';
  const podeResponderOuIndeferir =
    pedido.situacao === 'Aberto' || pedido.situacao === 'EmAtendimento';
  const podeDecidirRecurso = pedido.situacao === 'RecursoAberto';
  const semAcao = !podeAtender && !podeResponderOuIndeferir && !podeDecidirRecurso;

  return (
    <Can permission="transparencia.esic.responder">
      <CardSecao titulo="Ações de atendimento">
        <Toolbar>
          {podeAtender && (
            <Button
              variant="secondary"
              loading={iniciar.isPending}
              onClick={() =>
                iniciar.mutate(undefined, {
                  onSuccess: () => toast.success('Atendimento iniciado.', 'Sucesso'),
                  onError: (e) => erroToast(e, 'Não foi possível iniciar o atendimento.'),
                })
              }
            >
              <i className="fas fa-play" aria-hidden="true" /> Iniciar atendimento
            </Button>
          )}
          {podeResponderOuIndeferir && (
            <>
              <Button variant="primary" onClick={() => setModal('responder')}>
                <i className="fas fa-reply" aria-hidden="true" /> Responder
              </Button>
              <Button variant="secondary" onClick={() => setModal('prorrogar')}>
                <i className="fas fa-clock" aria-hidden="true" /> Prorrogar (+10 d.ú.)
              </Button>
              <Button variant="secondary" onClick={() => setModal('indeferir')}>
                <i className="fas fa-ban" aria-hidden="true" /> Indeferir
              </Button>
            </>
          )}
          {podeDecidirRecurso && (
            <Button variant="primary" onClick={() => setModal('recurso')}>
              <i className="fas fa-gavel" aria-hidden="true" /> Decidir recurso
            </Button>
          )}
        </Toolbar>
        {semAcao && (
          <p className="text-muted mb-0 mt-2">
            Não há ação pendente para este pedido na situação atual.
          </p>
        )}
      </CardSecao>

      <EsicAcaoModal
        open={modal === 'responder'}
        onClose={fechar}
        titulo="Responder pedido"
        contextoLegal="Acesso concedido — LAI art. 11. O texto será disponibilizado ao cidadão no acompanhamento público do protocolo."
        rotuloTexto="Texto da resposta"
        ajudaTexto="Resposta ao pedido de informação (até 8000 caracteres)."
        maxTexto={8000}
        rotuloConfirmar="Responder"
        enviando={responder.isPending}
        onSubmit={(texto) =>
          responder.mutate(
            { texto },
            {
              onSuccess: () => {
                toast.success('Pedido respondido.', 'Sucesso');
                fechar();
              },
              onError: (e) => erroToast(e, 'Não foi possível responder o pedido.'),
            },
          )
        }
      />

      <EsicAcaoModal
        open={modal === 'prorrogar'}
        onClose={fechar}
        titulo="Prorrogar prazo"
        contextoLegal="Prorrogação única de +10 dias úteis, mediante justificativa expressa — LAI art. 11 §2º."
        rotuloTexto="Justificativa"
        ajudaTexto="Motivo da prorrogação (obrigatório, até 2000 caracteres)."
        maxTexto={2000}
        rotuloConfirmar="Prorrogar"
        enviando={prorrogar.isPending}
        onSubmit={(motivo) =>
          prorrogar.mutate(motivo, {
            onSuccess: () => {
              toast.success('Prazo prorrogado em 10 dias úteis.', 'Sucesso');
              fechar();
            },
            onError: (e) => erroToast(e, 'Não foi possível prorrogar o prazo.'),
          })
        }
      />

      <EsicAcaoModal
        open={modal === 'indeferir'}
        onClose={fechar}
        titulo="Indeferir pedido"
        contextoLegal="Acesso negado com fundamento legal expresso — LAI art. 11 §1º. O cidadão poderá interpor recurso."
        rotuloTexto="Fundamento legal"
        ajudaTexto="Fundamento legal do indeferimento (obrigatório, até 4000 caracteres)."
        maxTexto={4000}
        rotuloConfirmar="Indeferir"
        enviando={indeferir.isPending}
        onSubmit={(fundamento) =>
          indeferir.mutate(fundamento, {
            onSuccess: () => {
              toast.success('Pedido indeferido.', 'Sucesso');
              fechar();
            },
            onError: (e) => erroToast(e, 'Não foi possível indeferir o pedido.'),
          })
        }
      />

      <EsicAcaoModal
        open={modal === 'recurso'}
        onClose={fechar}
        titulo="Decidir recurso"
        contextoLegal="Decisão do recurso administrativo interposto pelo cidadão — LAI art. 15-16."
        rotuloTexto="Decisão"
        ajudaTexto="Texto da decisão do recurso (obrigatório, até 8000 caracteres)."
        maxTexto={8000}
        rotuloConfirmar="Decidir recurso"
        enviando={decidir.isPending}
        extraValido={resultado !== ''}
        campoExtra={
          <FormField label="Resultado" required>
            {({ id, describedBy }) => (
              <Select
                id={id}
                aria-describedby={describedBy}
                placeholder="Selecione o resultado"
                options={resultadoRecursoOptions}
                value={resultado}
                onChange={(e) => setResultado(e.target.value as ResultadoRecurso | '')}
              />
            )}
          </FormField>
        }
        onSubmit={(decisao) => {
          if (resultado === '') return;
          decidir.mutate(
            { resultado, decisao },
            {
              onSuccess: () => {
                toast.success('Recurso decidido.', 'Sucesso');
                fechar();
              },
              onError: (e) => erroToast(e, 'Não foi possível decidir o recurso.'),
            },
          );
        }}
      />
    </Can>
  );
}
