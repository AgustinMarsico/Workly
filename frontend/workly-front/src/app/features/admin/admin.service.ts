import { Injectable } from '@angular/core';
import { ApiClient, AuditApiClient, IdentityApiClient } from '../../core/services/api-client.service';
import { Company } from '../../core/models/company';
import { Area } from '../../core/models/area';
import { User } from '../../core/models/user';
import { AuditRecord, AuditRecordsQuery } from '../../core/models/audit-record';

export interface CreateCompanyRequest {
  name: string;
  address: {
    street: string;
    number: number;
    city: string;
    state: string;
    zipCode: string;
    country: string;
  };
  website: string;
  base64Logo: string;
}

export interface CreateAreaRequest {
  name: string;
  description: string;
}

export interface CreateUserRequest {
  email: string;
  password?: string;
  role: 'Admin' | 'Member';
  companyId?: string;
}

export interface CreateInvitationRequest {
  userEmail: string;
  companyId: string;
  areaId: string;
}

@Injectable({ providedIn: 'root' })
export class AdminService {
  constructor(
    private readonly api: ApiClient,
    private readonly identityApi: IdentityApiClient,
    private readonly auditApi: AuditApiClient,
  ) {}

  listCompanies() {
    return this.api.get<Company[]>('/api/companies');
  }

  createCompany(req: CreateCompanyRequest) {
    return this.api.post<Company>('/api/companies', {
      name: req.name,
      address: req.address,
      website: req.website,
      base64Logo: req.base64Logo,
    });
  }

  listUsers() {
    return this.identityApi.get<User[]>('/api/users');
  }

  createArea(companyId: string, req: CreateAreaRequest) {
    return this.api.post<Area>(`/api/companies/${companyId}/areas`, {
      Name: req.name,
      Description: req.description,
    });
  }

  updateArea(companyId: string, areaId: string, req: CreateAreaRequest) {
    return this.api.put<Area>(`/api/companies/${companyId}/areas/${areaId}`, {
      Name: req.name,
      Description: req.description,
    });
  }

  deleteArea(companyId: string, areaId: string) {
    return this.api.delete<void>(`/api/companies/${companyId}/areas/${areaId}`);
  }

  createUser(req: CreateUserRequest) {
    return this.identityApi.post<void>('/api/users', {
      email: req.email,
      ...(req.password && { password: req.password }),
      role: req.role,
      companyId: req.companyId ?? null,
    });
  }

  createInvitation(req: CreateInvitationRequest) {
    return this.identityApi.post<void>('/api/invitations', {
      userEmail: req.userEmail,
      companyId: req.companyId,
      areaId: req.areaId,
    });
  }

  listAuditRecords(query: AuditRecordsQuery) {
    return this.auditApi.get<{
      items: AuditRecord[];
      page: number;
      pageSize: number;
      totalCount: number;
    }>('/api/audit-records', query);
  }
}
