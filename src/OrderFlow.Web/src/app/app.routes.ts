import { Routes } from '@angular/router';
import { adminGuard } from './core/admin.guard';
import { accountGuard } from './core/account.guard';
import { AdminCheckoutsComponent } from './features/admin-checkouts/admin-checkouts.component';
import { AdminProductsComponent } from './features/admin-products/admin-products.component';
import { AccountComponent } from './features/account/account.component';
import { LaboratoryComponent } from './features/laboratory/laboratory.component';

export const routes: Routes = [
  { path: '', component: LaboratoryComponent, title: 'Storefront | OrderFlow' },
  {
    path: 'account',
    component: AccountComponent,
    canActivate: [accountGuard],
    title: 'My Purchases | OrderFlow'
  },
  {
    path: 'admin/products',
    component: AdminProductsComponent,
    canActivate: [adminGuard],
    title: 'Catalog Operations | OrderFlow'
  },
  {
    path: 'admin/checkouts',
    component: AdminCheckoutsComponent,
    canActivate: [adminGuard],
    title: 'Checkout Operations | OrderFlow'
  },
  { path: '**', redirectTo: '' }
];
