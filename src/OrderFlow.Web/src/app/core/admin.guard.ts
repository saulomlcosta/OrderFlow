import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from './auth.service';

export const adminGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);

  if (!auth.authenticated()) {
    await auth.login();
    return false;
  }

  return auth.isAdministrator() || inject(Router).parseUrl('/');
};
