export interface VehiclePhoto {
  id: number;
  vehicleId: number;
  type: number;
  originalFileName: string;
  contentType: string;
  sizeBytes: number;
  caption: string | null;
  isPrimary: boolean;
  sortOrder: number;
  uploadedAt: string;
  uploadedByUserId: number | null;
}
export interface VehicleDocument {
  id: number;
  vehicleId: number;
  type: number;
  title: string;
  originalFileName: string;
  contentType: string;
  sizeBytes: number;
  documentDate: string | null;
  notes: string | null;
  uploadedAt: string;
  uploadedByUserId: number | null;
}
export interface DocumentMetadata {
  type: number;
  title: string;
  documentDate: string | null;
  notes: string | null;
}
export const PHOTO_TYPES = [0, 1, 2, 3, 4, 5, 6];
export const DOCUMENT_TYPES = [0, 1, 2, 3, 4, 5, 6, 7];
