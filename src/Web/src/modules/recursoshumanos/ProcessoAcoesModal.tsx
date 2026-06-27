// Modal de movimentações de um processo trabalhista: reavaliar prognóstico (em andamento), homologar
// acordo, registrar condenação, registrar improcedência e arquivar (encerrados). Cada ação respeita a
// situação atual; o backend é a fonte da verdade das invariantes (datas/valores/estados).
import { useState } from 'react';
import { Button, FormField, Modal, Select, Input, Tag, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { formatarMoeda } from '../../i18n/format';
import { useMovimentarProcesso } from './api';
import type { ProcessoTrabalhistaResumo } from './api';
import {
  PROGNOSTICOS_PERDA,
  prognosticoTagVariant,
  situacaoProcessoTagVariant,
} from './recursosHumanos.helpers';

type Acao = 'prognostico' | 'acordo' | 'condenacao' | 'improcedencia' | 'arquivar';

export function ProcessoAcoesModal({
  processo,
  onClose,
}: {
  processo: ProcessoTrabalhistaResumo;
  onClose: () => void;
}) {
  const toast = useToast();
  const acoes = useMovimentarProcesso(processo.id);
  const [acao, setAcao] = useState<Acao | null>(null);
  const [valor, setValor] = useState('');
  const [data, setData] = useState(new Date().toISOString().slice(0, 10));
  const [prognostico, setPrognostico] = useState('2');

  const emAndamento = processo.situacao === 'EmAndamento';
  const encerrado = ['Acordo', 'Condenado', 'Improcedente'].includes(processo.situacao);

  function sucesso(mensagem: string): void {
    toast.success(mensagem, 'Sucesso');
    onClose();
  }

  function falha(error: unknown): void {
    toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível movimentar.');
  }

  function executar(): void {
    if (acao === 'prognostico') {
      acoes.reavaliar.mutate(Number(prognostico), {
        onSuccess: () => sucesso('Prognóstico reavaliado.'),
        onError: falha,
      });
    } else if (acao === 'acordo') {
      acoes.acordo.mutate(
        { valor: Number(valor), data },
        { onSuccess: () => sucesso('Acordo homologado.'), onError: falha },
      );
    } else if (acao === 'condenacao') {
      acoes.condenacao.mutate(
        { valor: Number(valor), data },
        { onSuccess: () => sucesso('Condenação registrada.'), onError: falha },
      );
    } else if (acao === 'improcedencia') {
      acoes.improcedencia.mutate(data, {
        onSuccess: () => sucesso('Improcedência registrada.'),
        onError: falha,
      });
    } else if (acao === 'arquivar') {
      acoes.arquivar.mutate(undefined, {
        onSuccess: () => sucesso('Processo arquivado.'),
        onError: falha,
      });
    }
  }

  const pendente =
    acoes.reavaliar.isPending ||
    acoes.acordo.isPending ||
    acoes.condenacao.isPending ||
    acoes.improcedencia.isPending ||
    acoes.arquivar.isPending;

  return (
    <Modal
      open
      onClose={onClose}
      title={`Processo ${processo.numeroProcesso}`}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={pendente}>
            Fechar
          </Button>
          {acao && (
            <Button variant="primary" onClick={executar} loading={pendente}>
              Confirmar
            </Button>
          )}
        </>
      }
    >
      <dl className="row mb-3">
        <dt className="col-sm-4">Reclamante</dt>
        <dd className="col-sm-8">{processo.reclamante}</dd>
        <dt className="col-sm-4">Situação</dt>
        <dd className="col-sm-8">
          <Tag variant={situacaoProcessoTagVariant(processo.situacao)}>{processo.situacao}</Tag>
        </dd>
        <dt className="col-sm-4">Prognóstico</dt>
        <dd className="col-sm-8">
          <Tag variant={prognosticoTagVariant(processo.prognostico)}>{processo.prognostico}</Tag>
        </dd>
        <dt className="col-sm-4">Provisionado</dt>
        <dd className="col-sm-8">{formatarMoeda(processo.valorProvisionado)}</dd>
      </dl>

      <div className="d-flex flex-wrap mb-3" style={{ gap: '0.5rem' }}>
        {emAndamento && (
          <>
            <Button variant="secondary" size="sm" onClick={() => setAcao('prognostico')}>
              Reavaliar prognóstico
            </Button>
            <Button variant="secondary" size="sm" onClick={() => setAcao('acordo')}>
              Homologar acordo
            </Button>
            <Button variant="secondary" size="sm" onClick={() => setAcao('condenacao')}>
              Registrar condenação
            </Button>
            <Button variant="secondary" size="sm" onClick={() => setAcao('improcedencia')}>
              Improcedência
            </Button>
          </>
        )}
        {encerrado && (
          <Button variant="secondary" size="sm" onClick={() => setAcao('arquivar')}>
            Arquivar
          </Button>
        )}
        {!emAndamento && !encerrado && (
          <span className="text-secondary">Processo arquivado — sem movimentações.</span>
        )}
      </div>

      {acao === 'prognostico' && (
        <FormField label="Novo prognóstico">
          {({ id }) => (
            <Select
              id={id}
              value={prognostico}
              onChange={(e) => setPrognostico(e.target.value)}
              options={PROGNOSTICOS_PERDA}
            />
          )}
        </FormField>
      )}

      {(acao === 'acordo' || acao === 'condenacao') && (
        <div className="row">
          <div className="col-md-6">
            <FormField label="Valor (R$)">
              {({ id }) => (
                <Input
                  id={id}
                  type="number"
                  min="0"
                  step="0.01"
                  value={valor}
                  onChange={(e) => setValor(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-md-6">
            <FormField label="Data">
              {({ id }) => (
                <Input id={id} type="date" value={data} onChange={(e) => setData(e.target.value)} />
              )}
            </FormField>
          </div>
        </div>
      )}

      {acao === 'improcedencia' && (
        <FormField label="Data do trânsito em julgado">
          {({ id }) => (
            <Input id={id} type="date" value={data} onChange={(e) => setData(e.target.value)} />
          )}
        </FormField>
      )}
    </Modal>
  );
}
