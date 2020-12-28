import { HttpClient } from "@angular/common/http";
import { Injectable, isDevMode } from "@angular/core";
import { environment } from "../../environments/environment";
import { AppConfigService, IAppConfig } from "../app.config";

@Injectable({ providedIn: 'root' })
export class AuthService {
    constructor(private http: HttpClient, private configService: AppConfigService) {

    }

    public init() {
        const config = this.configService.config;
        if (isDevMode()) {
            return this.initDev(config);
        }
        return this.initProd(config);
    }

    private async initDev(config: IAppConfig) {
        try {
            const url = `${config.api}/login/dev`;
            var respons = await fetch(url, {
                method: 'POST',
                credentials: 'include',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ username: environment.userName, password: environment.password })
            });

            if (!respons.ok) {
                throw new Error();
            }

            return Promise.resolve('Successfully signed in');
        } catch (err) {
            alert("Couldn't start local session! Check your username/password...");
            return Promise.reject("Couldn't start local session...");
        }
    }

    private initProd(config: IAppConfig) {
        // TODO
        return Promise.resolve('TODO');
    }
}