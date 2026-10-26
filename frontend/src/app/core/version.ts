import { version } from '../../../package.json';

/**
 * The game's version (Semantic Versioning), the one in the repository's `VERSION` file that `package.json` repeats;
 * the API reports the same at `GET /api/info`. Only the number is bundled, not the rest of `package.json`.
 */
export const APP_VERSION: string = version;
