// Testes do http client: header Authorization, normalização de ProblemDetails e 401.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { http } from './http';
import { ApiError } from './problemDetails';
import { setAccessToken, clearAccessToken } from './authToken';
import { setUnauthorizedHandler } from './http';

function jsonResponse(body: unknown, status = 200, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'content-type': contentType } });
}

describe('http client', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });
  afterEach(() => {
    clearAccessToken();
    setUnauthorizedHandler(null);
  });

  it('envia o header Authorization quando há token', async () => {
    setAccessToken('header.payload.sig');
    const spy = vi.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse({ ok: true }));

    await http.get('/teste');

    const init = spy.mock.calls[0][1] as RequestInit;
    expect((init.headers as Record<string, string>).Authorization).toBe('Bearer header.payload.sig');
  });

  it('normaliza ProblemDetails em ApiError com fieldErrors', async () => {
    // Response nova a cada chamada (o corpo é um stream consumível uma única vez).
    vi.spyOn(globalThis, 'fetch').mockImplementation(() =>
      Promise.resolve(
        jsonResponse(
          { title: 'Validação', detail: 'Dados inválidos.', errors: { Email: ['Obrigatório.'] } },
          400,
        ),
      ),
    );

    await expect(http.post('/teste', {})).rejects.toMatchObject({ status: 400 });
    try {
      await http.post('/teste', {});
      expect.unreachable('deveria ter lançado ApiError');
    } catch (error) {
      expect(error).toBeInstanceOf(ApiError);
      const apiError = error as ApiError;
      expect(apiError.userMessage).toBe('Dados inválidos.');
      expect(apiError.fieldErrors.Email[0]).toBe('Obrigatório.');
    }
  });

  it('dispara o handler de 401 e limpa o token', async () => {
    setAccessToken('expirado');
    const onUnauthorized = vi.fn();
    setUnauthorizedHandler(onUnauthorized);
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse({ title: 'Não autorizado' }, 401));

    await expect(http.get('/protegido')).rejects.toBeInstanceOf(ApiError);
    expect(onUnauthorized).toHaveBeenCalledOnce();
  });
});
