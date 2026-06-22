// Barra de ACOES da Proposicao (gated por "legislativo.gerenciar"). Concentra os
// botoes de workflow e o estado de abertura dos modais correspondentes, mantendo a
// DetailPage enxuta. Cada acao cobre um endpoint POST do agregado Proposicao.
import { useState } from 'react';
import { Button, useToast } from '../../components/ui';
import { Can } from '../../auth/Can';
import { ConfirmarAcaoModal } from './ConfirmarAcaoModal';
import {
  EmendaModal,
  ParecerModal,
  DeliberacaoModal,
  AutografoModal,
} from './ProposicaoAcaoModais';
import {
  useDistribuirProposicao,
  useIncluirProposicaoEmOrdemDoDia,
  useArquivarProposicao,
} from './proposicao.api';
import { mensagemErro } from './legislativoAcao.shared';

type ModalAtivo =
  | null
  | 'emenda'
  | 'parecer'
  | 'aprovar'
  | 'rejeitar'
  | 'autografo'
  | 'distribuir'
  | 'ordemDoDia'
  | 'arquivar';

export function ProposicaoAcoes({ id }: { id: string }) {
  const toast = useToast();
  const [ativo, setAtivo] = useState<ModalAtivo>(null);
  const fechar = () => setAtivo(null);

  const distribuir = useDistribuirProposicao(id);
  const incluir = useIncluirProposicaoEmOrdemDoDia(id);
  const arquivar = useArquivarProposicao(id);

  function incluirOrdemDoDia(): void {
    incluir.mutate(undefined, {
      onSuccess: () => toast.success('Proposição incluída em Ordem do Dia.', 'Sucesso'),
      onError: (error) => toast.error(mensagemErro(error, 'Não foi possível incluir em Ordem do Dia.')),
    });
  }

  return (
    <Can permission="legislativo.gerenciar">
      <div className="d-flex flex-wrap gap-2 mb-4" role="group" aria-label="Ações da proposição">
        <Button variant="secondary" onClick={() => setAtivo('distribuir')}>
          Distribuir
        </Button>
        <Button variant="secondary" onClick={() => setAtivo('emenda')}>
          Apresentar emenda
        </Button>
        <Button variant="secondary" onClick={() => setAtivo('parecer')}>
          Registrar parecer
        </Button>
        <Button variant="secondary" onClick={incluirOrdemDoDia} loading={incluir.isPending}>
          Incluir em Ordem do Dia
        </Button>
        <Button variant="secondary" onClick={() => setAtivo('aprovar')}>
          Aprovar
        </Button>
        <Button variant="secondary" onClick={() => setAtivo('rejeitar')}>
          Rejeitar
        </Button>
        <Button variant="secondary" onClick={() => setAtivo('autografo')}>
          Gerar autógrafo
        </Button>
        <Button variant="danger" onClick={() => setAtivo('arquivar')}>
          Arquivar
        </Button>
      </div>

      <EmendaModal open={ativo === 'emenda'} onClose={fechar} id={id} />
      <ParecerModal open={ativo === 'parecer'} onClose={fechar} id={id} />
      <DeliberacaoModal open={ativo === 'aprovar'} onClose={fechar} id={id} sentido="aprovar" />
      <DeliberacaoModal open={ativo === 'rejeitar'} onClose={fechar} id={id} sentido="rejeitar" />
      <AutografoModal open={ativo === 'autografo'} onClose={fechar} id={id} />

      <ConfirmarAcaoModal
        open={ativo === 'distribuir'}
        onClose={fechar}
        title="Distribuir proposição"
        mensagem="Confirma a distribuição da proposição às comissões competentes?"
        rotuloConfirmar="Distribuir"
        mutation={distribuir}
        sucesso="Proposição distribuída."
        erroFallback="Não foi possível distribuir a proposição."
      />
      <ConfirmarAcaoModal
        open={ativo === 'arquivar'}
        onClose={fechar}
        title="Arquivar proposição"
        mensagem="Confirma o arquivamento da proposição? A matéria deixará de tramitar."
        rotuloConfirmar="Arquivar"
        variante="danger"
        mutation={arquivar}
        sucesso="Proposição arquivada."
        erroFallback="Não foi possível arquivar a proposição."
      />
    </Can>
  );
}
