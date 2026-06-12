export interface Patient {
  patientId: string;
  name: string;
  dateOfBirth: string;
  contactInfo?: string;
  patientStatus: string;
  createdAt: string;
  enrollmentStatus: string;
  enrollmentId?: string;
  protocolTitle?: string;
  siteName?: string;
  enrolledAt?: string;
  previousProtocols?: string[];
}
