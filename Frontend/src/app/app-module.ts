import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { CommonModule, TitleCasePipe } from '@angular/common';
import { provideHttpClient, withInterceptorsFromDi, HTTP_INTERCEPTORS } from '@angular/common/http';

import { AppRoutingModule } from './app-routing-module';
import { AppComponent } from './app';
import { AuthInterceptor } from './core/interceptors/auth.interceptor';
import { LoginComponent } from './pages/login/login.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { ProtocolsComponent }    from './modules/protocols/protocols/protocols.component';
import { CreateProtocolComponent } from './modules/protocols/create-protocol/create-protocol.component';
import { SitesComponent }        from './modules/protocols/sites/sites.component';
import { CreateSiteComponent }   from './modules/protocols/create-site/create-site.component';

import { UsersComponent } from './modules/users/users.component';
import { AuditComponent } from './modules/audit/audit.component';
import { SamplesComponent } from './modules/samples/samples/samples.component';
import { LabResultComponent } from './modules/samples/labresult/labresult.component';
import { ReportsComponent } from './modules/reports/reports.component';
import { NotificationsComponent } from './modules/notifications/notifications.component';
import { TrialsComponent } from './modules/trials/trials.component';
import { PatientsList }    from './modules/patients/patients-list/patients-list';
import { AddPatient }      from './modules/patients/add-patient/add-patient';
import { EnrollPatient }   from './modules/patients/enroll-patient/enroll-patient';
import { PatientDetail }   from './modules/patients/patient-detail/patient-detail';
import { ProtocolVisits }  from './modules/patients/protocol-visits/protocol-visits';
import { Visits }          from './modules/patients/visits/visits';
import { VisitDetail }     from './modules/patients/visit-detail/visit-detail';
import { MarkVisits }      from './modules/patients/mark-visits/mark-visits';

@NgModule({
  declarations: [
    AppComponent,
    LoginComponent,
    DashboardComponent,
    UsersComponent,
    AuditComponent,
    SamplesComponent,
    LabResultComponent,
    ReportsComponent,
    NotificationsComponent,
    TrialsComponent,
    PatientsList,
    AddPatient,
    EnrollPatient,
    PatientDetail,
    ProtocolVisits,
    Visits,
    VisitDetail,
    MarkVisits,
    ProtocolsComponent,
    CreateProtocolComponent,
    SitesComponent,
    CreateSiteComponent
  ],
  imports: [
    BrowserModule,
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    AppRoutingModule
  ],
  providers: [
    provideHttpClient(withInterceptorsFromDi()),
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
    TitleCasePipe
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
