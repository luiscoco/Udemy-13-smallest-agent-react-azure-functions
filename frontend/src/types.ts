// Browser-only public contracts. No imports from Functions, configuration or SDK.
export interface AgentResult {
  result: string; num_turns: number; duration_ms: number; total_cost_usd: number | null; is_error: boolean;
  input_tokens: number; output_tokens: number; stop_reason: string;
}
export type MessageType = 'SystemMessage' | 'TextDelta' | 'ResultMessage';
export interface AgentMessage { type: MessageType; summary: string; data: AgentResult | Record<string, unknown>; }
export interface TimelineMessage extends AgentMessage { elapsed: number; }
export interface Health { status: 'ok'; api_key_configured: boolean; }
export interface AgentClient {
  health(signal?: AbortSignal): Promise<Health>;
  query(prompt: string, signal?: AbortSignal): Promise<AgentResult>;
  stream(prompt: string, onMessage: (message: AgentMessage) => void, signal?: AbortSignal): Promise<void>;
}
export function isAgentResult(value: unknown): value is AgentResult {
  if (!value || typeof value !== 'object') return false;
  const data = value as Record<string, unknown>;
  return typeof data.result === 'string' && Number.isInteger(data.num_turns) && typeof data.duration_ms === 'number' && Number.isFinite(data.duration_ms)
    && (data.total_cost_usd === null || typeof data.total_cost_usd === 'number') && typeof data.is_error === 'boolean'
    && typeof data.input_tokens === 'number' && typeof data.output_tokens === 'number' && typeof data.stop_reason === 'string';
}
