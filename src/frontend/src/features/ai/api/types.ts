export interface AiChatMessage {
  role: 'user' | 'assistant';
  content: string;
}

export interface AskAiAdvisorRequest {
  message: string;
  history?: AiChatMessage[];
}

export interface AiAdvisorResponse {
  reply: string;
  suggestedActions: string[];
  modelUsed: string;
  generatedAtUtc: string;
}

