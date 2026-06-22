// Mapeia ProblemDetails.fieldErrors (PascalCase do backend) para chaves de formulário
// (camelCase). `conhecidos` restringe aos campos que o formulário sabe exibir.
import { ApiError } from '../../api/problemDetails';

export function mapearErros(
  error: unknown,
  conhecidos: Record<string, number>,
): Record<string, string> {
  const mapped: Record<string, string> = {};
  if (error instanceof ApiError) {
    for (const [field, messages] of Object.entries(error.fieldErrors)) {
      const leaf = field.split('.').pop() ?? field;
      const key = leaf.charAt(0).toLowerCase() + leaf.slice(1);
      if (key in conhecidos) mapped[key] = messages[0];
    }
  }
  return mapped;
}
