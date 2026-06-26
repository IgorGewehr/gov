// Tela DES-IF — Apuração Mensal do ISSQN das instituições financeiras (Módulo 2 ABRASF).
// Seleciona o contribuinte (banco) e a competência, escritura os subtítulos COSIF tributáveis
// (Registro 0430), informa as deduções/incentivos/depósitos (Registro 0440) e ENTREGA a
// declaração — constituindo o lançamento do ISSQN a recolher. Gated por "tributos.gerenciar".
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
import { useEntregarDesif, type ResultadoDesif, type SubtituloDesifInput } from './desif.api';

const PERM_GERENCIAR = 'tributos.gerenciar';

function competenciaAtual(): string {
  const agora = new Date();
  return `${agora.getFullYear()}-${String(agora.getMonth() + 1).padStart(2, '0')}`;
}

const SUBTITULO_VAZIO: SubtituloDesifInput = {
  contaCosif: '',
  codigoTributacaoDesif: '',
  itemListaServico: '',
  descricao: '',
  baseCalculo: 0,
  aliquotaPercentual: 0,
};

export function DesIfPage() {
  const toast = useToast();
  const [contribuinteId, setContribuinteId] = useState('');
  const [competencia, setCompetencia] = useState(competenciaAtual());
  const [fundamentoLegal, setFundamentoLegal] = useState(
    'LC 116/2003 (itens do setor financeiro); COSIF/BACEN; Código Tributário Municipal.',
  );
  const [vencimento, setVencimento] = useState('');
  const [deducoes, setDeducoes] = useState(0);
  const [incentivos, setIncentivos] = useState(0);
  const [depositos, setDepositos] = useState(0);
  const [subtitulos, setSubtitulos] = useState<SubtituloDesifInput[]>([{ ...SUBTITULO_VAZIO }]);
  const [resultado, setResultado] = useState<ResultadoDesif | null>(null);

  const entregar = useEntregarDesif(contribuinteId.trim());

  function atualizar(indice: number, patch: Partial<SubtituloDesifInput>): void {
    setSubtitulos((atual) => atual.map((s, i) => (i === indice ? { ...s, ...patch } : s)));
  }

  function adicionar(): void {
    setSubtitulos((atual) => [...atual, { ...SUBTITULO_VAZIO }]);
  }

  function remover(indice: number): void {
    setSubtitulos((atual) => (atual.length > 1 ? atual.filter((_, i) => i !== indice) : atual));
  }

  function enviar(event: FormEvent): void {
    event.preventDefault();
    if (contribuinteId.trim() === '') {
      toast.error('Informe o identificador do contribuinte (instituição financeira).');
      return;
    }
    if (!/^\d{4}-\d{2}$/.test(competencia)) {
      toast.error('Informe a competência no formato AAAA-MM.');
      return;
    }
    if (vencimento.trim() === '') {
      toast.error('Informe o vencimento do ISSQN a recolher.');
      return;
    }
    const validos = subtitulos.filter(
      (s) => s.contaCosif.trim() !== '' && s.itemListaServico.trim() !== '',
    );
    if (validos.length === 0) {
      toast.error('Escriture ao menos um subtítulo (conta COSIF e item LC 116).');
      return;
    }
    const [ano, mes] = competencia.split('-').map(Number);
    entregar.mutate(
      {
        ano,
        mes,
        fundamentoLegal: fundamentoLegal.trim(),
        deducoesReceita: Number(deducoes) || 0,
        incentivosFiscais: Number(incentivos) || 0,
        depositosJudiciais: Number(depositos) || 0,
        vencimentoIssqn: vencimento.trim(),
        subtitulos: validos,
      },
      {
        onSuccess: (r) => {
          setResultado(r);
          toast.success(`DES-IF da competência ${competencia} entregue.`, 'Sucesso');
        },
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível entregar a DES-IF.',
          ),
      },
    );
  }

  const totalBase = subtitulos.reduce((soma, s) => soma + (Number(s.baseCalculo) || 0), 0);

  const colunas: Column<SubtituloDesifInput & { indice: number }>[] = [
    {
      key: 'cosif',
      header: 'Conta COSIF',
      render: (s) => (
        <Input
          aria-label={`Conta COSIF do subtítulo ${s.indice + 1}`}
          value={s.contaCosif}
          onChange={(e) => atualizar(s.indice, { contaCosif: e.target.value })}
          placeholder="7.1.7.99.00-8"
        />
      ),
    },
    {
      key: 'codTrib',
      header: 'Cód. Trib. DES-IF',
      render: (s) => (
        <Input
          aria-label={`Código de tributação DES-IF do subtítulo ${s.indice + 1}`}
          value={s.codigoTributacaoDesif}
          onChange={(e) => atualizar(s.indice, { codigoTributacaoDesif: e.target.value })}
        />
      ),
    },
    {
      key: 'item',
      header: 'Item LC 116',
      render: (s) => (
        <Input
          aria-label={`Item LC 116 do subtítulo ${s.indice + 1}`}
          value={s.itemListaServico}
          onChange={(e) => atualizar(s.indice, { itemListaServico: e.target.value })}
          placeholder="15.01"
        />
      ),
    },
    {
      key: 'descricao',
      header: 'Descrição',
      render: (s) => (
        <Input
          aria-label={`Descrição do subtítulo ${s.indice + 1}`}
          value={s.descricao}
          onChange={(e) => atualizar(s.indice, { descricao: e.target.value })}
        />
      ),
    },
    {
      key: 'base',
      header: 'Receita tributável (R$)',
      render: (s) => (
        <Input
          type="number"
          step="0.01"
          min="0"
          aria-label={`Receita tributável do subtítulo ${s.indice + 1}`}
          value={String(s.baseCalculo)}
          onChange={(e) => atualizar(s.indice, { baseCalculo: Number(e.target.value) })}
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
          aria-label={`Alíquota do subtítulo ${s.indice + 1}`}
          value={String(s.aliquotaPercentual)}
          onChange={(e) => atualizar(s.indice, { aliquotaPercentual: Number(e.target.value) })}
        />
      ),
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (s) => (
        <Button variant="secondary" size="sm" onClick={() => remover(s.indice)}>
          <i className="fas fa-trash" aria-hidden="true" /> Remover
        </Button>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="DES-IF — Apuração Mensal do ISSQN"
        description="Declaração das instituições financeiras (modelo ABRASF): escriture os subtítulos COSIF tributáveis e entregue — constitui o lançamento do ISSQN a recolher."
      />

      <TributosSubNav />

      <Alert variant="info" title="Setor financeiro (COSIF)">
        A DES-IF substitui a NFS-e para os bancos. A apuração é por subtítulo COSIF (Registro 0430); o
        ISSQN a recolher é o devido bruto menos deduções, incentivos em lei e depósitos judiciais
        (Registro 0440).
      </Alert>

      <Card className="mb-4">
        <form className="br-form" onSubmit={enviar}>
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
              <FormField label="Vencimento do ISSQN" required>
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
            caption="Subtítulos COSIF declarados (Registro 0430)"
            columns={colunas}
            rows={subtitulos.map((s, indice) => ({ ...s, indice }))}
            rowKey={(s) => String(s.indice)}
          />

          <p className="text-gray-60">Receita tributável total: {formatarMoeda(totalBase)}</p>

          <div className="row align-items-end">
            <div className="col-md-4">
              <FormField label="Deduções da receita (R$)">
                {({ id, describedBy }) => (
                  <Input
                    id={id}
                    type="number"
                    step="0.01"
                    min="0"
                    aria-describedby={describedBy}
                    value={String(deducoes)}
                    onChange={(e) => setDeducoes(Number(e.target.value))}
                  />
                )}
              </FormField>
            </div>
            <div className="col-md-4">
              <FormField label="Incentivos fiscais (R$)">
                {({ id, describedBy }) => (
                  <Input
                    id={id}
                    type="number"
                    step="0.01"
                    min="0"
                    aria-describedby={describedBy}
                    value={String(incentivos)}
                    onChange={(e) => setIncentivos(Number(e.target.value))}
                  />
                )}
              </FormField>
            </div>
            <div className="col-md-4">
              <FormField label="Depósitos judiciais (R$)">
                {({ id, describedBy }) => (
                  <Input
                    id={id}
                    type="number"
                    step="0.01"
                    min="0"
                    aria-describedby={describedBy}
                    value={String(depositos)}
                    onChange={(e) => setDepositos(Number(e.target.value))}
                  />
                )}
              </FormField>
            </div>
          </div>

          <Can permission={PERM_GERENCIAR}>
            <Toolbar>
              <Button variant="secondary" onClick={adicionar}>
                <i className="fas fa-plus" aria-hidden="true" /> Adicionar subtítulo
              </Button>
              <Button variant="primary" type="submit" loading={entregar.isPending}>
                <i className="fas fa-paper-plane" aria-hidden="true" /> Entregar DES-IF
              </Button>
            </Toolbar>
          </Can>
        </form>
      </Card>

      {resultado && (
        <Card header={<strong>DES-IF entregue — competência {competencia}</strong>}>
          <dl className="row">
            <dt className="col-sm-4 text-gray-60">Subtítulos escriturados</dt>
            <dd className="col-sm-8">{resultado.quantidadeSubtitulos}</dd>
            <dt className="col-sm-4 text-gray-60">Receita tributável total</dt>
            <dd className="col-sm-8">{formatarMoeda(resultado.receitaTributavelTotal)}</dd>
            <dt className="col-sm-4 text-gray-60">ISSQN devido bruto</dt>
            <dd className="col-sm-8">{formatarMoeda(resultado.issqnDevidoBruto)}</dd>
            <dt className="col-sm-4 text-gray-60">ISSQN a recolher</dt>
            <dd className="col-sm-8 text-semi-bold text-up-02">
              {formatarMoeda(resultado.issqnARecolher)}
            </dd>
          </dl>
          {resultado.lancamentoId && (
            <Alert variant="success" title="Lançamento do ISSQN a recolher">
              Lançamento <strong>{resultado.lancamentoId}</strong> constituído (declaração{' '}
              {resultado.declaracaoId}).
            </Alert>
          )}
        </Card>
      )}
    </>
  );
}
