---
name: frontend-api-architecture
description: >-
  Use this skill whenever architecting, creating, or modifying the Web Frontend.
  Implements industry best practices for REST API consumption:
  Next.js (App Router) / React + strict TypeScript, TanStack Query v5, automated OpenAPI
  synchronization, React Hook Form + Zod, Supabase Auth, and automated testing (Vitest, Testing Library, MSW, Playwright).
---

# Modern Frontend Architecture for API Consumption (`frontend-api-architecture`)

This skill establishes the most robust, documented, and testable pattern for Frontend applications consuming a C# Minimal REST API with shared authentication via **Supabase Auth**.

---

## 1. Architectural Principles (Clean Code, KISS & Mobile-Ready)

1. **Strict Separation Between Server State and UI State**:
   - **Server State (C# REST API)**: Managed **100% via TanStack Query v5**. Never store API responses in manual `useState` or global Redux/Zustand stores.
   - **Local UI State**: Simple `useState` / `useReducer` (or URL Search Params for filters, pagination, and sorting — enabling easy link sharing and SSR).
2. **End-to-End Type Safety Without Redundancy (`OpenAPI -> TypeScript`)**:
   - Generate types directly from the C# Minimal API OpenAPI spec:
     `npx openapi-typescript http://localhost:5000/openapi/v1.json -o src/lib/api/schema.d.ts`
3. **Reusable Code for Future Mobile Apps**:
   - Keep the `features/<domain>/api/` and `features/<domain>/schemas/` layers free of DOM-specific dependencies (`window`, `document`), enabling seamless reuse in future **React Native / Expo** apps.

---

## 2. HTTP Client Pattern + Supabase Auth + ProblemDetails

Implement a clean, typed wrapper (`src/lib/api/client.ts`) that automatically injects the Supabase JWT and converts RFC 7807 (`ProblemDetails`) errors returned by the C# Minimal API:

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

## 3. Query Key Factory & Hooks Pattern (`TanStack Query v5`)

In each feature module (`src/features/<feature>/api/`), standardize cache keys and mutations:

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
    staleTime: 1000 * 60, // 1 minute
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

## 4. Mandatory Frontend Testing Strategy (`Vitest + Testing Library + MSW`)

No component or API hook is complete without automated tests covering:
1. **Loading state (`isLoading`) and successful rendering (`200 OK`)** intercepted via **MSW (`http.get`, `http.post`)**.
2. **Form validation (`Zod`)** preventing invalid submissions before calling the API.
3. **Resilient API error handling (`400/422 ProblemDetails` or `500`)**, displaying clear feedback to the user.

---

## 5. PageSpeed & WCAG Accessibility Guardrails (Lighthouse 100/100)

Every frontend component, page, and feature must be designed to pass Google PageSpeed Insights with 95+ (ideally 100/100) across all metrics:

1. **WCAG 2.1 AA Color Contrast Ratio (Minimum 4.5:1 for Normal Text)**:
   - Never use pale Tailwind shades like `text-slate-400` or `text-gray-400` directly on white or light gray backgrounds (contrast drops below 3:1).
   - Use `text-slate-600 dark:text-slate-400` for subtitles/badges on light backgrounds.
   - Use `text-emerald-700 dark:text-emerald-400` instead of `text-emerald-600` for status indicators to guarantee > 5.0:1 contrast.
2. **WCAG 2.5.3 (Label in Name Compliance)**:
   - When adding `aria-label` to buttons or links that contain visible text, the accessible label **MUST start with or contain the exact visible text**.
   - Example: Button displaying `"New Alert"` must use `aria-label="New Alert - Create new alert"` (never an unrelated phrase like `aria-label="Create item"` which confuses speech recognition and screen readers).
3. **Bundle Splitting & Critical Path Optimization (`next/dynamic`)**:
   - Interactive modals, chat drawers, and heavy third-party widgets not visible on initial render must be loaded asynchronously via `next/dynamic` with `{ ssr: false }`.
   - Keep the First Load JS shared by all pages strictly below 100 KiB.
4. **Zero Layout Shift (CLS = 0.000)**:
   - Always define explicit `width` and `height` or aspect-ratio boxes for images and asynchronous skeleton placeholders.


---

## 6. UX, Cognitive Heuristics & Interface Design Guidelines

To guarantee high user retention, accessibility, and zero cognitive friction, all frontend development must adhere to proven usability heuristics and human-computer interaction principles:

### 6.1. The 10 Usability Heuristics (Jakob Nielsen) in Frontend Engineering
1. **Visibility of System Status**:
   - Provide instantaneous visual feedback upon any user interaction. Never leave the user guessing if an action was received.
   - Use skeleton loaders matching the exact structural layout instead of generic blocking full-page spinners.
   - Employ optimistic updates for low-risk actions (e.g., removing a watchlist item) with automatic rollback on server error.
2. **Match Between System and Real World**:
   - Use language, symbols, and formatting standard to the domain (e.g., ISO currency codes `USD`, `BRL`, recognizable delta badges with upward green / downward red arrows).
3. **User Control and Freedom**:
   - Every overlay, modal, and drawer must offer clear exit routes: explicit close button (`X`), clicking outside on the backdrop, or pressing the `Escape` key.
   - Destructive actions must provide immediate undo capabilities (e.g., 5-second toast undo) or an explicit confirmation step.
4. **Consistency and Standards (Design Tokens)**:
   - Reuse standard primitives from the design system (`@/components/ui/*`). Button heights, corner radii (`rounded-lg`), font scales, and focus rings must remain strictly identical across all feature slices.
5. **Error Prevention**:
   - Validate form fields inline with `react-hook-form` and `zod` before submitting. Disable or add loading indicators to submit buttons to prevent double-clicks.
6. **Recognition Rather Than Recall**:
   - Keep options, actions, and current context visible. Minimize the user's cognitive load by displaying pre-filled defaults, search suggestions, and persistent summaries.
7. **Flexibility and Efficiency of Use**:
   - Support both novice and power users: provide full keyboard navigation, logical `Tab` order, and clear shortcuts.
8. **Aesthetic and Minimalist Design**:
   - Avoid visual clutter or generic "AI template" bloat. Maintain high signal-to-noise ratio: prioritize the primary task (e.g., viewing live rates and setting alerts) and relegate secondary information to drawers or tabs.
9. **Help Users Recognize, Diagnose, and Recover from Errors**:
   - Never show raw technical exceptions or HTTP status codes (`HTTP 500`, `NpgsqlException`).
   - Translate RFC 7807 `ProblemDetails` into friendly, actionable language (e.g., *"Too many requests in a short period. Please wait 10 seconds before refreshing."*) with a prominent 1-click **Retry** button.
10. **Help and Documentation**:
    - Provide contextual guidance (tooltips, inline helper text, and the AI Copilot drawer) exactly where the user might encounter ambiguity.

### 6.2. Cognitive UX Laws
* **Jakob's Law**: Users spend most of their time on other sites. Follow established web layout conventions (top navigation bar, right-hand slide-over drawer, standard table headers).
* **Hick's Law**: Time to make a decision increases with the number of choices. Keep forms concise and use progressive disclosure (collapsible accordion for advanced parameters).
* **Fitts's Law**: Touch and click targets must be comfortably reachable. All interactive elements on mobile viewports must meet the minimum size of **44x44px** (Apple HIG) or **48x48px** (Google Material). Place primary actions within comfortable thumb reach.
* **Miller's Law (Chunking)**: Group related data into logical cards and sections (Rates, Active Alerts, System Health) rather than monolithic data tables.

### 6.3. The 4 Mandatory Component Data States
Every asynchronous component consuming backend APIs must explicitly implement and test 4 states:
1. **Loading State**: Contextual skeleton placeholder with subtle pulse (`animate-pulse`).
2. **Empty State**: Friendly illustration or icon, explanatory text (*"You don't have any active currency alerts yet"*), and a clear primary Call-to-Action button (*"Create your first alert"*).
3. **Error State**: Empathetic message explaining what went wrong and a 1-click **"Try again"** retry handler.
4. **Success/Data State**: The populated UI view with smooth transitions.

### 6.4. Deep Accessibility (a11y) & Inclusivity
* **Live Regions (`aria-live="polite"`)**: High-frequency streaming data or real-time ticker updates must use `aria-live="polite"` so screen readers announce changes without interrupting active navigation.
* **Focus Trap & Keyboard Trap**: When drawers or dialogs are open, keyboard focus (`Tab`) must be constrained inside the active modal. Pressing `Escape` must close the overlay and return focus to the trigger button.
* **Motion Sensitivity (`prefers-reduced-motion`)**: Respect vestibular needs by disabling heavy CSS transforms or infinite marquee animations when the user prefers reduced motion (`motion-reduce:transition-none`).
