export enum TransactionDirection {
  In = 0,
  Out = 1,
}
export enum TransactionType {
  CapitalContribution = 0,
  VehiclePurchase = 1,
  VehicleExpense = 2,
  VehicleSale = 3,
  OwnerWithdrawal = 4,
  Adjustment = 5,
  Other = 6,
}
export interface CapitalAccount {
  id: number;
  name: string;
  currency: string;
  isActive: boolean;
  currentBalance: number;
}
export interface CapitalAccountDetails extends CapitalAccount {
  latestTransactions: FinancialTransaction[];
}
export interface FinancialTransaction {
  id: number;
  capitalAccountId: number;
  type: TransactionType;
  direction: TransactionDirection;
  amount: number;
  description: string;
  vehicleId: number | null;
  occurredAt: string;
  createdAt: string;
}
export interface CreateCapitalAccountRequest {
  name: string;
  currency: string;
}
export interface AddCapitalRequest {
  amount: number;
  description: string;
  occurredAt?: string;
}
export const TRANSACTION_TYPE_KEYS: Record<TransactionType, string> = {
  [TransactionType.CapitalContribution]: 'finance.types.contribution',
  [TransactionType.VehiclePurchase]: 'finance.types.purchase',
  [TransactionType.VehicleExpense]: 'finance.types.expense',
  [TransactionType.VehicleSale]: 'finance.types.sale',
  [TransactionType.OwnerWithdrawal]: 'finance.types.withdrawal',
  [TransactionType.Adjustment]: 'finance.types.adjustment',
  [TransactionType.Other]: 'finance.types.other',
};
