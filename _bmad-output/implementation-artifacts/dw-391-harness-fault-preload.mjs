import childProcess from 'node:child_process';
import { syncBuiltinESMExports } from 'node:module';
import { appendFileSync } from 'node:fs';
const original = childProcess.spawn;
childProcess.spawn = function (file, args, options) {
    const target = file === 'dotnet' && args?.[0]?.endsWith('/Hexalith.Tenants.UI.dll');
    if (target && process.env.DW391_FAULT_MODE === 'missing-child') file = '/tmp/nonexistent-dw391-ui-executable';
    const child = original.call(this, file, args, options);
    if (target && child.pid) appendFileSync(process.env.DW391_PID_FILE, `${child.pid}\n`, { mode: 0o600 });
    return child;
};
syncBuiltinESMExports();
