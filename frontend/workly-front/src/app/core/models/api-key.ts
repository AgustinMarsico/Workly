export interface ApiKeySummary {
  id: string;
  name: string;
  createdAt: string;
  revokedAt?: string;
  isActive: boolean;
}

export interface ApiKeyCreated {
  id: string;
  name: string;
  createdAt: string;
  apiKey: string;
}
