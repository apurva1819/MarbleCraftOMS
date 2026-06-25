import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { LoginResponse, JwtPayload } from '../models/models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly _token = signal<string | null>(null);

  private readonly _payload = computed<JwtPayload | null>(() => {
    const t = this._token();
    if (!t) return null;
    try { return JSON.parse(atob(t.split('.')[1])) as JwtPayload; }
    catch { return null; }
  });

  readonly isLoggedIn   = computed(() => !!this._token());
  readonly role         = computed(() => this._payload()?.role ?? null);
  readonly username     = computed(() => this._payload()?.sub ?? null);
  readonly distributorId = computed(() => this._payload()?.distributorId ?? null);

  constructor(private http: HttpClient, private router: Router) {}

  login(username: string, password: string) {
    return this.http
      .post<LoginResponse>('/api/v1/login', { username, password })
      .pipe(tap(res => this._token.set(res.token)));
  }

  logout() {
    this._token.set(null);
    this.router.navigate(['/login']);
  }

  token(): string | null { return this._token(); }

  isSalesOrAdmin(): boolean {
    const r = this.role();
    return r === 'Admin' || r === 'SalesAgent';
  }

  isDistributor(): boolean { return this.role() === 'Distributor'; }
}
