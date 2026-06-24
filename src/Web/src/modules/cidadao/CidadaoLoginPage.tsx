// Acesso do CIDADAO (ator externo): login local por CPF/CNPJ + senha OU cadastro
// self-service, ambos no MUNICIPIO escolhido. Coexiste com o login do admin (/login):
// fluxo, token e shell sao proprios do realm cidadao. gov.br DS, a11y, linguagem leiga.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { Alert, Button, Card, FormField, Input, Select } from '../../components/ui';
import { Footer } from '../../app/shell/Footer';
import { CidadaoTopbar } from './CidadaoTopbar';
import { ApiError } from '../../api/problemDetails';
import { useCidadaoAuth } from './useCidadaoAuth';
import { cidadaoApi } from './cidadaoApi';
import { municipiosDisponiveis } from './cidadao.helpers';

interface LocationState {
  from?: { pathname: string };
}

type Aba = 'entrar' | 'cadastrar';

export function CidadaoLoginPage() {
  const { isAutenticado, entrar } = useCidadaoAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const destino = (location.state as LocationState | null)?.from?.pathname ?? '/portal-cidadao';
  const municipios = municipiosDisponiveis();

  const [aba, setAba] = useState<Aba>('entrar');
  const [tenantId, setTenantId] = useState(municipios.length === 1 ? municipios[0].tenantId : '');
  const [documento, setDocumento] = useState('');
  const [senha, setSenha] = useState('');
  const [nome, setNome] = useState('');
  const [email, setEmail] = useState('');
  const [erro, setErro] = useState<string | null>(null);
  const [sucesso, setSucesso] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  if (isAutenticado) return <Navigate to={destino} replace />;

  const semMunicipios = municipios.length === 0;
  const baseInvalida = tenantId === '' || documento.trim() === '' || senha === '';
  const formInvalido = aba === 'entrar' ? baseInvalida : baseInvalida || nome.trim() === '';

  function trocarAba(proxima: Aba): void {
    setAba(proxima);
    setErro(null);
    setSucesso(null);
  }

  async function onSubmit(event: FormEvent): Promise<void> {
    event.preventDefault();
    setErro(null);
    setSucesso(null);
    setEnviando(true);
    try {
      if (aba === 'entrar') {
        await entrar({ tenantId, documento: documento.trim(), senha });
        navigate(destino, { replace: true });
        return;
      }
      await cidadaoApi.cadastrar({
        tenantId,
        documento: documento.trim(),
        nome: nome.trim(),
        senha,
        email: email.trim() || null,
      });
      // Cadastrou: ja loga em seguida para uma jornada sem atrito.
      await entrar({ tenantId, documento: documento.trim(), senha });
      navigate(destino, { replace: true });
    } catch (causa) {
      if (causa instanceof ApiError && causa.status === 401) {
        setErro('CPF/CNPJ ou senha invalidos.');
      } else if (causa instanceof ApiError && causa.status === 404) {
        setErro('Municipio nao disponivel para este acesso.');
      } else if (causa instanceof ApiError) {
        setErro(causa.userMessage);
      } else {
        setErro('Nao foi possivel concluir. Tente novamente.');
      }
    } finally {
      setEnviando(false);
    }
  }

  return (
    <div className="app-shell">
      <a className="br-skip-link" href="#conteudo" accessKey="1">
        Ir para o conteudo
      </a>
      <CidadaoTopbar />
      <main id="conteudo" className="tg-login-wrap" tabIndex={-1}>
        <div className="tg-login-card">
          <Card header={<h1 className="mb-0 text-up-01">Portal do Cidadao</h1>}>
            <div className="br-tab mb-3" role="tablist" aria-label="Tipo de acesso">
              <nav className="tab-nav">
                <ul>
                  <li className={`tab-item${aba === 'entrar' ? ' is-active' : ''}`} role="presentation">
                    <button type="button" role="tab" aria-selected={aba === 'entrar'} onClick={() => trocarAba('entrar')}>
                      <span className="name">Entrar</span>
                    </button>
                  </li>
                  <li className={`tab-item${aba === 'cadastrar' ? ' is-active' : ''}`} role="presentation">
                    <button type="button" role="tab" aria-selected={aba === 'cadastrar'} onClick={() => trocarAba('cadastrar')}>
                      <span className="name">Criar conta</span>
                    </button>
                  </li>
                </ul>
              </nav>
            </div>

            <form className="br-form" onSubmit={onSubmit} noValidate>
              <div className="stack">
                {erro && <Alert variant="danger">{erro}</Alert>}
                {sucesso && <Alert variant="success">{sucesso}</Alert>}
                {semMunicipios && (
                  <Alert variant="warning">
                    Nenhum municipio disponivel para acesso neste momento.
                  </Alert>
                )}

                <FormField label="Municipio" required>
                  {({ id, describedBy, invalid }) => (
                    <Select
                      id={id}
                      aria-describedby={describedBy}
                      invalid={invalid}
                      placeholder="Selecione seu municipio"
                      options={municipios.map((m) => ({ value: m.tenantId, label: m.nome }))}
                      value={tenantId}
                      onChange={(e) => setTenantId(e.target.value)}
                      disabled={semMunicipios}
                      required
                    />
                  )}
                </FormField>

                {aba === 'cadastrar' && (
                  <FormField label="Nome ou razao social" required>
                    {({ id, describedBy, invalid }) => (
                      <Input
                        id={id}
                        autoComplete="name"
                        aria-describedby={describedBy}
                        invalid={invalid}
                        value={nome}
                        onChange={(e) => setNome(e.target.value)}
                        required
                      />
                    )}
                  </FormField>
                )}

                <FormField label="CPF ou CNPJ" required help="Apenas voce acessa seus proprios dados.">
                  {({ id, describedBy, invalid }) => (
                    <Input
                      id={id}
                      inputMode="numeric"
                      autoComplete="username"
                      aria-describedby={describedBy}
                      invalid={invalid}
                      value={documento}
                      onChange={(e) => setDocumento(e.target.value)}
                      required
                    />
                  )}
                </FormField>

                {aba === 'cadastrar' && (
                  <FormField label="E-mail (opcional)">
                    {({ id, describedBy, invalid }) => (
                      <Input
                        id={id}
                        type="email"
                        autoComplete="email"
                        aria-describedby={describedBy}
                        invalid={invalid}
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                      />
                    )}
                  </FormField>
                )}

                <FormField
                  label="Senha"
                  required
                  help={aba === 'cadastrar' ? 'Minimo de 8 caracteres.' : undefined}
                >
                  {({ id, describedBy, invalid }) => (
                    <Input
                      id={id}
                      type="password"
                      autoComplete={aba === 'cadastrar' ? 'new-password' : 'current-password'}
                      aria-describedby={describedBy}
                      invalid={invalid}
                      value={senha}
                      onChange={(e) => setSenha(e.target.value)}
                      required
                    />
                  )}
                </FormField>

                <Button variant="primary" type="submit" block loading={enviando} disabled={formInvalido || semMunicipios}>
                  {aba === 'entrar' ? 'Entrar' : 'Criar conta e entrar'}
                </Button>

                <Button variant="secondary" type="button" block disabled title="Disponivel em breve">
                  <i className="fab fa-react" aria-hidden="true" /> Entrar com gov.br (em breve)
                </Button>
              </div>
            </form>
          </Card>
          <p className="tg-login-hint">
            Acesso do cidadao. Servidores da prefeitura entram em <a href="/login">acesso interno</a>.
          </p>
        </div>
      </main>
      <Footer />
    </div>
  );
}
