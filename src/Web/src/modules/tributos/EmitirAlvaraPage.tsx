// Tela EMITIR ALVARÁ — submódulo Alvarás. Emite o alvará (ATO de polícia) e lança a
// TLL (Taxa de Licença de Localização/Funcionamento) correspondente (POST
// /alvaras/emitir): o alvará NÃO é o tributo — a TLL é o tributo correlato, apurado
// pela tabela de taxa vigente e gerado à parte (lançamento + guia DAM). A RENOVAÇÃO
// fica num modal próprio. Toda ação é gated por "tributos.gerenciar".
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Alert,
  Button,
  Card,
  EmptyState,
  FormField,
  Input,
  PageHeader,
  Select,
  useToast,
} from '../../components/ui';
import { Can } from '../../auth/Can';
import { ApiError } from '../../api/problemDetails';
import { formatarMoeda } from '../../i18n/format';
import { ESPECIE_ALVARA_LABEL, ESPECIE_ALVARA_VALOR, useEmitirAlvara } from './alvaras.api';
import type { EspecieAlvara, ResultadoEmissaoAlvara } from './alvaras.api';
import { TributosSubNav } from './TributosSubNav';
import { RenovarAlvaraModal } from './RenovarAlvaraModal';

const PERM_GERENCIAR = 'tributos.gerenciar';

const ESPECIE_OPCOES = (Object.keys(ESPECIE_ALVARA_VALOR) as EspecieAlvara[]).map((k) => ({
  value: k,
  label: ESPECIE_ALVARA_LABEL[k],
}));

export function EmitirAlvaraPage() {
  const toast = useToast();
  const emitir = useEmitirAlvara();

  const [contribuinteId, setContribuinteId] = useState('');
  const [imovelId, setImovelId] = useState('');
  const [especie, setEspecie] = useState<EspecieAlvara>('LocalizacaoFuncionamento');
  const [nomeEstabelecimento, setNomeEstabelecimento] = useState('');
  const [atividadeCnae, setAtividadeCnae] = useState('');
  const [inicioVigencia, setInicioVigencia] = useState('');
  const [fimVigencia, setFimVigencia] = useState('');
  const [codigoTaxaTll, setCodigoTaxaTll] = useState('');
  const [exercicio, setExercicio] = useState(String(new Date().getFullYear()));
  const [quantidadeBaseTll, setQuantidadeBaseTll] = useState('');
  const [vencimentoTll, setVencimentoTll] = useState('');
  const [renovarAberto, setRenovarAberto] = useState(false);
  const [resultado, setResultado] = useState<ResultadoEmissaoAlvara | null>(null);

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (contribuinteId.trim() === '' || nomeEstabelecimento.trim() === '') {
      toast.error('Informe o contribuinte e o nome do estabelecimento.');
      return;
    }
    if (atividadeCnae.trim() === '') {
      toast.error('Informe a atividade (CNAE).');
      return;
    }
    if (inicioVigencia.trim() === '' || fimVigencia.trim() === '') {
      toast.error('Informe o período de vigência do alvará.');
      return;
    }
    if (fimVigencia < inicioVigencia) {
      toast.error('O fim da vigência deve ser igual ou posterior ao início.');
      return;
    }
    if (codigoTaxaTll.trim() === '' || vencimentoTll.trim() === '') {
      toast.error('Informe o código da TLL e o vencimento da guia.');
      return;
    }
    emitir.mutate(
      {
        contribuinteId: contribuinteId.trim(),
        imovelId: imovelId.trim() === '' ? null : imovelId.trim(),
        especie: ESPECIE_ALVARA_VALOR[especie],
        nomeEstabelecimento: nomeEstabelecimento.trim(),
        atividadeCnae: atividadeCnae.trim(),
        inicioVigencia: inicioVigencia.trim(),
        fimVigencia: fimVigencia.trim(),
        codigoTaxaTll: codigoTaxaTll.trim(),
        exercicio: Number(exercicio),
        quantidadeBaseTll: Number((quantidadeBaseTll || '0').replace(',', '.')),
        vencimentoTll: vencimentoTll.trim(),
      },
      {
        onSuccess: (r) => {
          setResultado(r);
          toast.success(`Alvará emitido. TLL: ${formatarMoeda(r.valorTll)}.`, 'Sucesso');
        },
        onError: (error) =>
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível emitir o alvará.'),
      },
    );
  }

  return (
    <>
      <PageHeader
        title="Emitir alvará"
        description="Emita o alvará (ato de polícia) e lance a Taxa de Licença (TLL) correspondente."
        actions={
          <Can permission={PERM_GERENCIAR}>
            <Button variant="secondary" onClick={() => setRenovarAberto(true)}>
              <i className="fas fa-rotate" aria-hidden="true" /> Renovar alvará
            </Button>
          </Can>
        }
      />

      <TributosSubNav />

      <Card className="mb-4">
        <form className="br-form" onSubmit={submeter} noValidate>
          <div className="row">
            <div className="col-md-6">
              <FormField label="Identificador do contribuinte" required>
                {({ id, describedBy, invalid }) => (
                  <Input id={id} aria-describedby={describedBy} invalid={invalid} value={contribuinteId} onChange={(e) => setContribuinteId(e.target.value)} placeholder="00000000-0000-0000-0000-000000000000" />
                )}
              </FormField>
            </div>
            <div className="col-md-6">
              <FormField label="Imóvel do estabelecimento (opcional)">
                {({ id }) => (
                  <Input id={id} value={imovelId} onChange={(e) => setImovelId(e.target.value)} placeholder="Identificador do imóvel" />
                )}
              </FormField>
            </div>
          </div>

          <div className="row">
            <div className="col-md-5">
              <FormField label="Espécie" required>
                {({ id }) => (
                  <Select id={id} options={ESPECIE_OPCOES} value={especie} onChange={(e) => setEspecie(e.target.value as EspecieAlvara)} />
                )}
              </FormField>
            </div>
            <div className="col-md-4">
              <FormField label="Estabelecimento" required>
                {({ id, describedBy, invalid }) => (
                  <Input id={id} aria-describedby={describedBy} invalid={invalid} value={nomeEstabelecimento} onChange={(e) => setNomeEstabelecimento(e.target.value)} placeholder="Razão social" />
                )}
              </FormField>
            </div>
            <div className="col-md-3">
              <FormField label="Atividade (CNAE)" required>
                {({ id, describedBy, invalid }) => (
                  <Input id={id} aria-describedby={describedBy} invalid={invalid} value={atividadeCnae} onChange={(e) => setAtividadeCnae(e.target.value)} placeholder="4711-3/02" />
                )}
              </FormField>
            </div>
          </div>

          <div className="row">
            <div className="col-md-6">
              <FormField label="Início de vigência" required>
                {({ id, describedBy, invalid }) => (
                  <Input id={id} type="date" aria-describedby={describedBy} invalid={invalid} value={inicioVigencia} onChange={(e) => setInicioVigencia(e.target.value)} />
                )}
              </FormField>
            </div>
            <div className="col-md-6">
              <FormField label="Fim de vigência" required>
                {({ id, describedBy, invalid }) => (
                  <Input id={id} type="date" aria-describedby={describedBy} invalid={invalid} value={fimVigencia} onChange={(e) => setFimVigencia(e.target.value)} />
                )}
              </FormField>
            </div>
          </div>

          <fieldset className="mb-3">
            <legend className="text-up-01 text-semi-bold">Taxa de licença (TLL)</legend>
            <div className="row">
              <div className="col-md-4">
                <FormField label="Código da TLL (CTM)" required>
                  {({ id, describedBy, invalid }) => (
                    <Input id={id} aria-describedby={describedBy} invalid={invalid} value={codigoTaxaTll} onChange={(e) => setCodigoTaxaTll(e.target.value)} placeholder="TLL-001" />
                  )}
                </FormField>
              </div>
              <div className="col-md-3">
                <FormField label="Exercício" required>
                  {({ id, describedBy, invalid }) => (
                    <Input id={id} type="number" min={1900} inputMode="numeric" aria-describedby={describedBy} invalid={invalid} value={exercicio} onChange={(e) => setExercicio(e.target.value)} />
                  )}
                </FormField>
              </div>
              <div className="col-md-2">
                <FormField label="Qtd.-base" help="Ignorada no Valor fixo.">
                  {({ id }) => (
                    <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" value={quantidadeBaseTll} onChange={(e) => setQuantidadeBaseTll(e.target.value)} />
                  )}
                </FormField>
              </div>
              <div className="col-md-3">
                <FormField label="Vencimento da TLL" required>
                  {({ id, describedBy, invalid }) => (
                    <Input id={id} type="date" aria-describedby={describedBy} invalid={invalid} value={vencimentoTll} onChange={(e) => setVencimentoTll(e.target.value)} />
                  )}
                </FormField>
              </div>
            </div>
          </fieldset>

          <Can permission={PERM_GERENCIAR}>
            <Button variant="primary" type="submit" loading={emitir.isPending}>
              <i className="fas fa-stamp" aria-hidden="true" /> Emitir alvará
            </Button>
          </Can>
        </form>
      </Card>

      {!resultado ? (
        <EmptyState
          icon="fas fa-stamp"
          title="Preencha os dados e emita"
          description="O alvará é o ato de polícia; a TLL é o tributo correlato, lançado à parte pela tabela vigente."
        />
      ) : (
        <Alert variant="success" title="Alvará emitido">
          Alvará <strong>{resultado.alvaraId}</strong> emitido. TLL apurada:{' '}
          <strong>{formatarMoeda(resultado.valorTll)}</strong> (lançamento {resultado.lancamentoTllId},
          guia DAM {resultado.damId}).
        </Alert>
      )}

      <RenovarAlvaraModal open={renovarAberto} onClose={() => setRenovarAberto(false)} />
    </>
  );
}
