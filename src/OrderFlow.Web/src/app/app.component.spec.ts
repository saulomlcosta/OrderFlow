import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { AppComponent } from './app.component';
import { provideRouter } from '@angular/router';
import { AuthService } from './core/auth.service';

describe('AppComponent', () => {
  const auth = {
    authenticated: signal(false),
    username: signal<string | null>(null),
    isAdministrator: jasmine.createSpy('isAdministrator').and.returnValue(false),
    login: jasmine.createSpy('login').and.resolveTo(),
    logout: jasmine.createSpy('logout').and.resolveTo()
  };

  beforeEach(async () => {
    auth.authenticated.set(false);
    auth.username.set(null);
    auth.isAdministrator.and.returnValue(false);

    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: auth }
      ]
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render public navigation without administrative operations', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Storefront');
    expect(compiled.textContent).toContain('Sign in');
    expect(compiled.textContent).not.toContain('Checkout operations');
  });

  it('should render administrative navigation for an administrator', () => {
    auth.authenticated.set(true);
    auth.username.set('administrator');
    auth.isAdministrator.and.returnValue(true);

    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('Checkout operations');
    expect(compiled.textContent).toContain('Product operations');
    expect(compiled.textContent).toContain('administrator');
    expect(compiled.textContent).toContain('Sign out');
  });
});
