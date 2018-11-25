import { HttpClient } from "./HttpClient";
import { ILogger } from "./ILogger";
import { ITransport, TransferFormat } from "./ITransport";
/** @private */
export declare class ServerSentEventsTransport implements ITransport {
    private readonly httpClient;
    private readonly accessTokenFactory;
    private readonly logger;
    private readonly logMessageContent;
    private eventSource;
    private url;
    constructor(httpClient: HttpClient, accessTokenFactory: () => string | Promise<string>, logger: ILogger, logMessageContent: boolean);
    connect(url: string, transferFormat: TransferFormat): Promise<void>;
    send(data: any): Promise<void>;
    stop(): Promise<void>;
    private close;
    onreceive: (data: string | ArrayBuffer) => void;
    onclose: (error?: Error) => void;
}
