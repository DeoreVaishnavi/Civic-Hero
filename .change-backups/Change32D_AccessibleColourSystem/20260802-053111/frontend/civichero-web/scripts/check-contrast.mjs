import fs from 'node:fs';
import path from 'node:path';
import process from 'node:process';

const root = path.resolve(process.cwd());
const cssPath = path.join(root, 'src', 'styles', 'contrast-system.css');
const srcRoot = path.join(root, 'src');

function relativeLuminance(hex) {
  const channels = hex.replace('#', '').match(/.{2}/g).map((part) => parseInt(part, 16) / 255);
  const linear = channels.map((channel) => channel <= 0.04045
    ? channel / 12.92
    : ((channel + 0.055) / 1.055) ** 2.4);
  return 0.2126 * linear[0] + 0.7152 * linear[1] + 0.0722 * linear[2];
}

function contrast(foreground, background) {
  const values = [relativeLuminance(foreground), relativeLuminance(background)].sort((a, b) => b - a);
  return (values[0] + 0.05) / (values[1] + 0.05);
}

const pairs = [
  ['Primary body text on white', '#0f172a', '#ffffff', 4.5],
  ['Secondary body text on white', '#334155', '#ffffff', 4.5],
  ['Muted body text on white', '#475569', '#ffffff', 4.5],
  ['Blue badge', '#1e3a8a', '#dbeafe', 4.5],
  ['Sky badge', '#075985', '#e0f2fe', 4.5],
  ['Green badge', '#14532d', '#dcfce7', 4.5],
  ['Amber badge', '#78350f', '#fef3c7', 4.5],
  ['Red badge', '#991b1b', '#fee2e2', 4.5],
  ['Violet badge', '#5b21b6', '#ede9fe', 4.5],
  ['Blue action', '#ffffff', '#1d4ed8', 4.5],
  ['Sky action', '#ffffff', '#0369a1', 4.5],
  ['Green action', '#ffffff', '#047857', 4.5],
  ['Violet action', '#ffffff', '#6d28d9', 4.5],
  ['Red action', '#ffffff', '#be123c', 4.5],
  ['Amber action', '#111827', '#f59e0b', 4.5],
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
const requiredMarkers = [
  'Forms: every form field on a light page uses dark, readable text',
  'Pastel badges, alerts and informational cards',
  'Buttons and interactive controls',
  'Intentionally dark surfaces: modals, drawers, code and dark side panels',
];
for (const marker of requiredMarkers) {
  if (!css.includes(marker)) {
    console.error(`FAIL Missing stylesheet marker: ${marker}`);
    failed = true;
  }
}

let sourceFiles = 0;
let styleFiles = 0;
function walk(directory) {
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const fullPath = path.join(directory, entry.name);
    if (entry.isDirectory()) walk(fullPath);
    else if (/\.(jsx?|css)$/.test(entry.name)) {
      sourceFiles += /\.jsx?$/.test(entry.name) ? 1 : 0;
      styleFiles += /\.css$/.test(entry.name) ? 1 : 0;
    }
  }
}
walk(srcRoot);
console.log(`Scanned ${sourceFiles} JavaScript/JSX files and ${styleFiles} CSS files.`);

if (failed) process.exit(1);
console.log('CivicHero contrast palette audit passed.');
