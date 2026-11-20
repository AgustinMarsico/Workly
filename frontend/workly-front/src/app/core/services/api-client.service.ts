import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { APP_ENV } from '../config/app-env';

export abstract class BaseApiClient {
  private readonly http = inject(HttpClient);
  protected abstract readonly baseUrl: string;

  get<T>(path: string, query?: object) {
    return this.http.get<T>(this.baseUrl + path, this.requestOptions(query));
  }

  post<T>(path: string, body?: unknown, query?: object) {
    return this.http.post<T>(this.baseUrl + path, body ?? {}, this.requestOptions(query));
  }

  put<T>(path: string, body?: unknown, query?: object) {
    return this.http.put<T>(this.baseUrl + path, body ?? {}, this.requestOptions(query));
  }

  delete<T>(path: string, query?: object) {
    return this.http.delete<T>(this.baseUrl + path, this.requestOptions(query));
  }

  postFormData<T>(path: string, formData: FormData) {
    return this.http.post<T>(this.baseUrl + path, formData, this.requestOptions());
  }

  private requestOptions(query?: object) {
    return {
      params: this.toParams(query),
    };
  }

  private toParams(query?: object): HttpParams | undefined {
    if (!query) return undefined;

    let params = new HttpParams();
    for (const [key, value] of Object.entries(query as Record<string, unknown>)) {
      if (value === undefined || value === null || value === '') continue;

      if (Array.isArray(value)) {
        for (const item of value) {
          params = params.append(key, String(item));
        }
        continue;
      }

      params = params.set(key, String(value));
    }

    return params;
  }
}

@Injectable({ providedIn: 'root' })
export class ApiClient extends BaseApiClient {
  protected readonly baseUrl = APP_ENV.apiBaseUrl;
}

@Injectable({ providedIn: 'root' })
export class IdentityApiClient extends BaseApiClient {
  protected readonly baseUrl = APP_ENV.identityApiBaseUrl;
}

@Injectable({ providedIn: 'root' })
export class RoomsApiClient extends BaseApiClient {
  protected readonly baseUrl = APP_ENV.roomsApiBaseUrl;
}

@Injectable({ providedIn: 'root' })
export class AuditApiClient extends BaseApiClient {
  protected readonly baseUrl = APP_ENV.auditApiBaseUrl;
}
