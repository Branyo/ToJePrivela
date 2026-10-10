import { Limit } from '../api/models';
import { Message } from '../i18n/language';

/** What is wrong with a new password and its repetition, shared by creating a login and changing its password. */
export function newPasswordProblem(limit: Limit, password: string, repeat: string): Message | null {
  if (password.length < limit.min || password.length > limit.max) {
    return { key: 'signIn.errors.passwordLength', params: limit };
  }

  if (password !== repeat) {
    return { key: 'signIn.errors.passwordsDiffer' };
  }

  return null;
}
