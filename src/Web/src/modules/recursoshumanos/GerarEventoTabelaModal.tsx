// Modal de GERAÇÃO dos eventos de TABELA do eSocial: S-1000 (empregador), S-1005
// (estabelecimento) e S-1010 (rubrica). Cada um tem o seu insumo próprio. O S-1000 é
// gerado da configuração do ente (sem entrada). Geração é idempotente no backend.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { MESES } from './recursosHumanos.helpers';
import {
  useGerarS1000,
  useGerarS1005,
  useGerarS1010,
} from './esocial.api';

type Tipo = 's1000' | 's1005' | 's1010';

const ANO_ATUAL = new Date().getFullYear();

export interface GerarEventoTabelaModalProps {
  open: boolean;
  onClose: () => void;
}

export function GerarEventoTabelaModal({ open, onClose }: GerarEventoTabelaModalProps) {
  const toast = useToast();
  const [tipo, setTipo] = useState<Tipo>('s1000');

  const [cnpj, setCnpj] = useState('');
  const [cnae, setCnae] = useState('');
  const [codigoRubrica, setCodigoRubrica] = useState('');
  const [ano, setAno] = useState(String(ANO_ATUAL));
  const [mes, setMes] = useState(String(new Date().getMonth() + 1));
  const [erro, setErro] = useState<string | null>(null);

  const s1000 = useGerarS1000();
  const s1005 = useGerarS1005();
  const s1010 = useGerarS1010();

  const pendente = s1000.isPending || s1005.isPending || s1010.isPending;

  function fechar(): void {
    setErro(null);
    onClose();
  }

  function aoErro(error: unknown): void {
    toast.error(
      error instanceof ApiError ? error.userMessage : 'Não foi possível gerar o evento.',
    );
  }

  function aoSucesso(rotulo: string): void {
    toast.success(`Evento ${rotulo} gerado.`, 'Sucesso');
    fechar();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    setErro(null);

    if (tipo === 's1000') {
      s1000.mutate(undefined, { onSuccess: () => aoSucesso('S-1000'), onError: aoErro });
      return;
    }

    if (tipo === 's1005') {
      if (cnpj.trim() === '') {
        setErro('Informe o CNPJ do estabelecimento.');
        return;
      }
      if (cnae.trim() === '') {
        setErro('Informe o CNAE preponderante.');
        return;
      }
      s1005.mutate(
        { cnpjEstabelecimento: cnpj.trim(), cnaePreponderante: cnae.trim() },
        { onSuccess: () => aoSucesso('S-1005'), onError: aoErro },
      );
      return;
    }

    // s1010
    if (codigoRubrica.trim() === '') {
      setErro('Informe o código da rubrica.');
      return;
    }
    s1010.mutate(
      { codigoRubrica: codigoRubrica.trim(), ano: Number(ano), mes: Number(mes) },
      { onSuccess: () => aoSucesso('S-1010'), onError: aoErro },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Gerar evento de tabela"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={pendente}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-gerar-tabela" loading={pendente}>
            Gerar
          </Button>
        </>
      }
    >
      <form id="form-gerar-tabela" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Tipo de evento" required>
          {({ id, describedBy }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              value={tipo}
              onChange={(e) => setTipo(e.target.value as Tipo)}
              options={[
                { value: 's1000', label: 'S-1000 — Empregador (do ente)' },
                { value: 's1005', label: 'S-1005 — Estabelecimento' },
                { value: 's1010', label: 'S-1010 — Rubrica' },
              ]}
            />
          )}
        </FormField>

        {tipo === 's1000' && (
          <Alert variant="info" title="S-1000 — Empregador.">
            Gerado a partir da configuração do ente (CNPJ, classificação tributária, início de
            validade). É o primeiro evento e pré-requisito dos demais. Operação idempotente.
          </Alert>
        )}

        {tipo === 's1005' && (
          <>
            <FormField label="CNPJ do estabelecimento" required>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={cnpj}
                  onChange={(e) => setCnpj(e.target.value)}
                  maxLength={14}
                  inputMode="numeric"
                />
              )}
            </FormField>
            <FormField label="CNAE preponderante" required>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={cnae}
                  onChange={(e) => setCnae(e.target.value)}
                  maxLength={7}
                  inputMode="numeric"
                />
              )}
            </FormField>
          </>
        )}

        {tipo === 's1010' && (
          <>
            <FormField
              label="Código da rubrica"
              required
              help="A rubrica deve estar vigente na competência informada."
            >
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={codigoRubrica}
                  onChange={(e) => setCodigoRubrica(e.target.value)}
                  maxLength={30}
                />
              )}
            </FormField>
            <div className="row">
              <div className="col-sm-6">
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
              <div className="col-sm-6">
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
            </div>
          </>
        )}

        {erro && (
          <Alert variant="danger" title="Verifique os campos.">
            {erro}
          </Alert>
        )}
      </form>
    </Modal>
  );
}
