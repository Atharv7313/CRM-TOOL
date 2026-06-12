export interface Visit {
  visitId: string;
  enrollmentId: string;
  visitName: string;
  visitDate: string;
  visitStatus: string;
  prevVisit?: string;
  nextVisit?: string;
  patientName?: string;
  protocolTitle?: string;
  siteName?: string;
  enrollmentStatus?: string;
  visitNumber?: number;
  totalVisits?: number;
  protocolStartDate?: string;
  protocolEndDate?: string;
  enrollmentWindowEnd?: string;
}
