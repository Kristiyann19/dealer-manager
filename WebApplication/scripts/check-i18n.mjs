import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { parseTemplate } from '@angular/compiler';

const flatten = (object, prefix = '') =>
  Object.fromEntries(
    Object.entries(object).flatMap(([key, value]) => {
      const name = prefix ? `${prefix}.${key}` : key;
      return typeof value === 'string' ? [[name, value]] : Object.entries(flatten(value, name));
    }),
  );
const en = flatten(JSON.parse(fs.readFileSync('src/assets/i18n/en.json', 'utf8')));
const bg = flatten(JSON.parse(fs.readFileSync('src/assets/i18n/bg.json', 'utf8')));
assert.deepEqual(
  Object.keys(en).sort(),
  Object.keys(bg).sort(),
  'Translation key sets must match.',
);
const placeholders = (value) =>
  [...value.matchAll(/\{\{\s*(\w+)\s*\}\}/g)].map((match) => match[1]).sort();
for (const key of Object.keys(en)) {
  assert.ok(en[key].trim() && bg[key].trim(), `Empty translation: ${key}`);
  assert.deepEqual(placeholders(en[key]), placeholders(bg[key]), `Interpolation mismatch: ${key}`);
}
const textAttributes = new Set([
  'aria-label',
  'ariaLabel',
  'closeAriaLabel',
  'placeholder',
  'pTooltip',
  'header',
  'message',
  'label',
  'context',
  'title',
]);
const files = fs
  .readdirSync('src/app', { recursive: true })
  .map((file) => path.join('src/app', file))
  .filter((file) => /\.(ts|html)$/.test(file) && !file.endsWith('.spec.ts'));
for (const file of files) {
  const source = fs.readFileSync(file, 'utf8');
  for (const match of source.matchAll(
    /'((?:ui|language|nav|profile|common|candidate|form|errors|dashboard|status|cost|months|specs)\.[A-Za-z0-9_ ./]+)'/g,
  )) {
    // Dot-only prefixes are used to map stable enum values to translation keys.
    if (!match[1].endsWith('.')) assert.ok(en[match[1]], `Missing key ${match[1]} in ${file}`);
  }
  const template = file.endsWith('.html') ? source : /template:\s*`([\s\S]*?)`/.exec(source)?.[1];
  if (!template) continue;
  const parsed = parseTemplate(template, file);
  assert.ok(!parsed.errors?.length, parsed.errors?.join('\n'));
  const seen = new Set();
  function checkExpression(expression) {
    if (!expression || typeof expression !== 'object') return;
    if (expression.constructor.name === 'LiteralPrimitive' && typeof expression.value === 'string') {
      const value = expression.value;
      const format = ['EUR', 'UTC', 'dd MMM y', 'dd MMM y, HH:mm', 'd MMM y'].includes(value);
      assert.ok(!/[A-ZА-Я]/.test(value) || format || en[value], `Untranslated display literal in ${file}: ${value}`);
    }
    for (const [key, value] of Object.entries(expression)) {
      if (key === 'sourceSpan') continue;
      if (Array.isArray(value)) value.forEach(checkExpression);
      else if (value && typeof value === 'object') checkExpression(value);
    }
  }
  function visit(node) {
    if (!node || typeof node !== 'object' || seen.has(node)) return;
    seen.add(node);
    if (node.constructor.name === 'BoundText' || (node.constructor.name === 'BoundAttribute' && textAttributes.has(node.name))) checkExpression(node.value?.ast);
    if (node.constructor.name === 'Text')
      assert.ok(!/[A-Za-zА-Яа-я]/.test(node.value), `Untranslated text in ${file}: ${node.value}`);
    if (node.constructor.name === 'TextAttribute' && textAttributes.has(node.name))
      assert.ok(!/[A-Za-zА-Яа-я]/.test(node.value), `Untranslated ${node.name} in ${file}`);
    for (const [name, value] of Object.entries(node))
      if (!['sourceSpan', 'valueSpan', 'keySpan', 'handler', 'value'].includes(name)) {
        if (Array.isArray(value)) value.forEach(visit);
        else if (value && typeof value === 'object') visit(value);
      }
  }
  parsed.nodes.forEach(visit);
}
console.log(
  `Verified ${Object.keys(en).length} matching translation keys, interpolation parameters and template text.`,
);
