// Modal de GERAÇÃO dos eventos NÃO-PERIÓDICOS do servidor: S-2200 (admissão) e
// S-2299 (desligamento), a partir do agregado Servidor. O servidor é escolhido na
// lista de ativos; codCateg/codCargo/mtvDeslig seguem as tabelas oficiais do leiaute.
import { useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useServidoresAtivos } from './servidor.api';
import { useGerarS2200, useGerarS2299 } from './esocial.api';

type Tipo = 's2200' | 's2299';

export interface GerarEventoServidorModalProps {
  open: boolean;
  onClose: () => void;
}

export function GerarEventoServidorModal({ open, onClose }: GerarEventoServidorModalProps) {
  const toast = useToast();
  const servidores = useServidoresAtivos();

  const [tipo, setTipo] = useState<Tipo>('s2200');
  const [servidorId, setServidorId] = useState('');
  const [codCateg, setCodCateg] = useState('');
  const [codCargo, setCodCargo] = useState('');
  const [vrSalFx, setVrSalFx] = useState('');
  const [mtvDeslig, setMtvDeslig] = useState('');
  const [erro, setErro] = useState<string | null>(null);

  const s2200 = useGerarS2200();
  const s2299 = useGerarS2299();
  const pendente = s2200.isPending || s2299.isPending;

  const opcoesServidores = useMemo(
    () =>
      (servidores.data ?? []).map((s) => ({
        value: s.id,
        label: `${s.nomeServidor} (mat. ${s.matricula})`,
      })),
    [servidores.data],
  );

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

    if (servidorId.trim() === '') {
      setErro('Selecione o servidor.');
      return;
    }

    if (tipo === 's2200') {
      if (codCateg.trim() === '') {
        setErro('Informe a categoria (codCateg — Tabela 01).');
        return;
      }
      const salario = Number(vrSalFx);
      if (vrSalFx.trim() === '' || Number.isNaN(salario) || salario <= 0) {
        setErro('Informe o salário fixo (maior que zero).');
        return;
      }
      s2200.mutate(
        {
          servidorId,
          codCateg: codCateg.trim(),
          codCargo: codCargo.trim(),
          vrSalFx: salario,
        },
        { onSuccess: () => aoSucesso('S-2200'), onError: aoErro },
      );
      return;
    }

    // s2299
    if (mtvDeslig.trim() === '') {
      setErro('Informe o motivo do desligamento (mtvDeslig — Tabela 19).');
      return;
    }
    s2299.mutate(
      { servidorId, mtvDeslig: mtvDeslig.trim() },
      { onSuccess: () => aoSucesso('S-2299'), onError: aoErro },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Gerar evento do servidor"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={pendente}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-gerar-servidor" loading={pendente}>
            Gerar
          </Button>
        </>
      }
    >
      <form id="form-gerar-servidor" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Tipo de evento" required>
          {({ id, describedBy }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              value={tipo}
              onChange={(e) => setTipo(e.target.value as Tipo)}
              options={[
                { value: 's2200', label: 'S-2200 — Admissão' },
                { value: 's2299', label: 'S-2299 — Desligamento' },
              ]}
            />
          )}
        </FormField>

        <FormField
          label="Servidor"
          required
          help={
            tipo === 's2299'
              ? 'O S-2299 exige que o servidor já esteja desligado.'
              : undefined
          }
        >
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={servidorId}
              onChange={(e) => setServidorId(e.target.value)}
              placeholder={servidores.isLoading ? 'Carregando…' : 'Selecione o servidor'}
              options={opcoesServidores}
            />
          )}
        </FormField>

        {tipo === 's2200' && (
          <>
            <FormField label="Categoria (codCateg — Tabela 01)" required>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={codCateg}
                  onChange={(e) => setCodCateg(e.target.value)}
                  maxLength={3}
                  inputMode="numeric"
                />
              )}
            </FormField>
            <FormField label="Código do cargo">
              {({ id, describedBy }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  value={codCargo}
                  onChange={(e) => setCodCargo(e.target.value)}
                  maxLength={30}
                />
              )}
            </FormField>
            <FormField label="Salário fixo (R$)" required>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="0"
                  step="0.01"
                  inputMode="decimal"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={vrSalFx}
                  onChange={(e) => setVrSalFx(e.target.value)}
                />
              )}
            </FormField>
          </>
        )}

        {tipo === 's2299' && (
          <FormField label="Motivo do desligamento (mtvDeslig — Tabela 19)" required>
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                aria-describedby={describedBy}
                invalid={invalid}
                value={mtvDeslig}
                onChange={(e) => setMtvDeslig(e.target.value)}
                maxLength={2}
                inputMode="numeric"
              />
            )}
          </FormField>
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
