import { Routes } from '@angular/router';
import { authGuard, inventoryGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/login/login').then(m => m.LoginComponent)
  },
  {
    path: '',
    loadComponent: () => import('./features/shell/shell').then(m => m.ShellComponent),
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'orders', pathMatch: 'full' },
      {
        path: 'catalogue',
        loadComponent: () => import('./features/catalogue/catalogue').then(m => m.CatalogueComponent)
      },
      {
        path: 'inventory',
        canActivate: [inventoryGuard],
        loadComponent: () => import('./features/inventory/inventory').then(m => m.InventoryComponent)
      },
      {
        path: 'orders',
        loadComponent: () => import('./features/orders/order-list').then(m => m.OrderListComponent)
      },
      {
        path: 'orders/new',
        loadComponent: () => import('./features/orders/place-order').then(m => m.PlaceOrderComponent)
      },
      {
        path: 'orders/:id',
        loadComponent: () => import('./features/orders/order-detail').then(m => m.OrderDetailComponent)
      },
      {
        path: 'notifications',
        loadComponent: () => import('./features/notifications/notifications').then(m => m.NotificationsComponent)
      }
    ]
  },
  { path: '**', redirectTo: 'login' }
];
