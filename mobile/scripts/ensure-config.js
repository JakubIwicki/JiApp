/* eslint-env node */
// Ensures src/config.generated.ts exists for tsc/jest/metro on fresh clones + CI.
// build-apk.sh overwrites it with real values from mobile/.env for device builds.
const fs = require('fs');
const path = require('path');
const src = path.join(__dirname, '..', 'src');
const target = path.join(src, 'config.generated.ts');
const example = path.join(src, 'config.generated.ts.example');
if (!fs.existsSync(target)) {
  fs.copyFileSync(example, target);
  console.log('ensure-config: created src/config.generated.ts from example');
}
