import { Component, OnInit } from '@angular/core';
import { CasesService, CaseItem } from './cases.service';

@Component({
  selector: 'app-cases',
  standalone: false,
  templateUrl: './cases.html',
  styleUrl: './cases.scss'
})
export class Cases implements OnInit {
  cases: CaseItem[] = [];
  loading = false;
  investigatorId = 0;
  selectedCaseId = 0;

  constructor(private readonly casesService: CasesService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.casesService.getAll().subscribe({
      next: (data) => {
        this.cases = data;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  changeStatus(caseId: number, newStatus: string): void {
    this.casesService.changeStatus(caseId, Number(newStatus)).subscribe({
      next: () => this.load()
    });
  }

  assign(): void {
    if (!this.selectedCaseId || !this.investigatorId) {
      return;
    }

    this.casesService.assign(this.selectedCaseId, this.investigatorId).subscribe({
      next: () => this.load()
    });
  }
}
