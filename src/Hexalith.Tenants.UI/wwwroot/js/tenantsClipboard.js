export async function writeText(value) {
    if (!globalThis.isSecureContext) {
        throw new Error("TENANTS_CLIPBOARD_INSECURE");
    }

    if (!globalThis.navigator?.clipboard || typeof globalThis.navigator.clipboard.writeText !== "function") {
        throw new Error("TENANTS_CLIPBOARD_MISSING");
    }

    try {
        await globalThis.navigator.clipboard.writeText(value);
    } catch (error) {
        if (error?.name === "NotAllowedError") {
            throw new Error("TENANTS_CLIPBOARD_NOT_ALLOWED");
        }

        throw new Error("TENANTS_CLIPBOARD_FAILED");
    }
}
