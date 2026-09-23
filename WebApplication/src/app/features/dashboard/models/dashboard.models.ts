import { CandidateStatus, VehicleStatus } from '../../../shared/models/ui.models';

export interface MonthlyOverview {
  carsInStock: number;
  carsInRepair: number;
  soldThisMonth: number;
  profitThisMonth: number;
  stockChange: number;
  overdueRepairs: number;
  salesGrowthPercent: number;
  profitGrowthPercent: number;
}
export interface FinancialOverview {
  availableCash: number;
  capitalInvested: number;
  committedCosts: number;
  inventoryValue: number;
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
  vehicle: string;
  year: number;
  mileage: number;
  specs: string;
  vin: string;
  estimatedTotalCost: number;
  expectedSale: number;
  expectedProfit: number;
  expectedRoi: number;
  status: CandidateStatus;
}
export interface ActiveVehicleSummary {
  id: number;
  vehicle: string;
  vin: string;
  status: VehicleStatus;
  spentSoFar: number;
  projectedTotal: number;
  isFinalCost: boolean;
  expectedSale: number;
  projectedProfit: number;
  spendingPercent: number;
}
export interface MonthlyFinancialData {
  month: string;
  capitalInvested: number;
  salesRevenue: number;
  netProfit: number;
}
export interface DashboardSummary {
  periodLabel: string;
  asOf: string;
  monthly: MonthlyOverview;
  financial: FinancialOverview;
  pipeline: PipelineSummary;
  candidates: CandidateSummary[];
  activeVehicles: ActiveVehicleSummary[];
  monthlyFinancials: MonthlyFinancialData[];
}
