import { Component, OnInit } from '@angular/core';
import { ComplaintsService, Complaint } from './complaints.service';

@Component({
  selector: 'app-complaints',
  standalone: false,
  templateUrl: './complaints.html',
  styleUrl: './complaints.scss'
})
export class Complaints implements OnInit {
  complaints: Complaint[] = [];
  loading = false;

  form = {
    complainantName: '',
    phone: '',
    crimeType: 'Extortion',
    description: ''
  };

  constructor(private readonly complaintsService: ComplaintsService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.complaintsService.getAll().subscribe({
      next: (data) => {
        this.complaints = data;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  create(): void {
    this.complaintsService.create(this.form).subscribe({
      next: () => {
        this.form = {
          complainantName: '',
          phone: '',
          crimeType: 'Extortion',
          description: ''
        };
        this.load();
      }
    });
  }

  approve(complaintId: number): void {
    this.complaintsService.review(complaintId, { approved: true, priority: 'High' }).subscribe({
      next: () => this.load()
    });
  }

  reject(complaintId: number): void {
    const reason = prompt('Rejection reason') ?? 'Rejected by admin';
    this.complaintsService.review(complaintId, { approved: false, rejectionReason: reason }).subscribe({
      next: () => this.load()
    });
  }
}
