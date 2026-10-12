import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_BASE_URL } from '../../../configuration/api.config';
import { DocumentMetadata, VehicleDocument, VehiclePhoto } from './vehicle-files.models';
@Injectable({ providedIn: 'root' })
export class VehicleFilesApiService {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_BASE_URL).replace(/\/$/, '');
  private url(id: number) {
    return `${this.base}/vehicles/${id}`;
  }
  photos(id: number) {
    return this.http.get<VehiclePhoto[]>(`${this.url(id)}/photos`);
  }
  documents(id: number) {
    return this.http.get<VehicleDocument[]>(`${this.url(id)}/documents`);
  }
  photoUrl(id: number, photoId: number) {
    return `${this.url(id)}/photos/${photoId}/content`;
  }
  uploadPhotos(id: number, files: File[], type: number, caption: string) {
    const form = new FormData();
    for (const file of files) form.append('files', file, file.name);
    form.append('type', String(type));
    if (caption.trim()) form.append('caption', caption.trim());
    return this.http.post<VehiclePhoto[]>(`${this.url(id)}/photos`, form);
  }
  setPrimary(id: number, photoId: number) {
    return this.http.put<void>(`${this.url(id)}/photos/${photoId}/primary`, {});
  }
  deletePhoto(id: number, photoId: number) {
    return this.http.delete<void>(`${this.url(id)}/photos/${photoId}`);
  }
  uploadDocument(id: number, file: File, metadata: DocumentMetadata) {
    const form = new FormData();
    form.append('file', file, file.name);
    form.append('type', String(metadata.type));
    form.append('title', metadata.title);
    if (metadata.documentDate) form.append('documentDate', metadata.documentDate);
    if (metadata.notes) form.append('notes', metadata.notes);
    return this.http.post<VehicleDocument>(`${this.url(id)}/documents`, form);
  }
  editDocument(id: number, documentId: number, metadata: DocumentMetadata) {
    return this.http.put<VehicleDocument>(`${this.url(id)}/documents/${documentId}`, metadata);
  }
  deleteDocument(id: number, documentId: number) {
    return this.http.delete<void>(`${this.url(id)}/documents/${documentId}`);
  }
  documentContent(id: number, documentId: number) {
    return this.http.get(`${this.url(id)}/documents/${documentId}/content`, {
      responseType: 'blob',
    });
  }
}
