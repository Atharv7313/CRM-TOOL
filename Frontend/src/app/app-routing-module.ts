import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LoginComponent }    from './pages/login/login.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { AuthGuard }          from './core/guards/auth.guard';

import { PatientsList }   from './modules/patients/patients-list/patients-list';
import { AddPatient }     from './modules/patients/add-patient/add-patient';
import { EnrollPatient }  from './modules/patients/enroll-patient/enroll-patient';
import { PatientDetail }  from './modules/patients/patient-detail/patient-detail';
import { ProtocolVisits } from './modules/patients/protocol-visits/protocol-visits';
import { Visits }         from './modules/patients/visits/visits';
import { VisitDetail }    from './modules/patients/visit-detail/visit-detail';
import { MarkVisits }     from './modules/patients/mark-visits/mark-visits';

const routes: Routes = [
  { path: 'login',                          component: LoginComponent },
  { path: 'dashboard',                      component: DashboardComponent,  canActivate: [AuthGuard] },
  { path: 'patients-list',                  component: PatientsList,        canActivate: [AuthGuard] },
  { path: 'add-patient',                    component: AddPatient,          canActivate: [AuthGuard] },
  { path: 'enroll-patient',                 component: EnrollPatient,       canActivate: [AuthGuard] },
  { path: 'patient-detail/:patientId',      component: PatientDetail,       canActivate: [AuthGuard] },
  { path: 'protocol-visits',                component: ProtocolVisits,      canActivate: [AuthGuard] },
  { path: 'visits',                         component: Visits,              canActivate: [AuthGuard] },
  { path: 'visit-detail/:enrollmentId',     component: VisitDetail,         canActivate: [AuthGuard] },
  { path: 'mark-visits',                    component: MarkVisits,          canActivate: [AuthGuard] },
  { path: '',                               redirectTo: 'login', pathMatch: 'full' },
  { path: '**',                             redirectTo: 'login' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
