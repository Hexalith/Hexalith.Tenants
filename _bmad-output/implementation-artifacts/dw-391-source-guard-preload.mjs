// Fault injection only: change the observed post-build Git diff without touching source.
import childProcess from 'node:child_process';
import { syncBuiltinESMExports } from 'node:module';
const original = childProcess.execFileSync;
let sourceDiffReads = 0;
childProcess.execFileSync = function (file, args, options) {
    const result = original.call(this, file, args, options);
    if (file === 'git' && args?.[0] === 'diff' && args.includes('src/Hexalith.Tenants.UI')) {
        sourceDiffReads++;
        if (sourceDiffReads === 2) return `${result}\ncontrolled-post-build-source-change\n`;
    }
    return result;
};
syncBuiltinESMExports();
