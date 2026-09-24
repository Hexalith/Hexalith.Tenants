import { defineConfig } from '@playwright/test';
import { resolve } from 'node:path';

export default defineConfig({
    testDir: '.',
    timeout: 0,
    workers: 1,
    retries: 0,
    outputDir: resolve(process.env.TMPDIR ?? '/tmp', 'tenant-audit-playwright-output'),
    use: { trace: 'off', screenshot: 'off', video: 'off' },
});
