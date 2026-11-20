export interface User {
  id: string;
  email: string;
  role: string;
  companyId?: string | null;
  areaId?: string | null;
  emailSubscribed?: boolean;
}
