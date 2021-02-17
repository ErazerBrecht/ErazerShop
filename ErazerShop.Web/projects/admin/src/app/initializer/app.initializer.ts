import { Optional } from "@angular/core";
import { APP_INITIALIZER, InjectionToken } from "@angular/core";
import { AppConfigService } from "../app.config";
import { AuthService } from "../auth/auth.service";

function initializeErazerShop(appConfig: AppConfigService, auth: AuthService, appInits: (() => any)[]) {
    return async () => {
        await appConfig.loadConfig();
        await auth.init();

        if (appInits?.length) {
            const asyncInitPromises = InvokeAsyncFunctions(appInits);
            await Promise.all(asyncInitPromises);
        }
    }
}

function InvokeAsyncFunctions(inits: (() => any)[]): Promise<any>[] {
    const asyncInitPromises: Promise<any>[] = [];
    for (let i = 0; i < inits.length; i++) {
        const initResult = inits[i]();
        if (isPromise(initResult)) {
            asyncInitPromises.push(initResult);
        }
    }
    return asyncInitPromises;
}

// BORROWED FROM ANGULAR SOURCE CODE
function isPromise(obj: any): obj is Promise<any> {
    // allow any Promise/A+ compliant thenable.
    // It's up to the caller to ensure that obj.then conforms to the spec
    return !!obj && typeof obj.then === 'function';
}

export const ERAZERSHOP_APP_INITIALIZER = new InjectionToken<Array<() => void>>('ErazerShop Application Initializer');
export const ErazerShopInitializer = {
    provide: APP_INITIALIZER,
    useFactory: initializeErazerShop,
    deps: [AppConfigService, AuthService, [new Optional(), ERAZERSHOP_APP_INITIALIZER]], multi: true
}