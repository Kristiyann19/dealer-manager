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
  asOf: string;
  monthly: MonthlyOverview;
  financial: FinancialOverview;
  pipeline: PipelineSummary;
  candidates: CandidateSummary[];
  activeVehicles: ActiveVehicleSummary[];
  monthlyFinancials: MonthlyFinancialData[];
}
