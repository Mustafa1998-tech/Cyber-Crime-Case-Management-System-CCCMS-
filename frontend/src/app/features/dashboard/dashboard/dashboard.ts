import { Component, OnInit } from '@angular/core';
import { forkJoin } from 'rxjs';
import { ComplaintsService } from '../../complaints/complaints/complaints.service';
import { CasesService } from '../../cases/cases/cases.service';

interface DashboardBucket {
  label: string;
  value: number;
  percent: number;
}

@Component({
  selector: 'app-dashboard',
  standalone: false,
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard implements OnInit {
  complaintCount = 0;
  openCases = 0;
  underInvestigation = 0;
  forensicAnalysis = 0;
  topCrimeTypes: DashboardBucket[] = [];
  topStates: DashboardBucket[] = [];
  monthlyTrend: DashboardBucket[] = [];
  loading = true;

  constructor(
    private readonly complaintsService: ComplaintsService,
    private readonly casesService: CasesService
  ) {}

  ngOnInit(): void {
    forkJoin({
      complaints: this.complaintsService.getAll(),
      cases: this.casesService.getAll()
    }).subscribe({
      next: ({ complaints, cases }) => {
        this.complaintCount = complaints.length;
        this.openCases = cases.filter((x) => x.status !== 5).length;
        this.underInvestigation = cases.filter((x) => x.status === 2).length;
        this.forensicAnalysis = cases.filter((x) => x.status === 3).length;
        this.topCrimeTypes = this.toPercentBuckets(this.groupBy(complaints, (x) => x.crimeType), 6);
        this.topStates = this.toPercentBuckets(
          this.groupBy(complaints, (x) => this.extractState(x.description)),
          8
        );
        this.monthlyTrend = this.buildMonthlyTrend(complaints);
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  private groupBy<T>(items: T[], selector: (item: T) => string): Map<string, number> {
    const counts = new Map<string, number>();
    for (const item of items) {
      const key = selector(item).trim() || 'Unknown';
      counts.set(key, (counts.get(key) ?? 0) + 1);
    }

    return counts;
  }

  private toPercentBuckets(grouped: Map<string, number>, limit: number): DashboardBucket[] {
    const items = [...grouped.entries()]
      .sort((a, b) => b[1] - a[1])
      .slice(0, limit);

    const total = items.reduce((sum, [, value]) => sum + value, 0);
    if (total === 0) {
      return [];
    }

    return items.map(([label, value]) => ({
      label,
      value,
      percent: Math.max(3, Math.round((value / total) * 100))
    }));
  }

  private buildMonthlyTrend(complaints: { createdAtUtc?: string }[]): DashboardBucket[] {
    const now = new Date();
    const formatter = new Intl.DateTimeFormat('en', { month: 'short', year: '2-digit' });
    const buckets: { key: string; label: string; value: number }[] = [];

    for (let offset = 11; offset >= 0; offset -= 1) {
      const monthDate = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth() - offset, 1));
      const key = `${monthDate.getUTCFullYear()}-${String(monthDate.getUTCMonth() + 1).padStart(2, '0')}`;
      buckets.push({
        key,
        label: formatter.format(monthDate),
        value: 0
      });
    }

    const lookup = new Map<string, { key: string; label: string; value: number }>(
      buckets.map((x) => [x.key, x])
    );

    for (const complaint of complaints) {
      if (!complaint.createdAtUtc) {
        continue;
      }

      const createdAt = new Date(complaint.createdAtUtc);
      if (Number.isNaN(createdAt.getTime())) {
        continue;
      }

      const key = `${createdAt.getUTCFullYear()}-${String(createdAt.getUTCMonth() + 1).padStart(2, '0')}`;
      const bucket = lookup.get(key);
      if (bucket) {
        bucket.value += 1;
      }
    }

    const max = Math.max(1, ...buckets.map((x) => x.value));
    return buckets.map((x) => ({
      label: x.label,
      value: x.value,
      percent: Math.max(4, Math.round((x.value / max) * 100))
    }));
  }

  private extractState(description: string): string {
    const match = /\[State:([^\]]+)\]/i.exec(description);
    if (!match) {
      return 'Unknown';
    }

    return match[1].trim() || 'Unknown';
  }
}
