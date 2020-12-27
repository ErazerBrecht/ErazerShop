import { APP_INITIALIZER } from "@angular/core";
import { AppConfigService } from "./app.config";

function initializeAppConfig(appConfig: AppConfigService) {
    return () => appConfig.loadConfig();
}

export const ConfigInitialzer = {
    provide: APP_INITIALIZER,
    useFactory: initializeAppConfig,
    deps: [AppConfigService], multi: true
}