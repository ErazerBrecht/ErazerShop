import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";

@Injectable({ providedIn: 'root' })
export class AppConfigService {
    private _config: IAppConfig | undefined = undefined;

    constructor(private http: HttpClient) {
    }

    public get config(): IAppConfig {
        if (!this._config) {
            throw new Error('Config isn\'t loaded');
        }
        return this._config;
    }

    public loadConfig() {
        const jsonFile = `assets/config/app.json`;
        return new Promise<void>((resolve, reject) => {
            this.http.get<IAppConfig>(jsonFile).toPromise().then((response) => {
                this._config = response;
                resolve();
            }).catch((response: any) => {
                reject(`Could not load file '${jsonFile}': ${JSON.stringify(response)}`);
            });
        });
    }
}

export interface IAppConfig {
    api: string;
}