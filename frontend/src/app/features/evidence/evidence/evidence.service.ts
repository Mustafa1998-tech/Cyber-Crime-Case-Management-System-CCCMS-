import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface EvidenceItem {
  id: number;
  caseId: number;
  title: string;
  description?: string;
  versions: {
    id: number;
    versionNumber: number;
    originalFileName: string;
    sha256Hash: string;
    md5Hash: string;
    uploadedAtUtc: string;
  }[];
}

@Injectable({ providedIn: 'root' })
export class EvidenceService {
  private readonly api = 'https://localhost:7261/api/v1/evidence';

  constructor(private readonly http: HttpClient) {}

  getByCase(caseId: number): Observable<EvidenceItem[]> {
    return this.http.get<EvidenceItem[]>(`${this.api}/case/${caseId}`);
  }

  upload(payload: FormData): Observable<unknown> {
    return this.http.post(`${this.api}/upload`, payload);
  }

  download(versionId: number): Observable<Blob> {
    return this.http.get(`${this.api}/versions/${versionId}/download`, {
      responseType: 'blob'
    });
  }
}
