import { useMutation } from '@tanstack/react-query';
import { apiFetch } from '@/lib/api/client';
import type { AskAiAdvisorRequest, AiAdvisorResponse } from './types';

export function useAiAdvisor() {
  return useMutation<AiAdvisorResponse, Error, AskAiAdvisorRequest>({
    mutationFn: (payload) =>
      apiFetch<AiAdvisorResponse>('/api/v1/ai/chat', {
        method: 'POST',
        body: JSON.stringify(payload),
      }),
  });
}

