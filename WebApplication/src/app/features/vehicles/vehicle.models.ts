import { VehicleDossier } from './vehicle-dossier.models';
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
  actualSalePrice: number | null;
  soldAt: string | null;
  realizedProfit: number | null;
  realizedROI: number | null;
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
export interface VehicleDetails extends VehicleFinancialSummary, Partial<VehicleDossier> {
  id: number;
  make: string;
  model: string;
  year: number;
  mileage: number | null;
  vin: string | null;
  status: VehicleStatus;
  sourceCandidateId: number | null;
  purchaseDate: string;
  originalForecast: OriginalForecast | null;
  currentListing: VehicleListing | null;
  listingPrice: number | null;
  listedAt: string | null;
  saleAccount: VehiclePaymentAccount | null;
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

export enum VehicleStatus {
  Purchased = 0,
  Transporting = 1,
  Arrived = 2,
  Inspecting = 3,
  Repairing = 4,
  Preparing = 5,
  ReadyForSale = 6,
  Listed = 7,
  Reserved = 8,
  Sold = 9,
}
export const VEHICLE_STATUSES = [
  'Purchased',
  'Transporting',
  'Arrived',
  'Inspecting',
  'Repairing',
  'Preparing',
  'ReadyForSale',
  'Listed',
  'Reserved',
  'Sold',
].map((key, value) => ({ value: value as VehicleStatus, label: 'vehicle.statuses.' + key }));
export interface VehicleStatusHistory {
  id: number;
  fromStatus: VehicleStatus;
  toStatus: VehicleStatus;
  changedAt: string;
  notes: string | null;
}
export type VehicleListItem = Omit<
  VehicleDetails,
  'sourceCandidateId' | 'originalForecast' | 'currentListing' | 'listedAt' | 'saleAccount'
> & { listingPrice: number | null };
export interface VehicleListing {
  vehicleId: number;
  listingId: number;
  listingPrice: number;
  listedAt: string;
  status: VehicleStatus;
}
export interface ListVehicleRequest {
  listingPrice: number;
  listedAt?: string;
}
export interface VehicleListResult {
  items: VehicleListItem[];
  totalCount: number;
}

export interface SellVehicleRequest {
  actualSalePrice: number;
  soldAt?: string;
  capitalAccountId?: number;
}
export interface VehicleSaleResult {
  vehicleId: number;
  saleId: number;
  actualSalePrice: number;
  soldAt: string;
  capitalAccountId: number;
  newCapitalBalance: number;
  totalInvested: number;
  realizedProfit: number;
  realizedROI: number;
  status: VehicleStatus;
}
