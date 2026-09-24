// Numeric values match DealerManager.Domain.Enums and the API JSON contract.
export enum CandidateStatus {
  UnderReview = 0,
  Approved = 1,
  Rejected = 2,
  Purchased = 3,
}
export enum CostCategory {
  Purchase = 0,
  Transport = 1,
  Repair = 2,
  Parts = 3,
  Labor = 4,
  Cleaning = 5,
  Detailing = 6,
  Documents = 7,
  Registration = 8,
  AuctionFee = 9,
  Taxes = 10,
  Fuel = 11,
  Advertising = 12,
  Other = 13,
}
export const CANDIDATE_STATUS_LABELS: Record<CandidateStatus, string> = {
  [CandidateStatus.UnderReview]: 'Under review',
  [CandidateStatus.Approved]: 'Approved',
  [CandidateStatus.Rejected]: 'Rejected',
  [CandidateStatus.Purchased]: 'Purchased',
};
export const COST_CATEGORIES = [
  'Purchase',
  'Transport',
  'Repair',
  'Parts',
  'Labor',
  'Cleaning',
  'Detailing',
  'Documents',
  'Registration',
  'Auction fee',
  'Taxes',
  'Fuel',
  'Advertising',
  'Other',
].map((label, value) => ({ label, value: value as CostCategory }));

export interface CreateCandidateRequest {
  make: string;
  model: string;
  year: number | null;
  mileage: number | null;
  vin: string | null;
  source: string | null;
  location: string | null;
  expectedSellingPrice: number;
  notes: string | null;
}
export interface CandidateListItem {
  id: number;
  make: string;
  model: string;
  year: number | null;
  mileage: number | null;
  status: CandidateStatus;
  expectedSellingPrice: number;
  createdAt: string;
  estimatedTotalCost: number | null;
  expectedProfit: number | null;
  expectedRoi: number | null;
}
export interface CandidateListResult {
  items: CandidateListItem[];
  totalCount: number;
}
export interface CandidateDetails extends CandidateListItem {
  vin: string | null;
  source: string | null;
  location: string | null;
  notes: string | null;
  rejectedAt: string | null;
  purchasedAt: string | null;
  estimateHistory: CandidateEstimate[];
  latestEstimate: CandidateEstimate | null;
}
export interface CreateEstimateItemRequest {
  category: CostCategory;
  description: string;
  estimatedAmount: number;
}
export interface CreateEstimateRequest {
  candidateId: number;
  expectedSellingPrice: number;
  notes: string | null;
  items: CreateEstimateItemRequest[];
}
export interface CandidateEstimate {
  id: number;
  candidateId: number;
  version: number;
  expectedSellingPrice: number;
  notes: string | null;
  createdAt: string;
  isDecisionSnapshot: boolean;
  items: (CreateEstimateItemRequest & { id: number })[];
  financialAnalysis: { estimatedTotalCost: number; expectedProfit: number; expectedRoi: number };
}
