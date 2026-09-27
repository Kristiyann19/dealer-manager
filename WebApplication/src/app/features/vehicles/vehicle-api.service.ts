import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_BASE_URL } from '../../configuration/api.config';
export interface VehicleDetails {
  id: number;
  make: string;
  model: string;
  year: number;
  status: number;
  sourceCandidateId: number | null;
  purchaseDate: string;
  actualPurchasePrice: number | null;
}
@Injectable({ providedIn: 'root' })
export class VehicleApiService {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_BASE_URL).replace(/\/$/, '');
  details(id: number) {
    return this.http.get<VehicleDetails>(`${this.base}/vehicles/${id}`);
  }
}
