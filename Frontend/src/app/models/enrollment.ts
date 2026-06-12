export interface Enrollment {
  enrollmentId: string;
  patientId: string;
  patientName: string;
  patientEmail?: string;
  protocolSiteId: string;
  protocolTitle: string;
  siteName: string;
  enrollmentStatus: string;
  enrolledAt: string;
  endDate?: string;
}

export interface Protocol {
  protocolId: string;
  title: string;
  startDate?: string;
  endDate?: string;
}

export interface ProtocolSite {
  protocolSiteId: string;
  siteName: string;
}
