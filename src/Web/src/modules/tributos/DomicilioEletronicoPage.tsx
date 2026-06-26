// Tela Domicílio Eletrônico do Contribuinte (DEC) — caixa postal fiscal.
// Adere o contribuinte ao DEC e disponibiliza comunicações fiscais (intimação/notificação/aviso) com
// prazo de ciência tácita e prazo de manifestação. Gated por "tributos.gerenciar".
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Alert,
  Button,
  Card,
  FormField,
  Input,
  PageHeader,
  Select,
  Textarea,
  Toolbar,
  useToast,
} from '../../components/ui';
import { Can } from '../../auth/Can';
import { ApiError } from '../../api/problemDetails';
import { TributosSubNav } from './TributosSubNav';
import {
  useAderirDomicilio,
  useDisponibilizarMensagem,
  type ResultadoMensagemFiscal,
  type TipoMensagemFiscal,
} from './domicilio.api';

const PERM_GERENCIAR = 'tributos.gerenciar';

const TIPO_OPCOES = [
  { value: 'Intimacao', label: 'Intimação' },
  { value: 'Notificacao', label: 'Notificação' },
  { value: 'Aviso', label: 'Aviso' },
];

export function DomicilioEletronicoPage() {
  const toast = useToast();
  const [contribuinteId, setContribuinteId] = useState('');

  // Adesão.
  const [dataAdesao, setDataAdesao] = useState('');
  const [diasCienciaTacita, setDiasCienciaTacita] = useState(15);
  const [aderido, setAderido] = useState(false);

  // Mensagem.
  const [tipo, setTipo] = useState<TipoMensagemFiscal>('Intimacao');
  const [assunto, setAssunto] = useState('');
  const [corpo, setCorpo] = useState('');
  const [dataDisponibilizacao, setDataDisponibilizacao] = useState('');
  const [diasPrazoManifestacao, setDiasPrazoManifestacao] = useState(30);
  const [referenciaExterna, setReferenciaExterna] = useState('');
  const [resultado, setResultado] = useState<ResultadoMensagemFiscal | null>(null);

  const aderir = useAderirDomicilio(contribuinteId.trim());
  const disponibilizar = useDisponibilizarMensagem(contribuinteId.trim());

  function enviarAdesao(event: FormEvent): void {
    event.preventDefault();
    if (contribuinteId.trim() === '') {
      toast.error('Informe o identificador do contribuinte.');
      return;
    }
    if (dataAdesao.trim() === '') {
      toast.error('Informe a data de adesão.');
      return;
    }
    aderir.mutate(
      { dataAdesao: dataAdesao.trim(), diasCienciaTacita: Number(diasCienciaTacita) || 15 },
      {
        onSuccess: () => {
          setAderido(true);
          toast.success('Contribuinte aderido ao Domicílio Eletrônico.', 'Sucesso');
        },
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível aderir ao domicílio.',
          ),
      },
    );
  }

  function enviarMensagem(event: FormEvent): void {
    event.preventDefault();
    if (assunto.trim() === '' || corpo.trim() === '') {
      toast.error('Informe o assunto e o corpo da mensagem.');
      return;
    }
    if (dataDisponibilizacao.trim() === '') {
      toast.error('Informe a data de disponibilização.');
      return;
    }
    disponibilizar.mutate(
      {
        tipo,
        assunto: assunto.trim(),
        corpo: corpo.trim(),
        dataDisponibilizacao: dataDisponibilizacao.trim(),
        diasPrazoManifestacao: Number(diasPrazoManifestacao) || 0,
        referenciaExterna: referenciaExterna.trim() === '' ? null : referenciaExterna.trim(),
      },
      {
        onSuccess: (r) => {
          setResultado(r);
          toast.success('Mensagem fiscal disponibilizada.', 'Sucesso');
        },
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível disponibilizar a mensagem.',
          ),
      },
    );
  }

  return (
    <>
      <PageHeader
        title="Domicílio Eletrônico do Contribuinte (DEC)"
        description="Caixa postal fiscal com efeito de intimação: adira o contribuinte e disponibilize comunicações com prazo de ciência tácita e de manifestação."
      />

      <TributosSubNav />

      <Alert variant="info" title="Efeito de intimação pessoal">
        Após a adesão, as comunicações têm efeito legal. A ciência ocorre na consulta do contribuinte ou,
        decorrido o prazo de disponibilização sem consulta, de forma tácita. Datas são do fato.
      </Alert>

      <Card className="mb-4" header={<strong>1. Adesão ao domicílio</strong>}>
        <form className="br-form" onSubmit={enviarAdesao}>
          <div className="row align-items-end">
            <div className="col-md-6">
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
              <FormField label="Data de adesão" required>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    type="date"
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={dataAdesao}
                    onChange={(e) => setDataAdesao(e.target.value)}
                  />
                )}
              </FormField>
            </div>
            <div className="col-md-3">
              <FormField label="Dias p/ ciência tácita" help="Padrão 15 dias.">
                {({ id, describedBy }) => (
                  <Input
                    id={id}
                    type="number"
                    min="1"
                    aria-describedby={describedBy}
                    value={String(diasCienciaTacita)}
                    onChange={(e) => setDiasCienciaTacita(Number(e.target.value))}
                  />
                )}
              </FormField>
            </div>
          </div>
          <Can permission={PERM_GERENCIAR}>
            <Toolbar>
              <Button variant="primary" type="submit" loading={aderir.isPending}>
                <i className="fas fa-inbox" aria-hidden="true" /> Aderir
              </Button>
            </Toolbar>
          </Can>
        </form>
      </Card>

      {aderido && (
        <Card header={<strong>2. Disponibilizar mensagem fiscal</strong>}>
          <form className="br-form" onSubmit={enviarMensagem}>
            <div className="row align-items-end">
              <div className="col-md-4">
                <FormField label="Tipo" required>
                  {({ id, describedBy }) => (
                    <Select
                      id={id}
                      aria-describedby={describedBy}
                      options={TIPO_OPCOES}
                      value={tipo}
                      onChange={(e) => setTipo(e.target.value as TipoMensagemFiscal)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-md-4">
                <FormField label="Data de disponibilização" required>
                  {({ id, describedBy, invalid }) => (
                    <Input
                      id={id}
                      type="date"
                      aria-describedby={describedBy}
                      invalid={invalid}
                      value={dataDisponibilizacao}
                      onChange={(e) => setDataDisponibilizacao(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-md-4">
                <FormField label="Prazo de manifestação (dias)" help="0 se sem prazo.">
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      type="number"
                      min="0"
                      aria-describedby={describedBy}
                      value={String(diasPrazoManifestacao)}
                      onChange={(e) => setDiasPrazoManifestacao(Number(e.target.value))}
                    />
                  )}
                </FormField>
              </div>
            </div>

            <div className="row">
              <div className="col-md-8">
                <FormField label="Assunto" required>
                  {({ id, describedBy, invalid }) => (
                    <Input
                      id={id}
                      aria-describedby={describedBy}
                      invalid={invalid}
                      value={assunto}
                      onChange={(e) => setAssunto(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-md-4">
                <FormField label="Referência (ato de origem)" help="Ex.: nº do lançamento/CDA.">
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      aria-describedby={describedBy}
                      value={referenciaExterna}
                      onChange={(e) => setReferenciaExterna(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
            </div>

            <FormField label="Corpo da comunicação" required>
              {({ id, describedBy, invalid }) => (
                <Textarea
                  id={id}
                  rows={5}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={corpo}
                  onChange={(e) => setCorpo(e.target.value)}
                />
              )}
            </FormField>

            <Can permission={PERM_GERENCIAR}>
              <Toolbar>
                <Button variant="primary" type="submit" loading={disponibilizar.isPending}>
                  <i className="fas fa-paper-plane" aria-hidden="true" /> Disponibilizar
                </Button>
              </Toolbar>
            </Can>
          </form>

          {resultado && (
            <Alert variant="success" title="Mensagem disponibilizada">
              Mensagem <strong>{resultado.mensagemId}</strong>. Ciência tácita em{' '}
              <strong>{resultado.dataLimiteCienciaTacita}</strong> se não houver consulta antes.
            </Alert>
          )}
        </Card>
      )}
    </>
  );
}
