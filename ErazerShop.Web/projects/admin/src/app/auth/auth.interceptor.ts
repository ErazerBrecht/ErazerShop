import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse, HTTP_INTERCEPTORS } from "@angular/common/http";
import { Injectable, isDevMode } from "@angular/core";
import { Observable, from, of } from "rxjs";
import { catchError } from "rxjs/operators";
import { AuthService } from "./auth.service";

@Injectable({ providedIn: 'root' })
class AuthInterceptor implements HttpInterceptor {

    constructor(private authService: AuthService) { }

    async editRequest(req: HttpRequest<any>, next: HttpHandler): Promise<HttpEvent<any>> {
        if (isDevMode()) {
            const newReq = req.clone({ withCredentials: true });
            return next.handle(newReq).toPromise();
        }
        else {
            // TODO Add signing...
            return next.handle(req).toPromise();
        }
    }

    async handle401Error() {
        if (isDevMode()) {
            alert('Session expired/invalid! Press OK to start a new session');
            return this.authService.init().then(() => window.location.reload());
        }
        else {
            console.error('Session is unauthenticated. Refresh session...');
            window.location.href = "/login";
            return Promise.reject('Redirecting...');
        }
    }

    intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        return from(this.editRequest(req, next)).pipe(
            catchError((error) => {
                if (error instanceof HttpErrorResponse) {
                    switch ((<HttpErrorResponse>error).status) {
                        case 401:
                            return from(this.handle401Error());
                    }
                }
                return of(error);
            }));
    }
}

export const AuthHttpInterceptor = {
    provide: HTTP_INTERCEPTORS,
    useClass: AuthInterceptor,
    multi: true
}