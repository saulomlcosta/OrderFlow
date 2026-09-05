import { Routes } from '@angular/router';
import { AdminCheckoutsComponent } from './features/admin-checkouts/admin-checkouts.component';
import { LaboratoryComponent } from './features/laboratory/laboratory.component';

export const routes: Routes = [
  { path: '', component: LaboratoryComponent, title: 'Laboratory | OrderFlow' },
  { path: 'admin/checkouts', component: AdminCheckoutsComponent, title: 'Checkout Operations | OrderFlow' },
  { path: '**', redirectTo: '' }
];
