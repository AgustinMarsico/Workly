import { Injectable } from '@angular/core';
import { APP_ENV } from '../config/app-env';
import { BaseApiClient } from './api-client.service';

@Injectable({ providedIn: 'root' })
export class ReservationApiClient extends BaseApiClient {
  protected readonly baseUrl = APP_ENV.reservationApiBaseUrl;
}
