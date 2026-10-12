# Private vehicle photos and documents

## Domain and persistence

Separate `VehiclePhoto` and `VehicleDocument` entities use integer IDs and belong to Vehicle through initialized navigation collections. Metadata contains original filename, content type, size, storage key, timestamp and current server-side user ID. Photos add category, caption, sort order and primary flag. Documents add category, title, document date and notes. No binary or direct DealershipId is stored on either child.

Enums: VehiclePhotoType = Other, Exterior, Interior, Engine, Vin, Odometer, Damage; VehicleDocumentType = Other, PurchaseInvoice, ForeignRegistration, Coc, ServiceInvoice, InspectionReport, RegistrationDocument, SaleContract.

Migration `20261012115834_AddVehiclePhotosAndDocuments` adds both tables, vehicle indexes, photo ordering index and a filtered unique index for one primary photo per vehicle. Foreign keys use Restrict. Sale logic is unchanged; sold vehicles retain files and allow historical uploads. The migration has been applied to the local development database without a reset.

## Storage and security

Application `IFileStorage` supports saving bounded streams, opening a stream by opaque key and deleting by key. Infrastructure `LocalFileStorage` is the current implementation; business services never resolve physical paths. Replace that implementation to adopt Azure Blob/S3 later.

`FileStorage:RootPath` defaults to `DealerManagerStorage` relative to the API content root, so local files normally live under `DealerManager.WebAPI/DealerManagerStorage/`. Configuration can override this per environment. The directory is ignored by Git, cannot be under wwwroot, and is not exposed by static middleware.

Keys are generated server-side: `dealerships/{tenant}/vehicles/{vehicle}/photos|documents/{random-guid}.{validated-extension}`. Original filenames are metadata only. Path traversal, absolute keys and symlink traversal are rejected. File responses use private/no-store caching and nosniff. Authenticated tenant ownership is checked before any private binary is opened; child IDs are matched with the route VehicleId. DTOs never expose StorageKey. Foreign resources and incorrect ID pairs return 404. Missing physical content is logged and returned as a 404 ProblemDetails with code `missingContent`.

Photo uploads: JPG/JPEG, PNG, WebP, up to 10 MiB each, 1–10 files per multipart request. Documents: PDF, JPG/JPEG, PNG, up to 20 MiB, one per request. Extension, MIME, declared size and basic magic bytes are validated on the server; storage also enforces the actual byte limit. This is basic signature validation, not antivirus or a full image decoder. Dates normalize to UTC and audit timestamps use TimeProvider.

## API and consistency

Under `/api/vehicles/{vehicleId}`:

- GET/POST `photos`: multipart uses repeated `files` fields, required enum `type`, optional `caption`.
- GET `photos/{photoId}/content`, PUT `photos/{photoId}/primary`, DELETE `photos/{photoId}`.
- GET/POST `documents`: multipart uses `file`, `type`, `title`, optional `documentDate` and `notes`.
- GET `documents/{documentId}/content` (optional `download=true` preserves OriginalFileName), PUT `documents/{documentId}` (JSON metadata only), DELETE `documents/{documentId}`.

Multiple photo uploads are one DB transaction. The first uploaded image is primary if none exists. SortOrder continues from the maximum; reads order primary first, then SortOrder/UploadedAt/Id. Primary switches flush the old flag before setting the new one inside a serializable transaction. Deleting a primary promotes the earliest remaining image in that same transaction.

Upload failures clean up any files already saved, including failures during a batch. Deletes commit DB metadata changes before attempting physical deletion; a failed physical cleanup is logged without restoring metadata or failing the successful business action. Such orphan files need future operational cleanup. Storage and DB are not a distributed transaction.

## Angular

Vehicle Details has Photos then Documents directly after the vehicle data section. Includes a lazy original-image gallery, primary badge, photo preview, multiple-photo picker/category/caption dialog, compact document list, PDF/image preview via authorized HttpClient blob content, download with original filename, metadata-only editing and delete confirmation dialogs. Object URLs are revoked when previews close or the component is destroyed. Loading/invalid/busy states block duplicate mutations; successful writes re-fetch metadata and show translated toasts. No manual refresh button. All UI text is in BG/EN JSON.

Original full-size images are used; thumbnails in the inventory list are intentionally deferred to avoid loading original images for every row. No resizing, compression, cloud storage, OCR/AI, EXIF, public sharing, scheduler, reorder UI or antivirus infrastructure is implemented.

## Verification

Backend tests use isolated random temporary storage roots (including real cookie/tenant tests), never development uploads. They cover multi-upload, validation, metadata and bytes, randomized storage keys, primary changes/deletions, rollback, failed cleanup, edit without replacing content, missing files, wrong route IDs, cross-tenant reads/mutations/uploads, traversal, actual stream limits and preservation through sale.

Angular tests cover multipart payloads, file validation, automatic metadata refresh, primary/delete actions, metadata edits, protected content requests and errors; existing vehicle/purchase suites isolate the gallery dependency. Builds and translation validation are run. The manual browser walkthrough could not be performed because the UI automation runtime exited at initialization; HTTP integration and Angular component tests are the verification evidence instead.
