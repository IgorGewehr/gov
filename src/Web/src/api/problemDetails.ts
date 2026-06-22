// Modelo RFC 7807 (ProblemDetails) — formato de erro padrão da API .NET (ASP.NET Core).
// Erros de validação usam o campo "errors" (ValidationProblemDetails).

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  /** Erros de validação por campo (ValidationProblemDetails). */
  errors?: Record<string, string[]>;
  /** Campos de extensão arbitrários. */
  [key: string]: unknown;
}

/**
 * Erro de aplicação tipado, lançado pelo http client. Carrega o status HTTP e,
 * quando disponível, o ProblemDetails da API para feedback preciso ao usuário.
 */
export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails | null;

  constructor(message: string, status: number, problem: ProblemDetails | null = null) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
  }

  /** Erros de validação por campo, prontos para mapear em FormField. */
  get fieldErrors(): Record<string, string[]> {
    return this.problem?.errors ?? {};
  }

  /** Mensagem amigável (PT-BR) priorizando detail/title do ProblemDetails. */
  get userMessage(): string {
    return this.problem?.detail ?? this.problem?.title ?? this.message;
  }
}
