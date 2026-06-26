// Tela S.I.M. — Serviço de Inspeção Municipal (título de registro de estabelecimento).
// Protocola o requerimento (responsável, estabelecimento, natureza, produtos com rótulo) e, em seguida,
// concede o registro (atribui o número do S.I.M.). Gated por "tributos.gerenciar".
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
  Select,
  Toolbar,
  useToast,
  type Column,
} from '../../components/ui';
import { Can } from '../../auth/Can';
import { ApiError } from '../../api/problemDetails';
import { TributosSubNav } from './TributosSubNav';
import {
  useRequererTituloSim,
  useConcederTituloSim,
  type NaturezaProdutoSim,
  type ProdutoSimInput,
} from './sim.api';

const PERM_GERENCIAR = 'tributos.gerenciar';

const PRODUTO_VAZIO: ProdutoSimInput = { denominacao: '', classificacao: '', numeroRotulo: '' };

const NATUREZA_OPCOES = [
  { value: 'OrigemAnimal', label: 'Origem animal' },
  { value: 'OrigemVegetal', label: 'Origem vegetal' },
];

export function SimPage() {
  const toast = useToast();
  const [responsavelId, setResponsavelId] = useState('');
  const [razaoSocial, setRazaoSocial] = useState('');
  const [natureza, setNatureza] = useState<NaturezaProdutoSim>('OrigemAnimal');
  const [endereco, setEndereco] = useState('');
  const [dataRequerimento, setDataRequerimento] = useState('');
  const [produtos, setProdutos] = useState<ProdutoSimInput[]>([{ ...PRODUTO_VAZIO }]);
  const [tituloId, setTituloId] = useState<string | null>(null);

  // Concessão.
  const [dataRegistro, setDataRegistro] = useState('');
  const [fimVigencia, setFimVigencia] = useState('');
  const [numeroSim, setNumeroSim] = useState<string | null>(null);

  const requerer = useRequererTituloSim();
  const conceder = useConcederTituloSim(tituloId ?? '');

  function atualizar(indice: number, patch: Partial<ProdutoSimInput>): void {
    setProdutos((atual) => atual.map((p, i) => (i === indice ? { ...p, ...patch } : p)));
  }

  function enviarRequerimento(event: FormEvent): void {
    event.preventDefault();
    if (responsavelId.trim() === '' || razaoSocial.trim() === '' || endereco.trim() === '') {
      toast.error('Informe responsável, razão social e endereço.');
      return;
    }
    if (dataRequerimento.trim() === '') {
      toast.error('Informe a data do requerimento.');
      return;
    }
    const validos = produtos.filter(
      (p) => p.denominacao.trim() !== '' && p.numeroRotulo.trim() !== '',
    );
    if (validos.length === 0) {
      toast.error('Habilite ao menos um produto (denominação e rótulo).');
      return;
    }
    requerer.mutate(
      {
        responsavelId: responsavelId.trim(),
        razaoSocialEstabelecimento: razaoSocial.trim(),
        natureza,
        enderecoEstabelecimento: endereco.trim(),
        dataRequerimento: dataRequerimento.trim(),
        produtos: validos,
      },
      {
        onSuccess: (r) => {
          setTituloId(r.id);
          toast.success('Requerimento protocolado. Conceda o registro abaixo.', 'Sucesso');
        },
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível protocolar o requerimento.',
          ),
      },
    );
  }

  function concederRegistro(event: FormEvent): void {
    event.preventDefault();
    if (!tituloId) {
      return;
    }
    if (dataRegistro.trim() === '' || fimVigencia.trim() === '') {
      toast.error('Informe a data de registro e o fim da vigência.');
      return;
    }
    conceder.mutate(
      { dataRegistro: dataRegistro.trim(), fimVigencia: fimVigencia.trim() },
      {
        onSuccess: (r) => {
          setNumeroSim(r.numeroSim);
          toast.success(`Registro concedido: ${r.numeroSim}.`, 'Sucesso');
        },
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível conceder o registro.',
          ),
      },
    );
  }

  const colunas: Column<ProdutoSimInput & { indice: number }>[] = [
    {
      key: 'denominacao',
      header: 'Denominação',
      render: (p) => (
        <Input
          aria-label={`Denominação do produto ${p.indice + 1}`}
          value={p.denominacao}
          onChange={(e) => atualizar(p.indice, { denominacao: e.target.value })}
          placeholder="Queijo Colonial"
        />
      ),
    },
    {
      key: 'classificacao',
      header: 'Classificação',
      render: (p) => (
        <Input
          aria-label={`Classificação do produto ${p.indice + 1}`}
          value={p.classificacao}
          onChange={(e) => atualizar(p.indice, { classificacao: e.target.value })}
        />
      ),
    },
    {
      key: 'rotulo',
      header: 'Nº do rótulo',
      render: (p) => (
        <Input
          aria-label={`Número do rótulo do produto ${p.indice + 1}`}
          value={p.numeroRotulo}
          onChange={(e) => atualizar(p.indice, { numeroRotulo: e.target.value })}
        />
      ),
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (p) => (
        <Button
          variant="secondary"
          size="sm"
          onClick={() =>
            setProdutos((atual) =>
              atual.length > 1 ? atual.filter((_, i) => i !== p.indice) : atual,
            )
          }
        >
          <i className="fas fa-trash" aria-hidden="true" /> Remover
        </Button>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="S.I.M. — Serviço de Inspeção Municipal"
        description="Registro de estabelecimento de produtos de origem animal/vegetal: protocole o requerimento e conceda o título (número do S.I.M.)."
      />

      <TributosSubNav />

      <Alert variant="info" title="Ato de polícia sanitária">
        Habilita o estabelecimento a industrializar/comercializar no município (Lei 7.889/89; Decreto
        9.013/2017 — RIISPOA). A taxa de inspeção (TLL) é lançada à parte.
      </Alert>

      <Card className="mb-4" header={<strong>1. Requerimento de registro</strong>}>
        <form className="br-form" onSubmit={enviarRequerimento}>
          <div className="row align-items-end">
            <div className="col-md-6">
              <FormField label="Responsável (contribuinte)" required>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={responsavelId}
                    onChange={(e) => setResponsavelId(e.target.value)}
                    placeholder="00000000-0000-0000-0000-000000000000"
                  />
                )}
              </FormField>
            </div>
            <div className="col-md-3">
              <FormField label="Natureza" required>
                {({ id, describedBy }) => (
                  <Select
                    id={id}
                    aria-describedby={describedBy}
                    options={NATUREZA_OPCOES}
                    value={natureza}
                    onChange={(e) => setNatureza(e.target.value as NaturezaProdutoSim)}
                  />
                )}
              </FormField>
            </div>
            <div className="col-md-3">
              <FormField label="Data do requerimento" required>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    type="date"
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={dataRequerimento}
                    onChange={(e) => setDataRequerimento(e.target.value)}
                  />
                )}
              </FormField>
            </div>
          </div>

          <div className="row">
            <div className="col-md-6">
              <FormField label="Razão social do estabelecimento" required>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={razaoSocial}
                    onChange={(e) => setRazaoSocial(e.target.value)}
                  />
                )}
              </FormField>
            </div>
            <div className="col-md-6">
              <FormField label="Endereço do estabelecimento" required>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={endereco}
                    onChange={(e) => setEndereco(e.target.value)}
                  />
                )}
              </FormField>
            </div>
          </div>

          <DataTable
            caption="Produtos inspecionados a habilitar"
            columns={colunas}
            rows={produtos.map((p, indice) => ({ ...p, indice }))}
            rowKey={(p) => String(p.indice)}
          />

          <Can permission={PERM_GERENCIAR}>
            <Toolbar>
              <Button
                variant="secondary"
                onClick={() => setProdutos((atual) => [...atual, { ...PRODUTO_VAZIO }])}
              >
                <i className="fas fa-plus" aria-hidden="true" /> Adicionar produto
              </Button>
              <Button variant="primary" type="submit" loading={requerer.isPending}>
                <i className="fas fa-file-signature" aria-hidden="true" /> Protocolar requerimento
              </Button>
            </Toolbar>
          </Can>
        </form>
      </Card>

      {tituloId && (
        <Card header={<strong>2. Concessão do registro</strong>}>
          <Alert variant="success" title="Requerimento protocolado">
            Título <strong>{tituloId}</strong> em análise. Informe a vigência e conceda o registro.
          </Alert>
          <form className="br-form" onSubmit={concederRegistro}>
            <div className="row align-items-end">
              <div className="col-md-4">
                <FormField label="Data de registro" required>
                  {({ id, describedBy, invalid }) => (
                    <Input
                      id={id}
                      type="date"
                      aria-describedby={describedBy}
                      invalid={invalid}
                      value={dataRegistro}
                      onChange={(e) => setDataRegistro(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-md-4">
                <FormField label="Fim da vigência" required>
                  {({ id, describedBy, invalid }) => (
                    <Input
                      id={id}
                      type="date"
                      aria-describedby={describedBy}
                      invalid={invalid}
                      value={fimVigencia}
                      onChange={(e) => setFimVigencia(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-md-4">
                <Can permission={PERM_GERENCIAR}>
                  <Button variant="primary" type="submit" loading={conceder.isPending}>
                    <i className="fas fa-stamp" aria-hidden="true" /> Conceder registro
                  </Button>
                </Can>
              </div>
            </div>
          </form>
          {numeroSim && (
            <Alert variant="success" title="Registro concedido">
              Número do S.I.M.: <strong>{numeroSim}</strong>.
            </Alert>
          )}
        </Card>
      )}
    </>
  );
}
