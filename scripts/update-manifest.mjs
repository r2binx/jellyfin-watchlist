// Usage: node scripts/update-manifest.mjs <version 4-part> <zip path> <sourceUrl> [changelog]
import { createHash } from 'node:crypto';
import { readFileSync, writeFileSync } from 'node:fs';

const [version, zipPath, sourceUrl, changelog = ''] = process.argv.slice(2);
if (!/^\d+\.\d+\.\d+\.\d+$/.test(version || '') || !zipPath || !sourceUrl) {
  console.error('usage: update-manifest.mjs <a.b.c.d> <zip> <sourceUrl> [changelog]');
  process.exit(1);
}

const checksum = createHash('md5').update(readFileSync(zipPath)).digest('hex').toUpperCase();
const manifest = JSON.parse(readFileSync('manifest.json', 'utf8'));
const plugin = manifest[0];
plugin.versions = plugin.versions.filter((v) => v.version !== version);
plugin.versions.unshift({
  version,
  changelog,
  targetAbi: '12.0.0.0',
  sourceUrl,
  checksum,
  timestamp: new Date().toISOString().replace(/\.\d{3}Z$/, 'Z'),
});
writeFileSync('manifest.json', JSON.stringify(manifest, null, 2) + '\n');
console.log(`manifest.json: added ${version} (${checksum})`);
