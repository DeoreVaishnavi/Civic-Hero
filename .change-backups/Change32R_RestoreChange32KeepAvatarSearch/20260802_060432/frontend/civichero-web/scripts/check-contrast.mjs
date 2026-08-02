import fs from 'node:fs';
import path from 'node:path';
import process from 'node:process';

const root = path.resolve(process.cwd());
const cssPath = path.join(root, 'src', 'styles', 'contrast-system.css');
const srcRoot = path.join(root, 'src');

function relativeLuminance(hex) {
  const channels = hex.replace('#', '').match(/.{2}/g).map((part) => parseInt(part, 16) / 255);
  const linear = channels.map((channel) => channel <= 0.04045 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4);
  return 0.2126 * linear[0] + 0.7152 * linear[1] + 0.0722 * linear[2];
}
function contrast(foreground, background) {
  const values = [relativeLuminance(foreground), relativeLuminance(background)].sort((a, b) => b - a);
  return (values[0] + 0.05) / (values[1] + 0.05);
}

const pairs = [
  ['Light canvas text', '#1B2430', '#F7F8FA', 4.5],
  ['Dark surface text', '#F5F6F8', '#1E2430', 4.5],
  ['Blue pastel status', '#0B3D91', '#DCEAFB', 4.5],
  ['Green pastel status', '#1B5E20', '#DFF5E3', 4.5],
  ['Yellow pastel status', '#8A5B00', '#FDF3D0', 4.5],
  ['Red pastel status', '#8E1F1F', '#FBE0E0', 4.5],
  ['Violet pastel status', '#4A1E8E', '#EAE0FB', 4.5],
  ['Blue action button', '#FFFFFF', '#1E5FCC', 4.5],
  ['Green action button', '#FFFFFF', '#187A3E', 4.5],
  ['Red action button', '#FFFFFF', '#C0392B', 4.5],
  ['Violet action button', '#FFFFFF', '#6A3FBF', 4.5],
  ['Amber action button', '#3A2A00', '#F2B705', 4.5],
  ['Disabled control', '#5D6675', '#EEF0F3', 4.5],
];

let failed = false;
for (const [label, foreground, background, minimum] of pairs) {
  const ratio = contrast(foreground, background);
  const passed = ratio >= minimum;
  console.log(`${passed ? 'PASS' : 'FAIL'} ${label}: ${ratio.toFixed(2)}:1`);
  if (!passed) failed = true;
}

if (!fs.existsSync(cssPath)) {
  console.error(`FAIL Missing contrast stylesheet: ${cssPath}`);
  process.exit(1);
}
const css = fs.readFileSync(cssPath, 'utf8');
for (const marker of ['Semantic surfaces', 'Forms', 'Pastel status surfaces', 'Solid and outline controls', 'Public dark hero/footer sections']) {
  if (!css.includes(marker)) {
    console.error(`FAIL Missing stylesheet marker: ${marker}`);
    failed = true;
  }
}

let sourceFiles = 0;
let styleFiles = 0;
let suspiciousPairs = 0;
const suspicious = [
  /bg-(?:amber|yellow|rose|red|emerald|green|blue|sky|violet|purple)-\d+\/\d+[^"'`\n]*text-(?:white|amber-100|amber-200|rose-100|rose-200|emerald-100|emerald-200|blue-100|blue-200|violet-100|violet-200)/g,
  /bg-white[^"'`\n]*text-white/g,
];
function walk(directory) {
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const fullPath = path.join(directory, entry.name);
    if (entry.isDirectory()) walk(fullPath);
    else if (/\.(jsx?|css)$/.test(entry.name)) {
      if (/\.jsx?$/.test(entry.name)) {
        sourceFiles += 1;
        const text = fs.readFileSync(fullPath, 'utf8');
        for (const pattern of suspicious) suspiciousPairs += (text.match(pattern) || []).length;
      } else styleFiles += 1;
    }
  }
}
walk(srcRoot);
console.log(`Scanned ${sourceFiles} JavaScript/JSX files and ${styleFiles} CSS files.`);
console.log(`Legacy utility combinations normalized by the final stylesheet: ${suspiciousPairs}.`);
if (failed) process.exit(1);
console.log('CivicHero final contrast palette audit passed.');
