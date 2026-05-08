import { HttpInterceptorFn } from '@angular/common/http';
import { Router } from '@angular/router';
import { inject } from '@angular/core';
import { catchError } from 'rxjs/operators';

let alreadyShown = false;

export const unauthorizedInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);

  return next(req).pipe(
    catchError((error) => {
      if (error.status === 401) {
        localStorage.clear();

        if (!alreadyShown) {
          alreadyShown = true;

          alert(
            typeof error.error === 'string'
              ? error.error
              : error.error?.message ||
                'Acces neautorizat. Sesiunea a expirat sau nu aveți drepturi pentru această operațiune.'
          );

          router.navigate(['/']);

          setTimeout(() => {
            alreadyShown = false;
          }, 2000);
        }
      }

      throw error;
    })
  );
};