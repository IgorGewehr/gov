// Modal de GERAÇÃO dos eventos PERIÓDICOS do eSocial a partir de uma FOLHA fechada:
// S-1200/1202 (remuneração), S-1210 (pagamentos) e S-1299 (fechamento). A folha é
// localizada por competência; cada ação só fica disponível quando a situação permite
// (remuneração/fechamento exigem Fechada/Paga; pagamentos exige Paga).
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Alert,
  Button,
  FormField,
  Input,
  Modal,
  Select,
  Tag,
  useToast,
} from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { MESES, situacaoFolhaTagVariant } from './recursosHumanos.helpers';
import { useFolhaPorCompetencia } from './folha.api';
import {
  useGerarFechamentoFolha,
  useGerarPagamentosFolha,
  useGerarRemuneracaoFolha,
} from './esocial.api';

const ANO_ATUAL = new Date().getFullYear();

export interface GerarPeriodicosFolhaModalProps {
  open: boolean;
  onClose: () => void;
}

export function GerarPeriodicosFolhaModal({ open, onClose }: GerarPeriodicosFolhaModalProps) {
  const toast = useToast();
  const [ano, setAno] = useState(String(ANO_ATUAL));
  const [mes, setMes] = useState(String(new Date().getMonth() + 1));
  const [consulta, setConsulta] = useState<{ ano: number; mes: number } | null>(null);

  const folha = useFolhaPorCompetencia(consulta?.ano ?? 0, consulta?.mes ?? 0, consulta !== null);
  const remuneracao = useGerarRemuneracaoFolha();
  const pagamentos = useGerarPagamentosFolha();
  const fechamento = useGerarFechamentoFolha();

  const dados = folha.data ?? null;
  const fechada = dados?.situacao === 'Fechada' || dados?.situacao === 'Paga';
  const paga = dados?.situacao === 'Paga';

  function consultar(event: FormEvent): void {
    event.preventDefault();
    setConsulta({ ano: Number(ano), mes: Number(mes) });
  }

  function aoErro(error: unknown): void {
    toast.error(
      error instanceof ApiError ? error.userMessage : 'Não foi possível gerar os eventos.',
    );
  }

  function gerarRemuneracao(): void {
    if (!dados) return;
    remuneracao.mutate(dados.id, {
      onSuccess: (r) => toast.success(`${r.ids.length} evento(s) S-1200/1202 gerado(s).`, 'Sucesso'),
      onError: aoErro,
    });
  }

  function gerarPagamentos(): void {
    if (!dados) return;
    pagamentos.mutate(dados.id, {
      onSuccess: (r) => toast.success(`${r.ids.length} evento(s) S-1210 gerado(s).`, 'Sucesso'),
      onError: aoErro,
    });
  }

  function gerarFechamento(): void {
    if (!dados) return;
    fechamento.mutate(dados.id, {
      onSuccess: () => toast.success('Evento S-1299 (fechamento) gerado.', 'Sucesso'),
      onError: aoErro,
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Gerar periódicos da folha"
      size="large"
      footer={
        <Button variant="secondary" onClick={onClose}>
          Fechar
        </Button>
      }
    >
      <Alert variant="info" title="Eventos periódicos do eSocial.">
        Os eventos S-1200/1202 (remuneração), S-1210 (pagamentos) e S-1299 (fechamento) são
        gerados a partir de uma folha já <strong>Fechada</strong> (a folha é a fonte de verdade
        das verbas). Os pagamentos (S-1210) exigem a folha <strong>Paga</strong>.
      </Alert>

      <form className="br-form mt-3" onSubmit={consultar}>
        <div className="row align-items-end">
          <div className="col-sm-5">
            <FormField label="Mês" required>
              {({ id, describedBy }) => (
                <Select
                  id={id}
                  aria-describedby={describedBy}
                  value={mes}
                  onChange={(e) => setMes(e.target.value)}
                  options={MESES}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-4">
            <FormField label="Ano" required>
              {({ id, describedBy }) => (
                <Input
                  id={id}
                  type="number"
                  min="2000"
                  max="2100"
                  step="1"
                  inputMode="numeric"
                  aria-describedby={describedBy}
                  value={ano}
                  onChange={(e) => setAno(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-auto mb-3">
            <Button variant="primary" type="submit" loading={folha.isFetching}>
              Localizar folha
            </Button>
          </div>
        </div>
      </form>

      {consulta !== null && !folha.isFetching && dados === null && (
        <Alert variant="warning" title="Folha não encontrada.">
          Não há folha para a competência informada. Abra/feche a folha na aba Folha de Pagamento.
        </Alert>
      )}

      {dados && (
        <div className="mt-2">
          <p className="mb-2">
            Folha <strong>{dados.competencia}</strong> —{' '}
            <Tag variant={situacaoFolhaTagVariant(dados.situacao)}>{dados.situacao}</Tag>
          </p>

          {!fechada && (
            <Alert variant="warning" title="Folha não fechada.">
              A geração de eventos periódicos exige a folha Fechada. Feche-a primeiro.
            </Alert>
          )}

          <div className="d-flex flex-wrap" style={{ gap: '0.5rem' }}>
            <Button
              variant="primary"
              onClick={gerarRemuneracao}
              disabled={!fechada}
              loading={remuneracao.isPending}
            >
              Gerar S-1200/1202 (remuneração)
            </Button>
            <Button
              variant="primary"
              onClick={gerarPagamentos}
              disabled={!paga}
              loading={pagamentos.isPending}
            >
              Gerar S-1210 (pagamentos)
            </Button>
            <Button
              variant="primary"
              onClick={gerarFechamento}
              disabled={!fechada}
              loading={fechamento.isPending}
            >
              Gerar S-1299 (fechamento)
            </Button>
          </div>

          {!paga && fechada && (
            <p className="text-down-01 text-gray-60 mt-2 mb-0">
              S-1210 (pagamentos) ficará disponível após o pagamento da folha.
            </p>
          )}
        </div>
      )}
    </Modal>
  );
}
