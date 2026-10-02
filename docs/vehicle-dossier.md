# Vehicle dossier V1

Candidate stays compact: Make, Model, Year, Mileage, AskingPrice, Notes and optional VIN. Candidate VIN is trimmed, uppercased and limited to 32 characters; blank input becomes null. The existing purchase flow already copies Make/Model/Year/Mileage/VIN. Later dossier edits never update the source Candidate or its estimates.

Vehicle retains its existing VIN and Mileage and adds 16 nullable fields:

- RegistrationNumber, FirstRegistration (`DateOnly`, PostgreSQL `date`).
- FuelType, Transmission, EngineDisplacementCc, PowerKw, PowerHp, DriveType, EuroStandard, BodyType.
- Color, NumberOfDoors, NumberOfSeats, NumberOfKeys, ImportedFrom, Notes.

FuelType, Transmission, DriveType, BodyType and EuroStandard are nullable enums with explicit stable numeric values and translated BG/EN dropdowns. Unknown numeric values and arbitrary text are rejected. Other (99) is available; null means not provided. Color, ImportedFrom and Notes remain free text; no lookup tables were added. Both power units are independent optional integer measurements; no inferred conversion is stored. Numeric validation: mileage >= 0; engine 1–100000 cc; power 1–10000; doors 0–10; seats 1–100; keys 0–20. VIN/registration are trimmed and uppercased; blanks become null. Text lengths: VIN/registration 32, other short text 100, notes 4000. All dossier values can be cleared.

## API and tenancy

`PUT /api/vehicles/{id}/details` accepts `UpdateVehicleDossierRequest` and returns `VehicleDetailsDto`. It replaces only the editable dossier fields and Mileage/VIN. Omitted optional fields become null, so clients should send the complete current dossier when retaining values. It does not accept dealership, status, source candidate, purchase date, financial, listing or sale changes.

The action uses the existing repository, tracked tenant-filtered lookup, UnitOfWork transaction and SaveChanges tenant safeguards. Another dealership's vehicle returns 404. Existing lifecycle and calculations are unchanged. Sold vehicles retain their records and dossier, remain searchable and can receive dossier corrections.

Inventory search supports make/model/VIN/registration number, case-insensitively, within the current dealership. V1 deliberately uses search support for duplicate investigation; there is no global VIN uniqueness constraint or automatic duplicate warning. A future warning should distinguish the expected source-Candidate/purchased-Vehicle pair from independent duplicate records.

## UI

The dossier panel follows the financial KPI section, before pending costs. The compact view shows only VIN and Mileage in one row, with the existing missing-value translation. RegistrationNumber and FirstRegistration are hidden from the dossier display and edit form; previously stored values are preserved when saving other fields. Show all details expands into Identity, Technical and Other groups. Fewer than half of the 16 visible dossier fields populated produces a neutral incomplete message and Complete dossier action.

`/vehicles/{id}/edit` uses a grouped reactive form, loading/error states, optional fields, numeric validation and disabled save while invalid/pending. Success navigates back with a translated confirmation; the details component re-fetches the backend state. No manual refresh button. Existing shared styles and BG/EN translations are used. Candidate Details now displays VIN.

## Migration and verification

`20261001153909_AddVehicleDossier` adds nullable columns only. No historical row backfill, data deletion or reset. It was verified against legacy records in a temporary PostgreSQL schema with rollback, then applied to the configured local development database. The local API was restarted with the tested Release build.

Checks: backend regression suite 190 passing tests, plus the PostgreSQL migration test passing separately (191 cases verified); frontend 114 passing tests; 615 matching BG/EN translation keys; backend and Angular builds pass. Existing nullable-annotation warnings and Angular's initial bundle warning remain.

Manual flow: create candidate with optional VIN → estimate/approve/purchase → open vehicle → complete dossier → save → verify Candidate VIN unchanged → list/sell → search VIN or registration number in All vehicles or Sold. Existing purchase/sale permissions and tenant filters still apply.

Not included: mileage/service/registration histories, document uploads, VIN decoding, external providers, market lookup, gallery redesign or customer ownership history.

## Enum conversion

`20261001155137_VehicleDossierEnums` converts the five former text columns to integer enums. The migration recognizes enum names and common BG/EN labels, preserves null/blank as null, and maps unrecognized values to Other while retaining their original text in Notes. This is tested with an isolated PostgreSQL migration. Requests accept known enum names or numeric values; response DTOs use numeric values. Existing financial and tenant behavior is unchanged.
