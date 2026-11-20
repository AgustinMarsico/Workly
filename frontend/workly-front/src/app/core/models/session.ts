import { UserRole } from './user-role';

export interface Session {
  token: string;
  role: UserRole;
  email: string;
  companyId?: string;
  companyName?: string;
}
