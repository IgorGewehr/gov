// Tela GIA MENSAL DE ISS — declaração do prestador (PARIDADE-PoC SW-A10). Seleciona o
// contribuinte e a competência, escritura os serviços prestados (item LC 116, base,
// alíquota, retenção) e ENTREGA a declaração — constituindo o lançamento do ISS devido.
// Cobre serviços SEM NFS-e nacional. Gated por "tributos.gerenciar".
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Alert,
  Button,
  Card,
  DataTable,
  FormField,
  Input,
  PageHeader,
  Toolbar,
  useToast,
  type Column,
} from '../../components/ui';
import { Can } from '../../auth/Can';
import { ApiError } from '../../api/problemDetails';
import { formatarMoeda } from '../../i18n/format';
import { TributosSubNav } from './TributosSubNav';
import { useEntregarGia, type ResultadoGiaIss, type ServicoGiaInput } from './gia.api';

const PERM_GERENCIAR = 'tributos.gerenciar';

function competenciaAtual(): string {
  const agora = new Date();
  return `${agora.getFullYear()}-${String(agora.getMonth() + 1).padStart(2, '0')}`;
}

const SERVICO_VAZIO: ServicoGiaInput = {
  itemListaServico: '',
  descricao: '',
  baseCalculo: 0,
  aliquotaPercentual: 0,
  retidoNaFonte: false,
};

export function GiaIssPage() {
  const toast = useToast();
  const [contribuinteId, setContribuinteId] = useState('');
  const [competencia, setCompetencia] = useState(competenciaAtual());
  const [fundamentoLegal, setFundamentoLegal] = useState(
    'LC 116/2003; Código Tributário Municipal (obrigação acessória).',
  );
  const [vencimento, setVencimento] = useState('');
  const [servicos, setServicos] = useState<ServicoGiaInput[]>([{ ...SERVICO_VAZIO }]);
  const [resultado, setResultado] = useState<ResultadoGiaIss | null>(null);

  const entregar = useEntregarGia(contribuinteId.trim());

  function atualizarServico(indice: number, patch: Partial<ServicoGiaInput>): void {
    setServicos((atual) => atual.map((s, i) => (i === indice ? { ...s, ...patch } : s)));
  }

  function adicionarServico(): void {
    setServicos((atual) => [...atual, { ...SERVICO_VAZIO }]);
  }

  function removerServico(indice: number): void {
    setServicos((atual) => (atual.length > 1 ? atual.filter((_, i) => i !== indice) : atual));
  }

  function entregarGia(event: FormEvent): void {
    event.preventDefault();
    if (contribuinteId.trim() === '') {
      toast.error('Informe o identificador do contribuinte.');
      return;
    }
    if (!/^\d{4}-\d{2}$/.test(competencia)) {
      toast.error('Informe a competência no formato AAAA-MM.');
      return;
    }
    if (vencimento.trim() === '') {
      toast.error('Informe o vencimento do ISS devido.');
      return;
    }
    const validos = servicos.filter((s) => s.itemListaServico.trim() !== '' && s.descricao.trim() !== '');
    if (validos.length === 0) {
      toast.error('Declare ao menos um serviço (item e descrição).');
      return;
    }
    const [ano, mes] = competencia.split('-').map(Number);
    entregar.mutate(
      { ano, mes, fundamentoLegal: fundamentoLegal.trim(), vencimentoIssDevido: vencimento.trim(), servicos: validos },
      {
        onSuccess: (r) => {
          setResultado(r);
          toast.success(`GIA da competência ${competencia} entregue.`, 'Sucesso');
        },
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível entregar a GIA.',
          ),
      },
    );
  }

  const totalBase = servicos.reduce((soma, s) => soma + (Number(s.baseCalculo) || 0), 0);

  const colunas: Column<ServicoGiaInput & { indice: number }>[] = [
    {
      key: 'item',
      header: 'Item LC 116',
      render: (s) => (
        <Input
          aria-label={`Item do serviço ${s.indice + 1}`}
          value={s.itemListaServico}
          onChange={(e) => atualizarServico(s.indice, { itemListaServico: e.target.value })}
          placeholder="7.02"
        />
      ),
    },
    {
      key: 'descricao',
      header: 'Descrição',
      render: (s) => (
        <Input
          aria-label={`Descrição do serviço ${s.indice + 1}`}
          value={s.descricao}
          onChange={(e) => atualizarServico(s.indice, { descricao: e.target.value })}
        />
      ),
    },
    {
      key: 'base',
      header: 'Base (R$)',
      render: (s) => (
        <Input
          type="number"
          step="0.01"
          min="0"
          aria-label={`Base de cálculo do serviço ${s.indice + 1}`}
          value={String(s.baseCalculo)}
          onChange={(e) => atualizarServico(s.indice, { baseCalculo: Number(e.target.value) })}
        />
      ),
    },
    {
      key: 'aliquota',
      header: 'Alíquota (%)',
      render: (s) => (
        <Input
          type="number"
          step="0.01"
          min="0"
          max="100"
          aria-label={`Alíquota do serviço ${s.indice + 1}`}
          value={String(s.aliquotaPercentual)}
          onChange={(e) => atualizarServico(s.indice, { aliquotaPercentual: Number(e.target.value) })}
        />
      ),
    },
    {
      key: 'retido',
      header: 'Retido?',
      render: (s) => (
        <input
          type="checkbox"
          aria-label={`ISS retido na fonte do serviço ${s.indice + 1}`}
          checked={s.retidoNaFonte}
          onChange={(e) => atualizarServico(s.indice, { retidoNaFonte: e.target.checked })}
        />
      ),
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (s) => (
        <Button variant="secondary" size="sm" onClick={() => removerServico(s.indice)}>
          <i className="fas fa-trash" aria-hidden="true" /> Remover
        </Button>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="GIA mensal de ISS"
        description="Declaração mensal do prestador: escriture os serviços prestados e entregue a GIA — constitui o lançamento do ISS devido."
      />

      <TributosSubNav />

      <Alert variant="info" title="Complementar à apuração por NFS-e">
        A GIA cobre serviços sem NFS-e nacional, que a apuração derivada do ADN não alcança. O ISS retido
        na fonte pelo tomador não compõe o devido próprio.
      </Alert>

      <Card className="mb-4">
        <form className="br-form" onSubmit={entregarGia}>
          <div className="row align-items-end">
            <div className="col-md-5">
              <FormField label="Identificador do contribuinte" required>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={contribuinteId}
                    onChange={(e) => setContribuinteId(e.target.value)}
                    placeholder="00000000-0000-0000-0000-000000000000"
                  />
                )}
              </FormField>
            </div>
            <div className="col-md-3">
              <FormField label="Competência" required help="Mês de referência (AAAA-MM).">
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    type="month"
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={competencia}
                    onChange={(e) => setCompetencia(e.target.value)}
                  />
                )}
              </FormField>
            </div>
            <div className="col-md-4">
              <FormField label="Vencimento do ISS devido" required>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    type="date"
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={vencimento}
                    onChange={(e) => setVencimento(e.target.value)}
                  />
                )}
              </FormField>
            </div>
          </div>

          <div className="row">
            <div className="col-12">
              <FormField label="Fundamento legal" required>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={fundamentoLegal}
                    onChange={(e) => setFundamentoLegal(e.target.value)}
                  />
                )}
              </FormField>
            </div>
          </div>

          <DataTable
            caption="Serviços declarados na GIA"
            columns={colunas}
            rows={servicos.map((s, indice) => ({ ...s, indice }))}
            rowKey={(s) => String(s.indice)}
          />

          <p className="text-gray-60">Total dos serviços: {formatarMoeda(totalBase)}</p>

          <Can permission={PERM_GERENCIAR}>
            <Toolbar>
              <Button variant="secondary" onClick={adicionarServico}>
                <i className="fas fa-plus" aria-hidden="true" /> Adicionar serviço
              </Button>
              <Button variant="primary" type="submit" loading={entregar.isPending}>
                <i className="fas fa-paper-plane" aria-hidden="true" /> Entregar GIA
              </Button>
            </Toolbar>
          </Can>
        </form>
      </Card>

      {resultado && (
        <Card header={<strong>GIA entregue — competência {competencia}</strong>}>
          <dl className="row">
            <dt className="col-sm-3 text-gray-60">Serviços declarados</dt>
            <dd className="col-sm-9">{resultado.quantidadeServicos}</dd>
            <dt className="col-sm-3 text-gray-60">Total dos serviços</dt>
            <dd className="col-sm-9">{formatarMoeda(resultado.totalServicos)}</dd>
            <dt className="col-sm-3 text-gray-60">ISS devido</dt>
            <dd className="col-sm-9 text-semi-bold text-up-02">{formatarMoeda(resultado.issDevido)}</dd>
          </dl>
          {resultado.lancamentoId && (
            <Alert variant="success" title="Lançamento do ISS devido">
              Lançamento <strong>{resultado.lancamentoId}</strong> constituído (declaração{' '}
              {resultado.declaracaoId}).
            </Alert>
          )}
        </Card>
      )}
    </>
  );
}
