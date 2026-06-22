// Página de Login. Topbar institucional + card centralizado, formulário acessível
// (FormField), estado de loading/erro e retorno ao destino original (state.from).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../auth/useAuth';
import { ApiError } from '../../api/problemDetails';
import { Alert, Button, Card, FormField, Input } from '../../components/ui';
import { Header } from '../shell/Header';
import { Footer } from '../shell/Footer';

interface LocationState {
  from?: { pathname: string };
}

export function LoginPage() {
  const { isAuthenticated, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const destino = (location.state as LocationState | null)?.from?.pathname ?? '/';

  const [email, setEmail] = useState('');
  const [senha, setSenha] = useState('');
  const [erro, setErro] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  if (isAuthenticated) {
    return <Navigate to={destino} replace />;
  }

  async function entrar(event: FormEvent): Promise<void> {
    event.preventDefault();
    setErro(null);
    setEnviando(true);
    try {
      await login({ email: email.trim(), senha });
      navigate(destino, { replace: true });
    } catch (causa) {
      if (causa instanceof ApiError && causa.status === 401) {
        setErro('E-mail ou senha inválidos.');
      } else if (causa instanceof ApiError) {
        setErro(causa.userMessage);
      } else {
        setErro('Não foi possível entrar. Tente novamente.');
      }
    } finally {
      setEnviando(false);
    }
  }

  return (
    <div className="app-shell">
      <a className="br-skip-link" href="#conteudo" accessKey="1">
        Ir para o conteúdo
      </a>
      <Header />
      <main id="conteudo" className="tg-login-wrap" tabIndex={-1}>
        <div className="tg-login-card">
          <Card header={<h1 className="mb-0 text-up-01">Acessar o sistema</h1>}>
            <form className="br-form" onSubmit={entrar} noValidate>
              <div className="stack">
                {erro && <Alert variant="danger">{erro}</Alert>}

                <FormField label="E-mail" required>
                  {({ id, describedBy, invalid }) => (
                    <Input
                      id={id}
                      type="email"
                      autoComplete="username"
                      aria-describedby={describedBy}
                      invalid={invalid}
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      required
                    />
                  )}
                </FormField>

                <FormField label="Senha" required>
                  {({ id, describedBy, invalid }) => (
                    <Input
                      id={id}
                      type="password"
                      autoComplete="current-password"
                      aria-describedby={describedBy}
                      invalid={invalid}
                      value={senha}
                      onChange={(e) => setSenha(e.target.value)}
                      required
                    />
                  )}
                </FormField>

                <Button
                  variant="primary"
                  type="submit"
                  block
                  loading={enviando}
                  disabled={email.trim() === '' || senha === ''}
                >
                  Entrar
                </Button>
              </div>
            </form>
          </Card>
          <p className="tg-login-hint">
            Ambiente de demonstração — entre com qualquer e-mail e senha.
          </p>
        </div>
      </main>
      <Footer />
    </div>
  );
}
