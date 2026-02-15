import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CaseItem {
  id: number;
  complaintId: number;
  complaintCrimeType: string;
  status: number;
  priority: string;
  assignedInvestigatorId?: number;
}

@Injectable({ providedIn: 'root' })
export class CasesService {
  private readonly api = 'https://localhost:7261/api/v1/cases';

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<CaseItem[]> {
    return this.http.get<CaseItem[]>(this.api);
  }

  changeStatus(caseId: number, newStatus: number): Observable<void> {
    return this.http.put<void>(`${this.api}/${caseId}/status`, { newStatus });
  }

  assign(caseId: number, investigatorId: number): Observable<void> {
    return this.http.post<void>(`${this.api}/${caseId}/assign`, { investigatorId });
  }
}
