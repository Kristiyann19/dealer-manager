import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  input,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, finalize, forkJoin, switchMap, tap } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import { DialogModule } from 'primeng/dialog';
import { MessageService } from 'primeng/api';
import { LocalizedDatePipe } from '../../../shared/pipes/localized-format.pipe';
import { requiredText, validOptionalDate } from '../vehicle-form-utils';
import { DOCUMENT_TYPES, PHOTO_TYPES, VehicleDocument, VehiclePhoto } from './vehicle-files.models';
import { VehicleFilesApiService } from './vehicle-files-api.service';
@Component({
  selector: 'app-vehicle-files',
  imports: [ReactiveFormsModule, TranslatePipe, DialogModule, LocalizedDatePipe],
  templateUrl: './vehicle-files.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block space-y-6' },
})
export class VehicleFilesComponent implements OnInit {
  readonly vehicleId = input.required<number>();
  readonly editable = input(true);
  readonly embedded = input(false);
  readonly photosExpanded = signal(false);
  readonly documentsExpanded = signal(false);
  readonly api = inject(VehicleFilesApiService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly messages = inject(MessageService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly fb = inject(FormBuilder);
  readonly photos = signal<VehiclePhoto[]>([]);
  readonly documents = signal<VehicleDocument[]>([]);
  readonly busy = signal(false);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly fileError = signal('');
  readonly photoDialog = signal(false);
  readonly documentDialog = signal(false);
  readonly editing = signal<VehicleDocument | null>(null);
  readonly selectedPhotos = signal<File[]>([]);
  readonly selectedDocument = signal<File | null>(null);
  readonly photoPreview = signal<VehiclePhoto | null>(null);
  readonly imageFailed = signal(false);
  readonly documentPreview = signal<{
    url: string;
    trustedUrl: SafeResourceUrl;
    title: string;
    pdf: boolean;
  } | null>(null);
  readonly deleting = signal<{ kind: 'photo' | 'document'; id: number } | null>(null);
  readonly photoTypes = PHOTO_TYPES;
  readonly documentTypes = DOCUMENT_TYPES;
  private readonly urls = new Set<string>();
  readonly photoForm = this.fb.nonNullable.group({
    type: [0, Validators.required],
    caption: ['', Validators.maxLength(1000)],
  });
  readonly documentForm = this.fb.nonNullable.group({
    type: [0, Validators.required],
    title: ['', [requiredText, Validators.maxLength(200)]],
    documentDate: ['', validOptionalDate],
    notes: ['', Validators.maxLength(2000)],
  });
  ngOnInit() {
    this.destroyRef.onDestroy(() => {
      for (const url of this.urls) URL.revokeObjectURL(url);
    });
    this.read()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.loading.set(false)),
      )
      .subscribe({
        next: (data) => this.apply(data),
        error: (e) => this.error.set(this.errorKey(e)),
      });
  }
  private read() {
    return forkJoin({
      photos: this.api.photos(this.vehicleId()),
      documents: this.api.documents(this.vehicleId()),
    });
  }
  private apply(data: { photos: VehiclePhoto[]; documents: VehicleDocument[] }) {
    this.photos.set(data.photos);
    this.documents.set(data.documents);
  }
  private errorKey(e: unknown): string {
    if (e instanceof HttpErrorResponse) {
      if (e.status === 413) return 'files.errors.tooLarge';
      if (e.status === 404) return 'files.errors.notFound';
      const code = e.error?.code;
      if (
        [
          'unsupported',
          'tooLarge',
          'empty',
          'invalidName',
          'invalidContent',
          'photoCount',
          'missingContent',
          'notFound',
        ].includes(code)
      )
        return 'files.errors.' + code;
      if (e.status === 400) return 'files.errors.metadata';
      if (e.status === 409) return 'files.errors.conflict';
    }
    return 'files.errors.unavailable';
  }
  private mutate(operation: Observable<unknown>, success: string) {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    operation
      .pipe(
        tap(() => {
          this.photoDialog.set(false);
          this.documentDialog.set(false);
          this.deleting.set(null);
          this.photoPreview.set(null);
          this.loading.set(true);
        }),
        switchMap(() => this.read()),
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.busy.set(false);
          this.loading.set(false);
        }),
      )
      .subscribe({
        next: (data) => {
          this.apply(data);
          this.messages.add({ severity: 'success', detail: success, life: 5000 });
        },
        error: (e) => this.error.set(this.errorKey(e)),
      });
  }
  openPhotos() {
    this.error.set('');
    this.fileError.set('');
    this.selectedPhotos.set([]);
    this.photoForm.reset({ type: 0, caption: '' });
    this.photoDialog.set(true);
  }
  private validateFiles(files: File[], photo: boolean): string {
    if (!files.length) return 'files.errors.empty';
    if (photo && files.length > 10) return 'files.errors.photoCount';
    const types: Record<string, string> = photo
      ? { jpg: 'image/jpeg', jpeg: 'image/jpeg', png: 'image/png', webp: 'image/webp' }
      : { pdf: 'application/pdf', jpg: 'image/jpeg', jpeg: 'image/jpeg', png: 'image/png' };
    for (const file of files) {
      const expected = types[file.name.split('.').pop()!.toLowerCase()];
      if (!expected || (file.type && file.type !== expected)) return 'files.errors.unsupported';
      if (!file.size) return 'files.errors.empty';
      if (file.size > (photo ? 10 : 20) * 1024 * 1024) return 'files.errors.tooLarge';
    }
    return '';
  }
  selectPhotos(event: Event) {
    const files = Array.from((event.target as HTMLInputElement).files ?? []);
    this.selectedPhotos.set(files);
    this.fileError.set(this.validateFiles(files, true));
  }
  selectDocument(event: Event) {
    const files = Array.from((event.target as HTMLInputElement).files ?? []);
    this.selectedDocument.set(files[0] ?? null);
    this.fileError.set(this.validateFiles(files, false));
  }
  uploadPhotos() {
    this.photoForm.markAllAsTouched();
    if (this.photoForm.invalid || this.fileError() || !this.selectedPhotos().length) return;
    const value = this.photoForm.getRawValue();
    this.mutate(
      this.api.uploadPhotos(this.vehicleId(), this.selectedPhotos(), value.type, value.caption),
      'files.photosUploaded',
    );
  }
  previewPhoto(photo: VehiclePhoto) {
    this.imageFailed.set(false);
    this.photoPreview.set(photo);
  }
  primary(photo: VehiclePhoto) {
    this.mutate(this.api.setPrimary(this.vehicleId(), photo.id), 'files.primarySaved');
  }
  openDocument(document: VehicleDocument | null = null) {
    this.error.set('');
    this.fileError.set('');
    this.editing.set(document);
    this.selectedDocument.set(null);
    this.documentForm.reset({
      type: document?.type ?? 0,
      title: document?.title ?? '',
      documentDate: document?.documentDate?.slice(0, 10) ?? '',
      notes: document?.notes ?? '',
    });
    this.documentDialog.set(true);
  }
  saveDocument() {
    this.documentForm.markAllAsTouched();
    if (
      this.documentForm.invalid ||
      this.fileError() ||
      (!this.editing() && !this.selectedDocument())
    )
      return;
    const value = this.documentForm.getRawValue();
    const metadata = {
      ...value,
      title: value.title.trim(),
      notes: value.notes.trim() || null,
      documentDate: value.documentDate
        ? new Date(value.documentDate + 'T00:00:00Z').toISOString()
        : null,
    };
    const document = this.editing();
    this.mutate(
      document
        ? this.api.editDocument(this.vehicleId(), document.id, metadata)
        : this.api.uploadDocument(this.vehicleId(), this.selectedDocument()!, metadata),
      'files.documentSaved',
    );
  }
  confirmDelete() {
    const item = this.deleting();
    if (!item) return;
    this.mutate(
      item.kind === 'photo'
        ? this.api.deletePhoto(this.vehicleId(), item.id)
        : this.api.deleteDocument(this.vehicleId(), item.id),
      'files.deleted',
    );
  }
  content(document: VehicleDocument, download: boolean) {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    this.api
      .documentContent(this.vehicleId(), document.id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.busy.set(false)),
      )
      .subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          this.urls.add(url);
          if (download) {
            const link = window.document.createElement('a');
            link.href = url;
            link.download = document.originalFileName;
            link.click();
            setTimeout(() => {
              URL.revokeObjectURL(url);
              this.urls.delete(url);
            }, 1000);
          } else {
            this.closeDocumentPreview();
            this.documentPreview.set({
              url,
              trustedUrl: this.sanitizer.bypassSecurityTrustResourceUrl(url),
              title: document.title,
              pdf: document.contentType === 'application/pdf',
            });
          }
        },
        error: (e) => this.error.set(this.errorKey(e)),
      });
  }
  closeDocumentPreview() {
    const old = this.documentPreview();
    if (old) {
      URL.revokeObjectURL(old.url);
      this.urls.delete(old.url);
    }
    this.documentPreview.set(null);
  }
}
