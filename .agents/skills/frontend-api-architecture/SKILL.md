---
name: frontend-api-architecture
description: >-
  Use esta skill sempre que for arquitetar, criar ou modificar o Frontend Web.
  Implementa as melhores práticas recomendadas pelo mercado para consumo de REST APIs:
  Next.js (App Router) / React + TypeScript estrito, TanStack Query v5, sincronização automática
  OpenAPI, React Hook Form + Zod, Supabase Auth e testes automatizados (Vitest, Testing Library, MSW, Playwright).
---

# Arquitetura Frontend Recomendada de Mercado para Consumo de API (`frontend-api-architecture`)

Esta skill consolida o padrão mais robusto, documentado e testável do mercado para aplicações Frontend que consomem uma REST API C# e compartilham autenticação com **Supabase Auth**.

---

## 1. Princípios Arquiteturais (Clean Code, KISS & Mobile-Ready)

1. **Separação Estrita entre Estado de Servidor e Estado de UI**:
   - **Estado de Servidor (REST API C#)**: Gerenciado **100% pelo TanStack Query v5**. Proibido guardar respostas de API em `useState` ou Redux/Zustand global.
   - **Estado de UI Local**: `useState` / `useReducer` simples (ou URL Search Params para filtros, paginação e ordenação — facilita compartilhamento de links e SSR).
2. **Tipagem End-to-End Sem Redundância (`OpenAPI -> TypeScript`)**:
   - Gere os tipos a partir do Swagger/OpenAPI da Minimal API C#:
     `npx openapi-typescript http://localhost:5000/openapi/v1.json -o src/lib/api/schema.d.ts`
3. **Código Reaproveitável para Futuro App Mobile**:
   - Mantenha a camada `features/<dominio>/api/` e `features/<dominio>/schemas/` livre de dependências exclusivas do DOM (`window`, `document`), permitindo reutilização direta em um futuro app **React Native / Expo**.

---

## 2. Padrão de Cliente HTTP + Supabase Auth + ProblemDetails

Crie um wrapper limpo e tipado (`src/lib/api/client.ts`) que injeta automaticamente o JWT do Supabase e converte erros RFC 7807 (`ProblemDetails`) da Minimal API C#:

```typescript
import { createBrowserClient } from '@supabase/ssr';

export interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  constructor(public readonly problem: ProblemDetails) {
    super(problem.detail || problem.title);
    this.name = 'ApiError';
  }
}

export async function apiFetch<TResponse>(
  endpoint: string,
  options: RequestInit = {}
): Promise<TResponse> {
  const supabase = createBrowserClient(
    process.env.NEXT_PUBLIC_SUPABASE_URL!,
    process.env.NEXT_PUBLIC_SUPABASE_ANON_KEY!
  );
  const { data: { session } } = await supabase.auth.getSession();

  const headers = new Headers(options.headers);
  headers.set('Content-Type', 'application/json');
  if (session?.access_token) {
    headers.set('Authorization', `Bearer ${session.access_token}`);
  }

  const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}${endpoint}`, {
    ...options,
    headers,
  });

  if (!response.ok) {
    const problem: ProblemDetails = await response.json().catch(() => ({
      title: 'Unexpected Error',
      status: response.status,
      detail: response.statusText,
    }));
    throw new ApiError(problem);
  }

  if (response.status === 204) return undefined as TResponse;
  return response.json() as Promise<TResponse>;
}
```

---

## 3. Padrão de Query Key Factory & Hooks (`TanStack Query v5`)

Em cada módulo (`src/features/<feature>/api/`), padronize as chaves de cache e mutações:

```typescript
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiFetch } from '@/lib/api/client';

export const resourceKeys = {
  all: ['resources'] as const,
  lists: () => [...resourceKeys.all, 'list'] as const,
  list: (filters: { page: number; pageSize: number }) => [...resourceKeys.lists(), filters] as const,
  details: () => [...resourceKeys.all, 'detail'] as const,
  detail: (id: string) => [...resourceKeys.details(), id] as const,
};

export function useResources(page = 1, pageSize = 20) {
  return useQuery({
    queryKey: resourceKeys.list({ page, pageSize }),
    queryFn: () => apiFetch<PagedResponse<ResourceDto>>(`/api/v1/resources?page=${page}&pageSize=${pageSize}`),
    staleTime: 1000 * 60, // 1 minuto
  });
}

export function useCreateResource() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateResourceInput) =>
      apiFetch<ResourceDto>('/api/v1/resources', {
        method: 'POST',
        body: JSON.stringify(payload),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: resourceKeys.lists() });
    },
  });
}
```

---

## 4. Estratégia Obrigatória de Testes Frontend (`Vitest + Testing Library + MSW`)

Nenhum componente ou hook de API está pronto sem testes automatizados cobrindo:
1. **Estado de carregamento (`loading`) e renderização com sucesso (`200 OK`)** interceptado via **MSW (`http.get`, `http.post`)**.
2. **Validação de formulário (`Zod`)** impedindo submissão inválida antes de chamar a API.
3. **Tratamento resiliente de erro da API (`400/422 ProblemDetails` ou `500`)**, exibindo feedback claro ao usuário.

