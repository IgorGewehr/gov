// Plano de Cargos e Salários (PCCS): lista os planos do tenant e institui novos (matriz classe x
// referência, vencimento derivado sem número mágico). A grade salarial completa é vista no detalhe.
// Enquadramento/progressão/promoção do servidor ficam na ficha funcional (aba PCCS do servidor).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link } from 'react-router-dom';
import {
  Button,
  DataTable,
  EmptyState,
  FormField,
  Input,
  Modal,
  PageHeader,
  Tag,
  Toolbar,
  useToast,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { Can } from '../../auth/Can';
import { ApiError } from '../../api/problemDetails';
import { formatarMoeda } from '../../i18n/format';
import { usePlanosCarreira, useInstituirPlanoCarreira } from './api';
import type { PlanoCarreiraResumo, InstituirPlanoInput } from './api';
import { PERM_RH_GERENCIAR, situacaoPlanoCarreiraTagVariant } from './recursosHumanos.helpers';
import { RhSubNav } from './RhSubNav';

function InstituirPlanoModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  const toast = useToast();
  const mutation = useInstituirPlanoCarreira();
  const [form, setForm] = useState<InstituirPlanoInput>({
    denominacaoCarreira: '',
    leiInstituicao: '',
    vencimentoBase: 0,
    numeroClasses: 1,
    numeroReferencias: 1,
    percentualEntreReferencias: 0,
    percentualEntreClasses: 0,
    intersticioMeses: 0,
    notaMinimaProgressao: 0,
  });
  const [erro, setErro] = useState<string | undefined>();

  function set<K extends keyof InstituirPlanoInput>(chave: K, valor: InstituirPlanoInput[K]): void {
    setForm((atual) => ({ ...atual, [chave]: valor }));
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (form.denominacaoCarreira.trim() === '' || form.leiInstituicao.trim() === '') {
      setErro('Denominação da carreira e lei de instituição são obrigatórias.');
      return;
    }
    if (form.vencimentoBase <= 0 || form.numeroClasses < 1 || form.numeroReferencias < 1) {
      setErro('Vencimento-base, classes e referências devem ser positivos.');
      return;
    }
    setErro(undefined);

    mutation.mutate(form, {
      onSuccess: () => {
        toast.success('Plano de carreira instituído.', 'Sucesso');
        onClose();
      },
      onError: (error) =>
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível instituir o plano.',
        ),
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Instituir plano de carreira"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-plano" loading={mutation.isPending}>
            Instituir
          </Button>
        </>
      }
    >
      <form id="form-plano" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Denominação da carreira" required error={erro}>
          {({ id, describedBy }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              value={form.denominacaoCarreira}
              onChange={(e) => set('denominacaoCarreira', e.target.value)}
              placeholder="Ex.: Magistério Municipal"
            />
          )}
        </FormField>

        <FormField label="Lei de instituição" required>
          {({ id, describedBy }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              value={form.leiInstituicao}
              onChange={(e) => set('leiInstituicao', e.target.value)}
              placeholder="Ex.: Lei Municipal nº 1.234/2020"
            />
          )}
        </FormField>

        <div className="row">
          <div className="col-md-4">
            <FormField label="Vencimento-base (R$)" help="Célula de ingresso (classe 1, referência 1).">
              {({ id }) => (
                <Input
                  id={id}
                  type="number"
                  min="0"
                  step="0.01"
                  inputMode="decimal"
                  value={String(form.vencimentoBase)}
                  onChange={(e) => set('vencimentoBase', Number(e.target.value))}
                />
              )}
            </FormField>
          </div>
          <div className="col-md-4">
            <FormField label="Nº de classes">
              {({ id }) => (
                <Input
                  id={id}
                  type="number"
                  min="1"
                  value={String(form.numeroClasses)}
                  onChange={(e) => set('numeroClasses', Number(e.target.value))}
                />
              )}
            </FormField>
          </div>
          <div className="col-md-4">
            <FormField label="Nº de referências">
              {({ id }) => (
                <Input
                  id={id}
                  type="number"
                  min="1"
                  value={String(form.numeroReferencias)}
                  onChange={(e) => set('numeroReferencias', Number(e.target.value))}
                />
              )}
            </FormField>
          </div>
        </div>

        <div className="row">
          <div className="col-md-6">
            <FormField label="% entre referências" help="Acréscimo horizontal (step).">
              {({ id }) => (
                <Input
                  id={id}
                  type="number"
                  min="0"
                  step="0.0001"
                  value={String(form.percentualEntreReferencias)}
                  onChange={(e) => set('percentualEntreReferencias', Number(e.target.value))}
                />
              )}
            </FormField>
          </div>
          <div className="col-md-6">
            <FormField label="% entre classes" help="Acréscimo vertical (salto de classe).">
              {({ id }) => (
                <Input
                  id={id}
                  type="number"
                  min="0"
                  step="0.0001"
                  value={String(form.percentualEntreClasses)}
                  onChange={(e) => set('percentualEntreClasses', Number(e.target.value))}
                />
              )}
            </FormField>
          </div>
        </div>

        <div className="row">
          <div className="col-md-6">
            <FormField label="Interstício (meses)" help="Tempo mínimo para a progressão horizontal.">
              {({ id }) => (
                <Input
                  id={id}
                  type="number"
                  min="0"
                  value={String(form.intersticioMeses)}
                  onChange={(e) => set('intersticioMeses', Number(e.target.value))}
                />
              )}
            </FormField>
          </div>
          <div className="col-md-6">
            <FormField label="Nota mínima (0–100)" help="Avaliação mínima para progressão por mérito.">
              {({ id }) => (
                <Input
                  id={id}
                  type="number"
                  min="0"
                  max="100"
                  step="0.01"
                  value={String(form.notaMinimaProgressao)}
                  onChange={(e) => set('notaMinimaProgressao', Number(e.target.value))}
                />
              )}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}

export function PlanosCarreiraListPage() {
  const [instituindo, setInstituindo] = useState(false);
  const query = usePlanosCarreira();

  const columns: Column<PlanoCarreiraResumo>[] = [
    {
      key: 'denominacao',
      header: 'Carreira',
      sortAccessor: (p) => p.denominacaoCarreira,
      render: (p) => <span className="text-semi-bold">{p.denominacaoCarreira}</span>,
    },
    { key: 'lei', header: 'Lei', render: (p) => p.leiInstituicao },
    {
      key: 'grade',
      header: 'Grade',
      render: (p) => `${p.numeroClasses} classes × ${p.numeroReferencias} referências`,
    },
    {
      key: 'base',
      header: 'Vencimento-base',
      align: 'end',
      sortAccessor: (p) => p.vencimentoBase,
      render: (p) => formatarMoeda(p.vencimentoBase),
    },
    {
      key: 'situacao',
      header: 'Situação',
      render: (p) => (
        <Tag variant={situacaoPlanoCarreiraTagVariant(p.situacao)}>{p.situacao}</Tag>
      ),
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (p) => (
        <Link className="br-button secondary small" to={`/recursoshumanos/planos-carreira/${p.id}`}>
          Matriz salarial
        </Link>
      ),
    },
  ];

  return (
    <>
      <RhSubNav />
      <PageHeader
        eyebrow="Recursos Humanos"
        title="Planos de Carreira (PCCS)"
        description="Planos de cargos, carreiras e salários: matriz classe × referência com vencimento derivado, progressão horizontal (tempo/avaliação) e promoção vertical (titulação/antiguidade)."
        actions={
          <Can permission={PERM_RH_GERENCIAR}>
            <Toolbar>
              <Button variant="primary" onClick={() => setInstituindo(true)}>
                <i className="fas fa-sitemap" aria-hidden="true" /> Instituir plano
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <DataTable
        caption="Planos de carreira do ente"
        columns={columns}
        rows={query.data}
        rowKey={(p) => p.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-sitemap"
            title="Nenhum plano de carreira"
            description="Institua um plano para enquadrar servidores e gerir progressões/promoções."
          />
        }
      />

      <InstituirPlanoModal open={instituindo} onClose={() => setInstituindo(false)} />
    </>
  );
}
