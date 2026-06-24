// DESTINACAO (CONARQ / e-ARQ v2 — W9.4): ciclo do irreversivel, na ordem: (1) avaliar
// aptidao (varredura AguardandoPrazo -> AptoEliminar, nunca antes do prazo, permanente
// jamais), (2) autorizar (ato humano RBAC, gated, I-T1/I-T2), (3) registrar (termo
// carimbado SHA-256 + edital sob WORM, I-T4). O contrato nao expoe GET de fila nem o
// status do carimbo num DTO; as acoes 2 e 3 operam por id da ficha e o carimbo (Local x
// ACT) e orientacao normativa — nao inventamos leitura inexistente.
import { useState } from 'react';
import {
  Alert,
  Button,
  Card,
  FormField,
  Input,
  Metrica,
  MetricaGrade,
  PageHeader,
  Tag,
  Toolbar,
  useToast,
} from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { Can } from '../../../auth/Can';
import { ProtocoloSubNav } from '../ProtocoloSubNav';
import {
  useAutorizarEliminacao,
  useAvaliarAptidaoDestinacoes,
  useRegistrarEliminacao,
} from './arquivistica.api';
import { COMPRIMENTO_HASH_SHA256, guidValido, hashSha256Valido } from './arquivistica.helpers';

export function DestinacaoPage() {
  const toast = useToast();
  const avaliar = useAvaliarAptidaoDestinacoes();
  const autorizar = useAutorizarEliminacao();
  const registrar = useRegistrarEliminacao();

  const [promovidas, setPromovidas] = useState<number | null>(null);

  // Autorizacao
  const [fichaAutorizar, setFichaAutorizar] = useState('');
  const [autorizadoPor, setAutorizadoPor] = useState('');
  const [erroAutorizar, setErroAutorizar] = useState<string | null>(null);

  // Registro
  const [fichaRegistrar, setFichaRegistrar] = useState('');
  const [termoHash, setTermoHash] = useState('');
  const [editalRef, setEditalRef] = useState('');
  const [erroRegistrar, setErroRegistrar] = useState<string | null>(null);

  function executarAvaliacao(): void {
    avaliar.mutate(undefined, {
      onSuccess: (r) => {
        setPromovidas(r.promovidas);
        toast.success(
          r.promovidas === 0
            ? 'Varredura concluída: nenhuma ficha apta a eliminar.'
            : `${r.promovidas} ficha(s) promovida(s) para "apto a eliminar".`,
        );
      },
      onError: (erro) => {
        const msg = erro instanceof ApiError ? erro.message : 'Falha ao avaliar aptidão.';
        toast.error(msg);
      },
    });
  }

  function executarAutorizacao(): void {
    if (!guidValido(fichaAutorizar)) {
      setErroAutorizar('Informe um identificador de ficha de destinação válido (GUID).');
      return;
    }
    if (!guidValido(autorizadoPor)) {
      setErroAutorizar('Informe o identificador do autorizador (GUID).');
      return;
    }
    setErroAutorizar(null);
    autorizar.mutate(
      { destinacaoId: fichaAutorizar.trim(), input: { autorizadoPor: autorizadoPor.trim() } },
      {
        onSuccess: () => {
          toast.success('Eliminação autorizada. Registre agora o termo carimbado + edital.');
          setFichaRegistrar(fichaAutorizar.trim());
          setFichaAutorizar('');
          setAutorizadoPor('');
        },
        onError: (erro) => {
          // O backend rejeita I-T1 (antes do prazo) / I-T2 (guarda permanente) com ProblemDetails.
          const msg = erro instanceof ApiError ? erro.message : 'Falha ao autorizar a eliminação.';
          toast.error(msg);
        },
      },
    );
  }

  function executarRegistro(): void {
    if (!guidValido(fichaRegistrar)) {
      setErroRegistrar('Informe um identificador de ficha de destinação válido (GUID).');
      return;
    }
    if (!hashSha256Valido(termoHash)) {
      setErroRegistrar(`O hash do termo deve ter ${COMPRIMENTO_HASH_SHA256} caracteres hexadecimais (SHA-256).`);
      return;
    }
    if (editalRef.trim() === '') {
      setErroRegistrar('Informe a referência do edital de eliminação (Res. CONARQ 40/2014).');
      return;
    }
    setErroRegistrar(null);
    registrar.mutate(
      {
        destinacaoId: fichaRegistrar.trim(),
        input: { termoEliminacaoHash: termoHash.trim(), editalEliminacaoRef: editalRef.trim() },
      },
      {
        onSuccess: () => {
          toast.success('Eliminação registrada sob WORM (prova oponível ao TCE).');
          setFichaRegistrar('');
          setTermoHash('');
          setEditalRef('');
        },
        onError: (erro) => {
          const msg = erro instanceof ApiError ? erro.message : 'Falha ao registrar a eliminação.';
          toast.error(msg);
        },
      },
    );
  }

  return (
    <>
      <PageHeader
        eyebrow="Protocolo · Arquivística"
        title="Destinação de processos arquivados"
        description="Avalie a aptidão à eliminação, autorize por ato humano e registre o termo carimbado + edital. Protege o irreversível (I-T1..I-T7)."
      />
      <ProtocoloSubNav />

      <Alert variant="danger" className="mb-4" title="Salvaguardas do irreversível.">
        Não elimina <strong>antes do prazo</strong> integral de guarda (I-T1). Documentos em{' '}
        <strong>guarda permanente nunca são eliminados</strong> (I-T2). A eliminação exige ato humano
        (RBAC), termo assinado/carimbado e edital sob WORM (I-T4) — estado terminal não retrocede (I-T7).
      </Alert>

      {/* (1) Avaliar aptidao — varredura batch */}
      <Card className="mb-4" accent="primary">
        <Toolbar align="between" className="mb-3 align-items-center">
          <h3 className="text-up-01 mb-0">1. Avaliar aptidão à eliminação</h3>
          <Button variant="primary" onClick={executarAvaliacao} loading={avaliar.isPending}>
            <i className="fas fa-magnifying-glass-chart" aria-hidden="true" /> Avaliar aptidão
          </Button>
        </Toolbar>
        <p className="text-down-01 mb-3">
          Varre as fichas em “aguardando prazo” e promove para “apto a eliminar” apenas as cujo prazo
          de guarda já decorreu. Guarda permanente jamais é promovida.
        </p>
        {promovidas !== null && (
          <MetricaGrade>
            <Metrica label="Fichas promovidas nesta varredura" valor={String(promovidas)} />
          </MetricaGrade>
        )}
      </Card>

      {/* (2) Autorizar eliminacao — gated por protocolo.gerenciar */}
      <Can
        permission="protocolo.gerenciar"
        fallback={
          <Alert variant="info" className="mb-4">
            Você não possui permissão para autorizar ou registrar eliminações.
          </Alert>
        }
      >
        <Card className="mb-4" accent="warning">
          <h3 className="text-up-01 mb-3">2. Autorizar eliminação (ato humano · RBAC)</h3>
          <div className="row">
            <div className="col-12 col-md">
              <FormField label="Ficha de destinação (GUID)" required>
                {({ id, describedBy }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    value={fichaAutorizar}
                    onChange={(e) => setFichaAutorizar(e.target.value)}
                    placeholder="00000000-0000-0000-0000-000000000000"
                  />
                )}
              </FormField>
            </div>
            <div className="col-12 col-md">
              <FormField label="Autorizado por (GUID)" required>
                {({ id, describedBy }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    value={autorizadoPor}
                    onChange={(e) => setAutorizadoPor(e.target.value)}
                    placeholder="Identificador do servidor autorizador"
                  />
                )}
              </FormField>
            </div>
          </div>
          {erroAutorizar && (
            <Alert variant="warning" className="mt-2">
              {erroAutorizar}
            </Alert>
          )}
          <Toolbar className="mt-3">
            <Button variant="secondary" onClick={executarAutorizacao} loading={autorizar.isPending}>
              <i className="fas fa-user-shield" aria-hidden="true" /> Autorizar eliminação
            </Button>
          </Toolbar>
        </Card>

        {/* (3) Registrar eliminacao — termo carimbado + edital */}
        <Card className="mb-4" accent="danger">
          <h3 className="text-up-01 mb-3">3. Registrar eliminação (termo carimbado + edital)</h3>
          <div className="row">
            <div className="col-12 col-md">
              <FormField label="Ficha de destinação (GUID)" required>
                {({ id, describedBy }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    value={fichaRegistrar}
                    onChange={(e) => setFichaRegistrar(e.target.value)}
                    placeholder="00000000-0000-0000-0000-000000000000"
                  />
                )}
              </FormField>
            </div>
            <div className="col-12 col-md">
              <FormField label="Referência do edital de eliminação" required>
                {({ id, describedBy }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    value={editalRef}
                    maxLength={200}
                    onChange={(e) => setEditalRef(e.target.value)}
                    placeholder="Edital nº 00/AAAA — DOM"
                  />
                )}
              </FormField>
            </div>
          </div>
          <FormField label={`Hash SHA-256 do termo carimbado (${COMPRIMENTO_HASH_SHA256} hex)`} required>
            {({ id, describedBy }) => (
              <Input
                id={id}
                aria-describedby={describedBy}
                value={termoHash}
                maxLength={COMPRIMENTO_HASH_SHA256}
                onChange={(e) => setTermoHash(e.target.value)}
                placeholder="e3b0c44298fc1c149afbf4c8996fb924…"
              />
            )}
          </FormField>
          {erroRegistrar && (
            <Alert variant="warning" className="mt-2">
              {erroRegistrar}
            </Alert>
          )}
          <Toolbar className="mt-3">
            <Button variant="primary" onClick={executarRegistro} loading={registrar.isPending}>
              <i className="fas fa-fire" aria-hidden="true" /> Registrar eliminação
            </Button>
          </Toolbar>
        </Card>
      </Can>

      {/* Carimbo de tempo (Local x ACT) — orientacao normativa (I-CT3) */}
      <Card>
        <h3 className="text-up-01 mb-3">
          Carimbo de tempo do termo <Tag variant="info">Local</Tag> <Tag variant="success">ACT</Tag>
        </h3>
        <p className="text-down-01 mb-2">
          O termo de eliminação é assinado e carimbado no tempo (oponibilidade ao TCE). A origem do
          carimbo determina a validade do ato:
        </p>
        <ul className="text-down-01">
          <li>
            <strong>ACT credenciada</strong> (RFC 3161 / ICP-Brasil) — exigida para atos de
            criticidade alta (assinatura qualificada). O hash atestado pela ACT deve coincidir com o
            hash do termo (vínculo hash↔token).
          </li>
          <li>
            <strong>Relógio local</strong> — fallback de desenvolvimento; <strong>não satisfaz</strong>{' '}
            atos qualificados e não deve carimbar termos de eliminação em produção.
          </li>
        </ul>
        <Alert variant="info" className="mb-0">
          A origem efetiva do carimbo é determinada no servidor no momento da assinatura (I-CT3); este
          painel não recebe esse status do contrato atual — registre apenas o hash do termo carimbado.
        </Alert>
      </Card>
    </>
  );
}
