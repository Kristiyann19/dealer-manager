import { VehicleDossier } from './vehicle-dossier.models';
import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_BASE_URL } from '../../configuration/api.config';
import {
  AddVehicleExpenseRequest,
  UpdateVehicleCostPlanItemRequest,
  VehicleCostPlanItem,
  VehicleDetails,
  VehicleExpense,
  VehicleFinancialSummary,
  VehiclePaymentAccount,
  VehiclePaymentPreview,
  VehicleListResult,
  VehicleStatus,
  VehicleStatusHistory,
  VehicleListing,
  ListVehicleRequest,
  SellVehicleRequest,
  VehicleSaleResult,
} from './vehicle.models';
export type { VehicleDetails } from './vehicle.models';
@Injectable({ providedIn: 'root' })
export class VehicleApiService {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_BASE_URL).replace(/\/$/, '');
  list(textFilter: string, status: VehicleStatus | null, offset: number, limit: number) {
    const params: Record<string, string | number> = {
      TextFilter: textFilter,
      Offset: offset,
      Limit: limit,
    };
    if (status !== null) params['Status'] = status;
    return this.http.get<VehicleListResult>(`${this.base}/vehicles`, { params });
  }
  changeStatus(id: number, status: VehicleStatus, notes: string | null) {
    return this.http.post<VehicleStatusHistory>(`${this.base}/vehicles/${id}/status`, {
      status,
      notes,
    });
  }
  statusHistory(id: number) {
    return this.http.get<VehicleStatusHistory[]>(`${this.base}/vehicles/${id}/status-history`);
  }
  listVehicle(id: number, request: ListVehicleRequest) {
    return this.http.post<VehicleListing>(`${this.base}/vehicles/${id}/listing`, request);
  }
  sellVehicle(id: number, request: SellVehicleRequest) {
    return this.http.post<VehicleSaleResult>(`${this.base}/vehicles/${id}/sale`, request);
  }
  currentListing(id: number) {
    return this.http.get<VehicleListing | null>(`${this.base}/vehicles/${id}/listing`);
  }
  updateDossier(id: number, request: VehicleDossier) {
    return this.http.put<VehicleDetails>(`${this.base}/vehicles/${id}/details`, request);
  }
  details(id: number) {
    return this.http.get<VehicleDetails>(`${this.base}/vehicles/${id}`);
  }
  financialSummary(id: number) {
    return this.http.get<VehicleFinancialSummary>(`${this.base}/vehicles/${id}/financial-summary`);
  }
  costPlan(id: number) {
    return this.http.get<VehicleCostPlanItem[]>(`${this.base}/vehicles/${id}/cost-plan`);
  }
  updateCostPlan(id: number, itemId: number, request: UpdateVehicleCostPlanItemRequest) {
    return this.http.put<VehicleCostPlanItem>(
      `${this.base}/vehicles/${id}/cost-plan/${itemId}`,
      request,
    );
  }
  paymentAccount(id: number) {
    return this.http.get<VehiclePaymentAccount>(`${this.base}/vehicles/${id}/payment-account`);
  }
  paymentPreview(id: number, itemId: number) {
    return this.http.get<VehiclePaymentPreview>(
      `${this.base}/vehicles/${id}/cost-plan/${itemId}/payment-preview`,
    );
  }
  confirmPayment(id: number, itemId: number, preview: VehiclePaymentPreview) {
    return this.http.post<VehicleExpense>(
      `${this.base}/vehicles/${id}/cost-plan/${itemId}/confirm-payment`,
      {
        expectedAmount: preview.amount,
        expectedCapitalAccountId: preview.capitalAccountId,
      },
    );
  }
  expenses(id: number) {
    return this.http.get<VehicleExpense[]>(`${this.base}/vehicles/${id}/expenses`);
  }
  addExpense(id: number, request: AddVehicleExpenseRequest) {
    return this.http.post<VehicleExpense>(`${this.base}/vehicles/${id}/expenses`, request);
  }
}
