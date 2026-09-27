export interface PurchaseCandidateRequest {
  capitalAccountId: number;
  actualPurchasePrice: number;
  purchaseDate?: string;
}
export interface PurchaseCandidateResult {
  candidateId: number;
  vehicleId: number;
  capitalAccountId: number;
  actualPurchasePrice: number;
  purchaseDate: string;
  previousBalance: number;
  remainingBalance: number;
}
