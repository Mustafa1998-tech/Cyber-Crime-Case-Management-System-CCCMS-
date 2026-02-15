import { Component } from '@angular/core';
import { ReportsService, ReportItem } from './reports.service';

@Component({
  selector: 'app-reports',
  standalone: false,
  templateUrl: './reports.html',
  styleUrl: './reports.scss'
})
export class Reports {
  caseId = 0;
  reportType = 4;
  content = '';
  reports: ReportItem[] = [];

  constructor(private readonly reportsService: ReportsService) {}

  load(): void {
    if (!this.caseId) {
      return;
    }

    this.reportsService.getByCase(this.caseId).subscribe({
      next: (data) => (this.reports = data)
    });
  }

  create(): void {
    if (!this.caseId || !this.content) {
      return;
    }

    this.reportsService.create({
      caseId: this.caseId,
      reportType: this.reportType,
      content: this.content
    }).subscribe({
      next: () => {
        this.content = '';
        this.load();
      }
    });
  }
}
