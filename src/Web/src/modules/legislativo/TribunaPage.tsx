// TRIBUNA — painel de oradores inscritos de uma sessao com CRONOMETRO visual e
// controles (iniciar/pausar/retomar/encerrar) gated por `legislativo.tribuna.controlar`.
// Informa-se a sessao; o painel faz polling enquanto a sessao estiver Aberta.
import { useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import {
  Button,
  Card,
  EmptyState,
  FormField,
  Input,
  PageHeader,
  QueryState,
  Spinner,
} from '../../components/ui';
import { Can } from '../../auth/Can';
import { useToast } from '../../components/ui';
import { mensagemErro } from './legislativoAcao.shared';
import { LegislativoSecoesNav } from './LegislativoSecoesNav';
import { TribunaCronometro } from './TribunaCronometro';
import { TribunaInscricaoModal } from './TribunaInscricaoModal';
import { useSessao } from './sessao.api';
import { useVereadores } from './vereadores.api';
import {
  useEncerrarFala,
  useIniciarFala,
  usePausarFala,
  useRetomarFala,
  useTribuna,
} from './tribuna.api';
import type { InscricaoResumo, TribunaPainel } from './tribuna.api';

/** Controles do cronometro de uma inscricao, dependentes da sua situacao. */
function ControlesInscricao({
  sessaoId,
  tribunaId,
  inscricao,
  nome,
}: {
  sessaoId: string;
  tribunaId: string;
  inscricao: InscricaoResumo;
  nome: string;
}) {
  const toast = useToast();
  const iniciar = useIniciarFala(sessaoId);
  const pausar = usePausarFala(sessaoId);
  const retomar = useRetomarFala(sessaoId);
  const encerrar = useEncerrarFala(sessaoId);

  function disparar(mutation: ReturnType<typeof useIniciarFala>, falha: string): void {
    mutation.mutate(
      { inscricaoId: inscricao.inscricaoId, tribunaId },
      {
        onError: (error) => toast.error(mensagemErro(error, falha)),
      },
    );
  }

  // Espelha o enum SituacaoInscricao do backend; a pausa e um flag (Pausada) sobre EmUso.
  const encerrada = inscricao.situacao === 'Concluido' || inscricao.situacao === 'Cancelado';
  const emUso = inscricao.situacao === 'EmUso' && !inscricao.pausada;
  const pausada = inscricao.situacao === 'EmUso' && inscricao.pausada;
  const aguardando = inscricao.situacao === 'Inscrito';
  const ocupado = iniciar.isPending || pausar.isPending || retomar.isPending || encerrar.isPending;

  if (encerrada) return null;

  return (
    <div className="d-flex flex-wrap gap-2" role="group" aria-label={`Controles de ${nome}`}>
      {aguardando && (
        <Button variant="primary" onClick={() => disparar(iniciar, 'Não foi possível iniciar a fala.')} loading={iniciar.isPending} disabled={ocupado}>
          <i className="fas fa-play" aria-hidden="true" /> Iniciar
        </Button>
      )}
      {emUso && (
        <Button variant="secondary" onClick={() => disparar(pausar, 'Não foi possível pausar a fala.')} loading={pausar.isPending} disabled={ocupado}>
          <i className="fas fa-pause" aria-hidden="true" /> Pausar
        </Button>
      )}
      {pausada && (
        <Button variant="primary" onClick={() => disparar(retomar, 'Não foi possível retomar a fala.')} loading={retomar.isPending} disabled={ocupado}>
          <i className="fas fa-play" aria-hidden="true" /> Retomar
        </Button>
      )}
      {(emUso || pausada) && (
        <Button variant="danger" onClick={() => disparar(encerrar, 'Não foi possível encerrar a fala.')} loading={encerrar.isPending} disabled={ocupado}>
          <i className="fas fa-stop" aria-hidden="true" /> Encerrar
        </Button>
      )}
    </div>
  );
}

function ListaOradores({ painel }: { painel: TribunaPainel }) {
  // O backend nao envia o nome do orador no painel — resolvemos via cadastro.
  const vereadores = useVereadores();
  const nomePorVereador = useMemo(
    () => new Map((vereadores.data ?? []).map((v) => [v.id, v.nomeParlamentar])),
    [vereadores.data],
  );

  const inscricoes = useMemo(
    () => [...painel.inscricoes].sort((a, b) => a.ordem - b.ordem),
    [painel.inscricoes],
  );

  if (inscricoes.length === 0) {
    return (
      <EmptyState
        icon="fas fa-microphone-slash"
        title="Nenhum orador inscrito"
        description="Inscreva oradores para iniciar o uso da palavra na Tribuna."
      />
    );
  }

  return (
    <ol className="p-0" style={{ listStyle: 'none' }}>
      {inscricoes.map((inscricao) => {
        const nome = nomePorVereador.get(inscricao.vereadorId) ?? inscricao.vereadorId;
        return (
          <li key={inscricao.inscricaoId}>
            <Card className="mb-3">
              <div className="d-flex flex-wrap justify-content-between align-items-center" style={{ gap: '1rem' }}>
                <div>
                  <span className="text-gray-60">{inscricao.ordem}.</span> <strong>{nome}</strong>
                </div>
                <TribunaCronometro inscricao={inscricao} />
              </div>
              <Can permission="legislativo.tribuna.controlar">
                <div className="mt-3">
                  <ControlesInscricao
                    sessaoId={painel.sessaoId}
                    tribunaId={painel.tribunaId}
                    inscricao={inscricao}
                    nome={nome}
                  />
                </div>
              </Can>
            </Card>
          </li>
        );
      })}
    </ol>
  );
}

export function TribunaPage() {
  const [sessaoInput, setSessaoInput] = useState('');
  const [sessaoId, setSessaoId] = useState('');
  const [inscricaoAberta, setInscricaoAberta] = useState(false);

  // A situacao da sessao nao vem no painel da tribuna — buscamos via useSessao
  // para controlar o polling e habilitar/desabilitar a inscricao de oradores.
  const sessao = useSessao(sessaoId);
  const sessaoAberta = sessao.data?.situacao === 'Aberta';
  const painel = useTribuna(sessaoId, sessaoAberta);

  function carregar(event: FormEvent): void {
    event.preventDefault();
    setSessaoId(sessaoInput.trim());
  }

  return (
    <>
      <PageHeader
        title="Tribuna"
        description="Controle o uso da palavra dos oradores inscritos com cronômetro em tempo real."
        actions={
          sessaoId !== '' ? (
            <Can permission="legislativo.tribuna.controlar">
              <Button variant="primary" onClick={() => setInscricaoAberta(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Inscrever orador
              </Button>
            </Can>
          ) : undefined
        }
      />

      <LegislativoSecoesNav />

      <Card className="mb-4">
        <form className="br-form" onSubmit={carregar}>
          <div className="row align-items-end">
            <div className="col">
              <FormField label="Identificador da sessão" required>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={sessaoInput}
                    onChange={(e) => setSessaoInput(e.target.value)}
                    placeholder="00000000-0000-0000-0000-000000000000"
                  />
                )}
              </FormField>
            </div>
            <div className="col-auto mb-3">
              <Button variant="primary" type="submit" disabled={sessaoInput.trim() === ''}>
                Carregar tribuna
              </Button>
            </div>
          </div>
        </form>
      </Card>

      {sessaoId === '' ? (
        <EmptyState
          icon="fas fa-comment-dots"
          title="Informe uma sessão"
          description="Digite o identificador da sessão para acompanhar a Tribuna."
        />
      ) : (
        <>
          {painel.isFetching && (
            <p className="text-gray-60 d-flex align-items-center mb-3" style={{ gap: '0.5rem' }}>
              <Spinner medium={false} label="Atualizando painel" /> Atualizando painel…
            </p>
          )}
          <QueryState<TribunaPainel>
            isLoading={painel.isLoading}
            isError={painel.isError}
            error={painel.error}
            data={painel.data}
          >
            {(dados) => <ListaOradores painel={dados} />}
          </QueryState>

          <TribunaInscricaoModal
            open={inscricaoAberta}
            onClose={() => setInscricaoAberta(false)}
            sessaoId={sessaoId}
            tribunaId={painel.data?.tribunaId ?? ''}
          />
        </>
      )}
    </>
  );
}
