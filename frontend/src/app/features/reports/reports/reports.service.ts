import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ReportItem {
  id: number;
  caseId: number;
  reportType: number;
  content: string;
  generatedByUserId: number;
  generatedAtUtc: string;
}

@Injectable({ providedIn: 'root' })
export class ReportsService {
  private readonly api = 'https://localhost:7261/api/v1/reports';

  constructor(private readonly http: HttpClient) {}

  getByCase(caseId: number): Observable<ReportItem[]> {
    return this.http.get<ReportItem[]>(`${this.api}/case/${caseId}`);
  }

  create(payload: { caseId: number; reportType: number; content: string }): Observable<number> {
    return this.http.post<number>(this.api, payload);
  }
}
