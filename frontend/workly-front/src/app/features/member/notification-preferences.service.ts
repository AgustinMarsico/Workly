import { Injectable } from '@angular/core';
import { IdentityApiClient } from '../../core/services/api-client.service';

export interface NotificationPreferences {
  emailNotificationsEnabled: boolean;
  inAppNotificationsEnabled: boolean;
}

@Injectable({ providedIn: 'root' })
export class NotificationPreferencesService {
  constructor(private readonly api: IdentityApiClient) {}

  get() {
    return this.api.get<NotificationPreferences>('/api/users/me/notification-preferences');
  }

  updateEmailNotifications(enabled: boolean) {
    return this.api.put<NotificationPreferences>('/api/users/me/notification-preferences', {
      emailNotificationsEnabled: enabled,
    });
  }
}
