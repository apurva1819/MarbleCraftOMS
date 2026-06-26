import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { LoginResponse, JwtPayload } from '../models/models';

const TOKEN_KEY = 'mc_token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly _token = signal<string | null>(localStorage.getItem(TOKEN_KEY));

  private readonly _payload = computed<JwtPayload | null>(() => {
    const t = this._token();
    if (!t) return null;
    try { return JSON.parse(atob(t.split('.')[1])) as JwtPayload; }
    catch { return null; }
  });

  readonly isLoggedIn    = computed(() => !!this._token());
  readonly role          = computed(() => {
    const p = this._payload() as Record<string, unknown> | null;
    if (!p) return null;
    return (p['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
         ?? p['role']) as string | null ?? null;
  });
  readonly username      = computed(() => this._payload()?.['unique_name'] ?? this._payload()?.sub ?? null);
  readonly distributorId = computed(() => {
    const val = this._payload()?.distributorId;
    if (val === undefined || val === null || val === '') return null;
    return +val;
  });

  constructor(private http: HttpClient, private router: Router) {}

  login(username: string, password: string) {
    return this.http
      .post<LoginResponse>('/api/v1/login', { username, password })
      .pipe(tap(res => {
        localStorage.setItem(TOKEN_KEY, res.token);
        this._token.set(res.token);
      }));
  }

  logout() {
    localStorage.removeItem(TOKEN_KEY);
    this._token.set(null);
    this.router.navigate(['/login']);
  }

  token(): string | null { return this._token(); }

  isSalesOrAdmin(): boolean {
    const r = this.role();
    return r === 'Admin' || r === 'SalesAgent';
  }

  isWarehouseStaff(): boolean { return this.role() === 'WarehouseStaff'; }

  canViewInventory(): boolean {
    const r = this.role();
    return r === 'Admin' || r === 'SalesAgent' || r === 'WarehouseStaff';
  }

  canAdjustStock(): boolean {
    const r = this.role();
    return r === 'Admin' || r === 'WarehouseStaff';
  }

  isDistributor(): boolean { return this.role() === 'Distributor'; }
}
