import { VehicleStatus } from './vehicle.models';
export interface VehicleReservation {
  reservationId: number; vehicleId: number; customerName: string;
  customerPhone: string | null; customerEmail: string | null;
  depositAmount: number; reservedAt: string; expiresAt: string | null;
  status: number; vehicleStatus: VehicleStatus; notes: string | null;
  cancellationNotes: string | null; createdAt: string; completedAt: string | null; cancelledAt: string | null;
  capitalAccountId: number; depositTransactionId: number; refundTransactionId: number | null;
}
export interface ReserveVehicleRequest {
  customerName: string; customerPhone: string | null; customerEmail: string | null;
  depositAmount: number; capitalAccountId: number; expiresAt: string | null; notes: string | null;
}
