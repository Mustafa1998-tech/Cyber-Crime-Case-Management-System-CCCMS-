import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Complaint {
  id: number;
  complainantName: string;
  phone: string;
  crimeType: string;
  description: string;
  status: number;
  createdAtUtc: string;
  createdByUserId: number;
  caseId?: number;
}

@Injectable({ providedIn: 'root' })
export class ComplaintsService {
  private readonly api = 'https://localhost:7261/api/v1/complaints';

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<Complaint[]> {
    return this.http.get<Complaint[]>(this.api);
  }

  create(payload: {
    complainantName: string;
    phone: string;
    crimeType: string;
    description: string;
  }): Observable<number> {
    return this.http.post<number>(this.api, payload);
  }

  review(complaintId: number, payload: {
    approved: boolean;
    assignedInvestigatorId?: number;
    priority?: string;
    rejectionReason?: string;
  }): Observable<{ caseId?: number }> {
    return this.http.post<{ caseId?: number }>(`${this.api}/${complaintId}/review`, payload);
  }
}
