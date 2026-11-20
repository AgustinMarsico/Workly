const { spawn } = require('child_process');
const fs = require('fs');
const path = require('path');

const projectRoot = path.resolve(__dirname, '..');
const envPath = path.join(projectRoot, '.env');
const generator = path.join(__dirname, 'generate-env.js');

function runGenerator() {
  const p = spawn(process.execPath, [generator], { stdio: 'inherit' });
  p.on('error', (err) => console.error('Generator error:', err));
}

// Run once immediately
runGenerator();

// Watch for changes with a short debounce
let timer = null;
fs.watch(envPath, (eventType) => {
  if (timer) clearTimeout(timer);
  timer = setTimeout(() => {
    console.log('.env changed, regenerating env.js...');
    runGenerator();
  }, 150);
});

console.log('watch-env: watching .env for changes (press Ctrl+C to stop)');
