import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { from } from 'rxjs';
import { switchMap } from 'rxjs/operators';

import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);

  return from(auth.getValidToken()).pipe(
    switchMap(token => next(token
      ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : request))
  );
};
