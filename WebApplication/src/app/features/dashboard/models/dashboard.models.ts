import { CandidateStatus } from '../../candidates/models/candidate.models';
import { VehicleStatus } from '../../vehicles/vehicle.models';

export interface MonthlyOverview {
  carsInStock: number;
  carsInRepair: number;
  soldThisMonth: number;
  profitThisMonth: number;
}
export interface FinancialOverview {
  availableCash: number;
  capitalInvested: number;
  upcomingProjectedCosts: number;
  netWorthAtCost: number;
  currency: string;
}
export interface PipelineSummary {
  candidates: number;
  transporting: number;
  repairing: number;
  readyForSale: number;
  listed: number;
}
export interface CandidateSummary {
  id: number;
  make: string;
  model: string;
  year: number | null;
  mileage: number | null;
  estimatedTotalCost: number | null;
  expectedSellingPrice: number | null;
  expectedProfit: number | null;
  expectedRoi: number | null;
  createdAt: string;
  status: CandidateStatus;
}
export interface ActiveVehicleSummary {
  id: number;
  make: string;
  model: string;
  year: number;
  status: VehicleStatus;
  totalInvested: number;
  remainingProjectedCosts: number;
  projectedFinalCost: number;
  expectedSellingPrice: number | null;
  listingPrice: number | null;
  projectedProfit: number | null;
}
export interface MonthlyFinancialData {
  month: string;
  capitalInvested: number;
  salesRevenue: number;
  realizedProfit: number;
  salesCount: number;
}
export interface DashboardSummary {
  attentionCount: number;
  attentionItems: DashboardAttentionItem[];
  asOf: string;
  agedVehicles: AgedVehicle[];
  monthly: MonthlyOverview;
  financial: FinancialOverview;
  pipeline: PipelineSummary;
  candidates: CandidateSummary[];
  activeVehicles: ActiveVehicleSummary[];
  monthlyFinancials: MonthlyFinancialData[];
}

export interface AgedVehicle {
  id: number;
  make: string;
  model: string;
  status: VehicleStatus;
  daysInStock: number;
  daysListed: number | null;
  severity: 'Warning' | 'Critical';
  totalInvested: number;
  listingPrice: number | null;
}

export enum DashboardAttentionSeverity {
  Critical = 0,
  Warning = 1,
  Info = 2,
}
export enum DashboardAttentionType {
  ReservationExpired = 0,
  ReservationExpiringSoon = 1,
  CandidateWaiting = 2,
  RepairTakingTooLong = 3,
  TransportTakingTooLong = 4,
  ReadyNotListed = 5,
  ReservedWithoutReservation = 6,
  ListedWithoutListing = 7,
}
export interface DashboardAttentionItem {
  type: DashboardAttentionType;
  severity: DashboardAttentionSeverity;
  vehicleId: number | null;
  candidateId: number | null;
  make: string;
  model: string;
  referenceDate: string;
  days: number;
  depositAmount: number | null;
}
