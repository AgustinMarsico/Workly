import { Injectable } from '@angular/core';
import { IdentityApiClient } from '../../core/services/api-client.service';
import { ApiKeyCreated } from '../../core/models/api-key';

@Injectable({ providedIn: 'root' })
export class ApiKeysService {
  constructor(private readonly api: IdentityApiClient) {}

  create(name: string) {
    return this.api.post<ApiKeyCreated>('/api/api-keys', { name });
  }

  revoke(id: string) {
    return this.api.delete<void>(`/api/api-keys/${encodeURIComponent(id)}`);
  }
}
