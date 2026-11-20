export interface AuditRecord {
  id: string;
  eventId: string;
  occurredAt: string;
  actionType: string;
  userId?: string | null;
  userEmail?: string | null;
  companyId?: string | null;
  entityType?: string | null;
  entityId?: string | null;
  detailsJson?: string | null;
  oldValuesJson?: string | null;
  newValuesJson?: string | null;
  correlationId?: string | null;
  createdAt: string;
}

export interface AuditRecordsQuery {
  from?: string | null;
  to?: string | null;
  userId?: string | null;
  actionType?: string | null;
  companyId?: string | null;
  page?: number;
  pageSize?: number;
}
