import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_BASE_URL } from '../../../configuration/api.config';
import {
  AddCapitalRequest,
  CapitalAccount,
  CapitalAccountDetails,
  CreateCapitalAccountRequest,
  FinancialTransaction,
} from '../models/finance.models';

@Injectable({ providedIn: 'root' })
export class CapitalAccountApiService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_BASE_URL).replace(/\/$/, '')}/capital-accounts`;
  getCapitalAccounts() {
    return this.http.get<CapitalAccount[]>(this.url);
  }
  getCapitalAccountDetails(id: number) {
    return this.http.get<CapitalAccountDetails>(`${this.url}/${id}`);
  }
  createCapitalAccount(request: CreateCapitalAccountRequest) {
    return this.http.post<CapitalAccount>(this.url, request);
  }
  addCapital(id: number, request: AddCapitalRequest) {
    return this.http.post<FinancialTransaction>(`${this.url}/${id}/contributions`, request);
  }
  getTransactions(id: number) {
    return this.http.get<FinancialTransaction[]>(`${this.url}/${id}/transactions`);
  }
}
