import { GameRules } from '../api/models';

/** `{{min}}`/`{{max}}` for texts that name a card's worth; `?` while the backend's rules are unknown. */
export function badPointsParams(rules: GameRules | null): { min: number | string; max: number | string } {
  return { min: rules?.badPoints.min ?? '?', max: rules?.badPoints.max ?? '?' };
}
