import { Component } from '@angular/core';
import { EvidenceItem, EvidenceService } from './evidence.service';

@Component({
  selector: 'app-evidence',
  standalone: false,
  templateUrl: './evidence.html',
  styleUrl: './evidence.scss'
})
export class Evidence {
  caseId = 0;
  title = '';
  description = '';
  deviceInfo = 'Web Client';
  file?: File;
  evidenceList: EvidenceItem[] = [];

  constructor(private readonly evidenceService: EvidenceService) {}

  onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.file = input.files?.[0];
  }

  load(): void {
    if (!this.caseId) {
      return;
    }

    this.evidenceService.getByCase(this.caseId).subscribe({
      next: (data) => (this.evidenceList = data)
    });
  }

  upload(): void {
    if (!this.file || !this.caseId || !this.title) {
      return;
    }

    const formData = new FormData();
    formData.append('caseId', this.caseId.toString());
    formData.append('title', this.title);
    formData.append('description', this.description);
    formData.append('deviceInfo', this.deviceInfo);
    formData.append('mimeType', this.file.type || 'application/octet-stream');
    formData.append('file', this.file);

    this.evidenceService.upload(formData).subscribe({
      next: () => {
        this.title = '';
        this.description = '';
        this.file = undefined;
        this.load();
      }
    });
  }

  download(versionId: number, fileName: string): void {
    this.evidenceService.download(versionId).subscribe((blob) => {
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = fileName;
      a.click();
      window.URL.revokeObjectURL(url);
    });
  }
}
