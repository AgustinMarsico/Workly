import { Injectable } from '@angular/core';
import { IdentityApiClient } from '../../core/services/api-client.service';

export interface InviteInfo {
  id: string;
  email: string;
  companyName: string;
  invitationStatus: string;
  expiresAt: string;
}

export interface AcceptInviteResponse {
  email: string;
}

@Injectable({ providedIn: 'root' })
export class AuthInviteService {
  constructor(private readonly api: IdentityApiClient) {}

  getInvite(invitationId: string) {
    return this.api.get<InviteInfo>(`/api/invitations/${encodeURIComponent(invitationId)}`);
  }

  acceptInvite(invitationId: string, token: string, password: string) {
    return this.api.post<AcceptInviteResponse>(`/api/invitations/accept`, {
      invitationId: invitationId,
      token,
      password,
    });
  }
}
