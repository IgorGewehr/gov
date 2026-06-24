// Tela CERTIDÕES DE REGULARIDADE FISCAL — CND/CPEN (PARIDADE-PoC SW-A3). Duas frentes:
//  1) EMITIR a certidão de um contribuinte (apura a situação fiscal e decide o tipo:
//     Negativa / Positiva-com-efeito-Negativa / Positiva) — gated por "tributos.ver".
//  2) CONFERIR a autenticidade de uma certidão apresentada (número + código) — público.
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Alert,
  Button,
  Card,
  FormField,
  Input,
  PageHeader,
  Tag,
  Toolbar,
  useToast,
} from '../../components/ui';
import { Can } from '../../auth/Can';
import { ApiError } from '../../api/problemDetails';
import { formatarData } from '../../i18n/format';
import { TributosSubNav } from './TributosSubNav';
import {
  ROTULO_TIPO_CERTIDAO,
  useConferirCertidao,
  useEmitirCertidao,
  type CertidaoRegularidade,
  type TipoCertidao,
} from './certidoes.api';

const PERM_VER = 'tributos.ver';

/** Variante de Tag pela regularidade do tipo de certidão. */
function variantePorTipo(tipo: TipoCertidao): 'success' | 'warning' | 'danger' {
  if (tipo === 'Negativa') return 'success';
  if (tipo === 'PositivaComEfeitoNegativa') return 'warning';
  return 'danger';
}

function CertidaoEmitidaCard({ certidao }: { certidao: CertidaoRegularidade }) {
  return (
    <Card
      className="mb-4"
      header={
        <strong>
          Certidão {certidao.numero}{' '}
          <Tag variant={variantePorTipo(certidao.tipo)}>{ROTULO_TIPO_CERTIDAO[certidao.tipo]}</Tag>
        </strong>
      }
    >
      <dl className="row">
        <dt className="col-sm-3 text-gray-60">Contribuinte</dt>
        <dd className="col-sm-9">
          {certidao.nomeContribuinte} ({certidao.documento})
        </dd>
        <dt className="col-sm-3 text-gray-60">Emissão</dt>
        <dd className="col-sm-9">{formatarData(certidao.dataEmissao)}</dd>
        <dt className="col-sm-3 text-gray-60">Validade</dt>
        <dd className="col-sm-9">{formatarData(certidao.dataValidade)}</dd>
        <dt className="col-sm-3 text-gray-60">Código de autenticação</dt>
        <dd className="col-sm-9">
          <code>{certidao.codigoAutenticacao}</code>
        </dd>
        <dt className="col-sm-3 text-gray-60">Fundamento</dt>
        <dd className="col-sm-9">{certidao.fundamentoLegal}</dd>
      </dl>
      {certidao.observacao && (
        <Alert variant="warning" title="Observação">
          {certidao.observacao}
        </Alert>
      )}
    </Card>
  );
}

export function CertidoesPage() {
  const toast = useToast();

  // Emissão
  const [contribuinteId, setContribuinteId] = useState('');
  const [fundamentoLegal, setFundamentoLegal] = useState(
    'CTN arts. 205 e 206; Código Tributário Municipal.',
  );
  const [emitida, setEmitida] = useState<CertidaoRegularidade | null>(null);
  const emitir = useEmitirCertidao();

  // Conferência
  const [numero, setNumero] = useState('');
  const [codigo, setCodigo] = useState('');
  const [conferindo, setConferindo] = useState(false);
  const conferencia = useConferirCertidao(numero, codigo, conferindo);

  function emitirCertidao(event: FormEvent): void {
    event.preventDefault();
    if (contribuinteId.trim() === '') {
      toast.error('Informe o identificador do contribuinte.');
      return;
    }
    if (fundamentoLegal.trim() === '') {
      toast.error('Informe o fundamento legal.');
      return;
    }
    emitir.mutate(
      { contribuinteId: contribuinteId.trim(), fundamentoLegal: fundamentoLegal.trim() },
      {
        onSuccess: (c) => {
          setEmitida(c);
          toast.success(`Certidão ${c.numero} emitida.`, 'Sucesso');
        },
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível emitir a certidão.',
          ),
      },
    );
  }

  function conferir(event: FormEvent): void {
    event.preventDefault();
    if (numero.trim() === '' || codigo.trim() === '') {
      toast.error('Informe o número e o código de autenticação.');
      return;
    }
    setConferindo(true);
  }

  return (
    <>
      <PageHeader
        title="Certidões de regularidade (CND)"
        description="Emita a Certidão Negativa de Débitos (ou Positiva com efeito de Negativa) e confira a autenticidade de uma certidão apresentada."
      />

      <TributosSubNav />

      <Can permission={PERM_VER}>
        <Card className="mb-4" header={<strong>Emitir certidão do contribuinte</strong>}>
          <form className="br-form" onSubmit={emitirCertidao}>
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
              <div className="col-md-5">
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
              <div className="col-md-2">
                <Toolbar>
                  <Button variant="primary" type="submit" loading={emitir.isPending}>
                    <i className="fas fa-certificate" aria-hidden="true" /> Emitir
                  </Button>
                </Toolbar>
              </div>
            </div>
          </form>
        </Card>
      </Can>

      {emitida && <CertidaoEmitidaCard certidao={emitida} />}

      <Card header={<strong>Conferir autenticidade</strong>}>
        <form className="br-form" onSubmit={conferir}>
          <div className="row align-items-end">
            <div className="col-md-5">
              <FormField label="Número da certidão" required>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={numero}
                    onChange={(e) => {
                      setNumero(e.target.value);
                      setConferindo(false);
                    }}
                    placeholder="CRF-2026-00000001"
                  />
                )}
              </FormField>
            </div>
            <div className="col-md-5">
              <FormField label="Código de autenticação" required>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={codigo}
                    onChange={(e) => {
                      setCodigo(e.target.value);
                      setConferindo(false);
                    }}
                  />
                )}
              </FormField>
            </div>
            <div className="col-md-2">
              <Toolbar>
                <Button variant="secondary" type="submit" loading={conferencia.isFetching}>
                  <i className="fas fa-magnifying-glass" aria-hidden="true" /> Conferir
                </Button>
              </Toolbar>
            </div>
          </div>
        </form>

        {conferindo && conferencia.data && (
          <div className="mt-3">
            {conferencia.data.autentica ? (
              <Alert
                variant={conferencia.data.vigente ? 'success' : 'warning'}
                title={conferencia.data.vigente ? 'Certidão autêntica e vigente' : 'Certidão autêntica, porém vencida'}
              >
                {conferencia.data.numero} — {conferencia.data.nomeContribuinte}
                {conferencia.data.tipo && (
                  <>
                    {' '}
                    <Tag variant={variantePorTipo(conferencia.data.tipo)}>
                      {ROTULO_TIPO_CERTIDAO[conferencia.data.tipo]}
                    </Tag>
                  </>
                )}
                . Validade até {formatarData(conferencia.data.dataValidade)}.
              </Alert>
            ) : (
              <Alert variant="danger" title="Certidão não autêntica">
                Número ou código de autenticação não conferem com nenhuma certidão emitida.
              </Alert>
            )}
          </div>
        )}
      </Card>
    </>
  );
}
