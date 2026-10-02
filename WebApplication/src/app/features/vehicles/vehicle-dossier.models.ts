export enum FuelType {
  Petrol = 0,
  Diesel = 1,
  Lpg = 2,
  Cng = 3,
  Hybrid = 4,
  PlugInHybrid = 5,
  Electric = 6,
  Hydrogen = 7,
  Other = 99,
}
export enum Transmission {
  Manual = 0,
  Automatic = 1,
  SemiAutomatic = 2,
  Cvt = 3,
  Other = 99,
}
export enum DriveType {
  FrontWheelDrive = 0,
  RearWheelDrive = 1,
  AllWheelDrive = 2,
  FourWheelDrive = 3,
  Other = 99,
}
export enum BodyType {
  Sedan = 0,
  Hatchback = 1,
  Estate = 2,
  Suv = 3,
  Coupe = 4,
  Convertible = 5,
  Minivan = 6,
  Van = 7,
  Pickup = 8,
  Other = 99,
}
export enum EuroStandard {
  Euro1 = 0,
  Euro2 = 1,
  Euro3 = 2,
  Euro4 = 3,
  Euro5 = 4,
  Euro6 = 5,
  Euro6dTemp = 6,
  Euro6d = 7,
  Euro7 = 8,
  Other = 99,
}
function options(values: Record<string, string | number>, key: string) {
  return Object.entries(values)
    .filter(([, value]) => typeof value === 'number')
    .map(([name, value]) => ({
      value: value as number,
      label: `vehicle.dossier.options.${key}.${name}`,
    }));
}
export interface VehicleDossier {
  vin: string | null;
  registrationNumber: string | null;
  firstRegistration: string | null;
  mileage: number | null;
  fuelType: FuelType | null;
  transmission: Transmission | null;
  engineDisplacementCc: number | null;
  powerKw: number | null;
  powerHp: number | null;
  driveType: DriveType | null;
  euroStandard: EuroStandard | null;
  bodyType: BodyType | null;
  color: string | null;
  numberOfDoors: number | null;
  numberOfSeats: number | null;
  numberOfKeys: number | null;
  importedFrom: string | null;
  notes: string | null;
}
export interface DossierField {
  key: keyof VehicleDossier;
  label: string;
  type: 'text' | 'number' | 'date' | 'textarea' | 'select';
  min?: number;
  max?: number;
  maxLength?: number;
  options?: { value: number; label: string }[];
}
export const DOSSIER_GROUPS: { title: string; fields: DossierField[] }[] = [
  {
    title: 'vehicle.dossier.identity',
    fields: [
      { key: 'vin', label: 'vehicle.dossier.vin', type: 'text', maxLength: 32 },
      { key: 'mileage', label: 'vehicle.dossier.mileage', type: 'number', min: 0, max: 2147483647 },
    ],
  },
  {
    title: 'vehicle.dossier.technical',
    fields: [
      {
        key: 'fuelType',
        label: 'vehicle.dossier.fuelType',
        type: 'select',
        options: options(FuelType, 'fuelType'),
      },
      {
        key: 'transmission',
        label: 'vehicle.dossier.transmission',
        type: 'select',
        options: options(Transmission, 'transmission'),
      },
      {
        key: 'engineDisplacementCc',
        label: 'vehicle.dossier.engineDisplacementCc',
        type: 'number',
        min: 1,
        max: 100000,
      },
      { key: 'powerKw', label: 'vehicle.dossier.powerKw', type: 'number', min: 1, max: 10000 },
      { key: 'powerHp', label: 'vehicle.dossier.powerHp', type: 'number', min: 1, max: 10000 },
      {
        key: 'driveType',
        label: 'vehicle.dossier.driveType',
        type: 'select',
        options: options(DriveType, 'driveType'),
      },
      {
        key: 'euroStandard',
        label: 'vehicle.dossier.euroStandard',
        type: 'select',
        options: options(EuroStandard, 'euroStandard'),
      },
      {
        key: 'bodyType',
        label: 'vehicle.dossier.bodyType',
        type: 'select',
        options: options(BodyType, 'bodyType'),
      },
    ],
  },
  {
    title: 'vehicle.dossier.other',
    fields: [
      { key: 'color', label: 'vehicle.dossier.color', type: 'text', maxLength: 100 },
      {
        key: 'numberOfDoors',
        label: 'vehicle.dossier.numberOfDoors',
        type: 'number',
        min: 0,
        max: 10,
      },
      {
        key: 'numberOfSeats',
        label: 'vehicle.dossier.numberOfSeats',
        type: 'number',
        min: 1,
        max: 100,
      },
      {
        key: 'numberOfKeys',
        label: 'vehicle.dossier.numberOfKeys',
        type: 'number',
        min: 0,
        max: 20,
      },
      { key: 'importedFrom', label: 'vehicle.dossier.importedFrom', type: 'text', maxLength: 100 },
      { key: 'notes', label: 'vehicle.dossier.notes', type: 'textarea', maxLength: 4000 },
    ],
  },
];
