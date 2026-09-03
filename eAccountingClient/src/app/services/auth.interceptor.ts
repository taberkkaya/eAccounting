import {
  HttpErrorResponse,
  HttpEvent,
  HttpInterceptorFn,
  HttpResponse,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, tap, throwError } from 'rxjs';
import { DemoErrorCode } from '../models/demo.model';
import { DemoService } from './demo.service';

const WRITE_ACTIONS = ['/create', '/update', '/deletebyid'];

const isWrite = (url: string): boolean => {
  const path = url.split('?')[0].toLowerCase();
  return WRITE_ACTIONS.some((action) => path.endsWith(action));
};

/**
 * Attaches the bearer token to every call and keeps the demo quota display in step
 * with what the API actually recorded, so the banner cannot drift from the server.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const demo = inject(DemoService);
  const token = localStorage.getItem('accessToken');

  const request = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(request).pipe(
    tap((event: HttpEvent<unknown>) => {
      if (event instanceof HttpResponse && demo.isDemo && isWrite(request.url)) {
        demo.refreshStatus();
      }
    }),
    catchError((error: HttpErrorResponse) => {
      const demoCode = error.error?.demoCode as DemoErrorCode | undefined;

      if (demoCode) demo.handleError(demoCode);

      return throwError(() => error);
    })
  );
};
