import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import en from '../../../../assets/i18n/en.json';
import { VehicleFilesComponent } from './vehicle-files.component';
import { VehicleDocument, VehiclePhoto } from './vehicle-files.models';
const photo: VehiclePhoto = {
  id: 1,
  vehicleId: 42,
  type: 1,
  originalFileName: 'image.png',
  contentType: 'image/png',
  sizeBytes: 10,
  caption: null,
  isPrimary: true,
  sortOrder: 0,
  uploadedAt: '2026-10-12T10:00:00Z',
  uploadedByUserId: 1,
};
const document: VehicleDocument = {
  id: 3,
  vehicleId: 42,
  type: 2,
  title: 'Registration',
  originalFileName: 'registration.pdf',
  contentType: 'application/pdf',
  sizeBytes: 10,
  documentDate: null,
  notes: null,
  uploadedAt: photo.uploadedAt,
  uploadedByUserId: 1,
};
describe('Private vehicle photos and documents', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideTranslateService(),
        provideRouter([]),
        MessageService,
      ],
    });
    http = TestBed.inject(HttpTestingController);
    const translations = TestBed.inject(TranslateService);
    translations.setTranslation('en', en);
    translations.use('en').subscribe();
  });
  afterEach(() => http.verify());
  function create() {
    const f = TestBed.createComponent(VehicleFilesComponent);
    f.componentRef.setInput('vehicleId', 42);
    f.detectChanges();
    read();
    f.detectChanges();
    return f;
  }
  function read(photos: VehiclePhoto[] = [], documents: VehicleDocument[] = []) {
    http.expectOne('/api/vehicles/42/photos').flush(photos);
    http.expectOne('/api/vehicles/42/documents').flush(documents);
  }
  const select = (files: File[]) => ({ target: { files } }) as unknown as Event;
  it('uploads multiple photos once, reloads automatically and shows the primary gallery image', () => {
    const f = create();
    const c = f.componentInstance;
    expect(f.nativeElement.textContent).toContain('No photos added');
    c.openPhotos();
    c.selectPhotos(
      select([
        new File(['abc'], 'one.png', { type: 'image/png' }),
        new File(['def'], 'two.png', { type: 'image/png' }),
      ]),
    );
    c.photoForm.patchValue({ type: 1, caption: 'Exterior' });
    c.uploadPhotos();
    c.uploadPhotos();
    const request = http.expectOne('/api/vehicles/42/photos');
    expect(request.request.method).toBe('POST');
    const body = request.request.body as FormData;
    expect(body.getAll('files')).toHaveLength(2);
    expect(body.get('type')).toBe('1');
    request.flush([photo]);
    expect(c.loading()).toBe(true);
    expect(c.busy()).toBe(true);
    read([photo]);
    f.detectChanges();
    expect(c.photoDialog()).toBe(false);
    expect(c.busy()).toBe(false);
    expect(f.nativeElement.textContent).toContain('Primary');
    expect(f.nativeElement.querySelector('img').getAttribute('src')).toBe(
      '/api/vehicles/42/photos/1/content',
    );
    f.destroy();
  });
  it('validates file formats and sizes, preserves the draft on failure and has no binary replacement on edit', () => {
    const f = create();
    const c = f.componentInstance;
    c.openPhotos();
    c.selectPhotos(select([new File(['x'], 'bad.svg', { type: 'image/svg+xml' })]));
    c.uploadPhotos();
    http.expectNone('/api/vehicles/42/photos');
    expect(c.fileError()).toBe('files.errors.unsupported');
    const huge = new File(['x'], 'big.png', { type: 'image/png' });
    Object.defineProperty(huge, 'size', { value: 10 * 1024 * 1024 + 1 });
    c.selectPhotos(select([huge]));
    expect(c.fileError()).toBe('files.errors.tooLarge');
    c.openDocument(document);
    expect(c.documentForm.controls.title.value).toBe('Registration');
    c.documentForm.patchValue({ title: 'Updated', notes: 'Test' });
    c.saveDocument();
    const request = http.expectOne('/api/vehicles/42/documents/3');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body.file).toBeUndefined();
    expect(request.request.body.title).toBe('Updated');
    request.flush({}, { status: 500, statusText: 'Failed' });
    expect(c.documentDialog()).toBe(true);
    expect(c.documentForm.controls.title.value).toBe('Updated');
    expect(c.busy()).toBe(false);
    f.destroy();
  });
  it('changes primary and deletes only after confirmation then refreshes both sections', () => {
    const f = create();
    const c = f.componentInstance;
    c.previewPhoto({ ...photo, isPrimary: false });
    c.primary(photo);
    http.expectOne('/api/vehicles/42/photos/1/primary').flush(null);
    read([photo]);
    expect(c.photoPreview()).toBeNull();
    c.deleting.set({ kind: 'photo', id: 1 });
    http.expectNone('/api/vehicles/42/photos/1');
    c.confirmDelete();
    http.expectOne('/api/vehicles/42/photos/1').flush(null);
    read();
    expect(c.deleting()).toBeNull();
    expect(c.photos()).toHaveLength(0);
    f.destroy();
  });
  it('uploads a document with metadata and retrieves content through the protected endpoint', () => {
    const f = create();
    const c = f.componentInstance;
    c.openDocument();
    c.documentForm.patchValue({ type: 2, title: '  Registration  ', documentDate: '2026-10-12' });
    c.selectDocument(select([new File(['%PDF'], 'registration.pdf', { type: 'application/pdf' })]));
    c.saveDocument();
    const request = http.expectOne('/api/vehicles/42/documents');
    expect((request.request.body as FormData).get('title')).toBe('Registration');
    request.flush(document);
    read([], [document]);
    c.content(document, false);
    const content = http.expectOne('/api/vehicles/42/documents/3/content');
    expect(content.request.responseType).toBe('blob');
    content.flush(new Blob(['missing']), { status: 404, statusText: 'Not Found' });
    expect(c.error()).toBe('files.errors.notFound');
    expect(c.documentPreview()).toBeNull();
    f.destroy();
  });
});
