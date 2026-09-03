import { parseActorInput, type ActorInput } from './input.js';

export interface ActorConfig {
  input: ActorInput;
  apifyToken: string;
}

export function loadConfig(input: unknown, env: NodeJS.ProcessEnv): ActorConfig {
  const parsedInput = parseActorInput(input);
  const apifyToken = env.APIFY_TOKEN?.trim();

  if (!apifyToken) {
    throw new Error('APIFY_TOKEN is required to build Actor runtime configuration.');
  }

  return { input: parsedInput, apifyToken };
}
