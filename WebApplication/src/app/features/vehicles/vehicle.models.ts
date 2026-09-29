import { CandidateEstimate, CostCategory } from '../candidates/models/candidate.models';

export interface VehicleFinancialSummary {
  actualPurchasePrice: number;
  actualExpenses: number;
  totalInvested: number;
  remainingProjectedCosts: number;
  projectedFinalCost: number;
  expectedSellingPrice: number | null;
  projectedProfit: number | null;
  projectedROI: number | null;
}
export interface OriginalForecast {
  estimateId: number;
  version: number;
  items: CandidateEstimate['items'];
  originalEstimatedTotal: number;
  originalExpectedSellingPrice: number;
  originalExpectedProfit: number;
  originalExpectedROI: number;
}
export interface VehicleDetails extends VehicleFinancialSummary {
  id: number;
  make: string;
  model: string;
  year: number;
  mileage: number | null;
  vin: string | null;
  status: number;
  sourceCandidateId: number | null;
  purchaseDate: string;
  originalForecast: OriginalForecast | null;
}
export interface VehicleCostPlanItem {
  id: number;
  category: CostCategory;
  description: string;
  currentEstimatedAmount: number | null;
  committedAmount: number | null;
  actualPaid: number;
  remainingProjected: number;
  isCancelled: boolean;
}
export interface UpdateVehicleCostPlanItemRequest {
  currentEstimatedAmount: number | null;
  committedAmount: number | null;
  description: string | null;
  isCancelled: boolean;
}
export interface VehicleExpense {
  id: number;
  category: CostCategory;
  description: string;
  amount: number;
  supplier: string | null;
  documentNumber: string | null;
  paidAt: string;
  costPlanItemId: number | null;
  costPlanDescription: string | null;
  financialTransactionId: number;
  capitalAccountId: number;
  capitalAccountName: string;
  currency: string;
}
export interface AddVehicleExpenseRequest {
  costPlanItemId: null;
  category: CostCategory;
  description: string;
  amount: number;
  supplier: string | null;
  documentNumber: string | null;
  paidAt?: string;
}

export interface VehiclePaymentAccount {
  capitalAccountId: number;
  currency: string;
  currentBalance: number;
}
export interface VehiclePaymentPreview extends VehiclePaymentAccount {
  category: CostCategory;
  amount: number;
}
