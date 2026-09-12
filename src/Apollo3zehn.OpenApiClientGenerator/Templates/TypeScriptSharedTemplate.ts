/**
 * The request handler function type.
 */
export type HttpRequestHandler = <T>(
    method: string,
    relativeUrl: string,
    acceptHeaderValue?: string,
    contentTypeValue?: string,
    content?: BodyInit,
    signal?: AbortSignal) => Promise<T>;

/**
 * A {{{ExceptionType}}}.
 */
export class {{{ExceptionType}}} extends Error {
    statusCode: string;

    constructor(statusCode: string, message: string) {
        super(message);
        this.name = "{{{ExceptionType}}}";
        this.statusCode = statusCode;
    }
}
