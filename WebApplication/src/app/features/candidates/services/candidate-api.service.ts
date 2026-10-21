import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_BASE_URL } from '../../../configuration/api.config';
import { PurchaseCandidateRequest, PurchaseCandidateResult } from '../models/purchase.models';
import {
  CandidateAcquisitionType,
  CandidateAuctionDetails,
  CandidateDetails,
  CandidateEstimate,
  CandidateListResult,
  CreateCandidateRequest,
  CreateEstimateRequest,
} from '../models/candidate.models';

@Injectable({ providedIn: 'root' })
export class CandidateApiService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_BASE_URL).replace(/\/$/, '')}/candidates`;

  list(
    textFilter: string,
    offset: number,
    limit: number,
    acquisitionType: CandidateAcquisitionType | null = null,
  ) {
    return this.http.get<CandidateListResult>(this.url, {
      params: {
        TextFilter: textFilter,
        Offset: offset,
        Limit: limit,
        ...(acquisitionType === null ? {} : { AcquisitionType: acquisitionType }),
      },
    });
  }
  details(id: number) {
    return this.http.get<CandidateDetails>(`${this.url}/${id}`);
  }
  create(request: CreateCandidateRequest) {
    return this.http.post<CandidateDetails>(this.url, request);
  }
  updateAuctionDetails(id: number, request: CandidateAuctionDetails) {
    return this.http.put<CandidateDetails>(`${this.url}/${id}/auction-details`, request);
  }
  createEstimate(request: CreateEstimateRequest) {
    return this.http.post<CandidateEstimate>(`${this.url}/estimates`, request);
  }
  approve(id: number) {
    return this.http.post<CandidateDetails>(`${this.url}/${id}/approve`, null);
  }
  purchase(id: number, request: PurchaseCandidateRequest) {
    return this.http.post<PurchaseCandidateResult>(`${this.url}/${id}/purchase`, request);
  }
  reject(id: number, reason: string | null) {
    return this.http.post<CandidateDetails>(`${this.url}/${id}/reject`, { reason });
  }
}
