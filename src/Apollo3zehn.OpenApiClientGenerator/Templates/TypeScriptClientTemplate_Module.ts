import { HttpRequestHandler } from "./_shared";

/**
 * Provides access to the {{Version}} API.
 */
export interface I{{Version}} {
{{SubClientInterfaceProperties}}
}

/**
 * Provides access to the {{Version}} API.
 */
export class {{Version}} implements I{{Version}} {
{{SubClientFields}}

    constructor(invoke: HttpRequestHandler) {
{{SubClientFieldAssignments}}
    }

}

{{SubClientSource}}
{{Models}}
