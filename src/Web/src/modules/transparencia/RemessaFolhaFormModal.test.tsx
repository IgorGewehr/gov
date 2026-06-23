// Teste do formulário da REMESSA DE FOLHA (Res. 1099). Cobre: envio ao endpoint
// dedicado /remessas-folha-tce com o shape do contrato { exercicio, mes, leiauteVersao }.
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import { clearAccessToken } from '../../api/authToken';
import { renderWithAuth } from './test-utils';
import { RemessaFolhaFormModal } from './RemessaFolhaFormModal';

describe('RemessaFolhaFormModal', () => {
  beforeEach(() => vi.restoreAllMocks());
  afterEach(() => {
    vi.restoreAllMocks();
    clearAccessToken();
  });

  it('envia exercício, mês e versão do leiaute ao endpoint de folha', async () => {
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(JSON.stringify({ id: 'remessa-folha-1' }), {
        status: 200,
        headers: { 'content-type': 'application/json' },
      }),
    );

    renderWithAuth(<RemessaFolhaFormModal open onClose={() => {}} exercicioInicial={2026} />);

    fireEvent.change(screen.getByLabelText(/Mês/i), { target: { value: '3' } });
    fireEvent.click(screen.getByRole('button', { name: /^Gerar$/i }));

    await waitFor(() => expect(fetchSpy).toHaveBeenCalled());
    const [url, init] = fetchSpy.mock.calls[0];
    expect(String(url)).toContain('/transparencia/remessas-folha-tce');
    expect(init?.method).toBe('POST');
    expect(JSON.parse(String(init?.body))).toEqual({
      exercicio: 2026,
      mes: 3,
      leiauteVersao: '1099',
    });
  });
});
