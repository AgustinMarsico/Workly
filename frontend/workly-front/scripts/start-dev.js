const { spawn, spawnSync } = require('child_process');
const path = require('path');

const projectRoot = path.resolve(__dirname, '..');
const generateScript = path.join(__dirname, 'generate-env.js');
const watchScript = path.join(__dirname, 'watch-env.js');

function runNodeScript(scriptPath) {
  const result = spawnSync(process.execPath, [scriptPath], {
    cwd: projectRoot,
    stdio: 'inherit',
  });

  if (result.error) {
    throw result.error;
  }

  if (result.status !== 0) {
    process.exit(result.status ?? 1);
  }
}

function runNpmScript(script, args) {
  const npmCmd = process.platform === 'win32' ? 'npm.cmd' : 'npm';
  return spawn(npmCmd, ['run', script, '--', ...(args ?? [])], {
    cwd: projectRoot,
    stdio: 'inherit',
    shell: process.platform === 'win32',
  });
}

runNodeScript(generateScript);

const envWatcher = spawn(process.execPath, [watchScript], {
  cwd: projectRoot,
  stdio: 'inherit',
});

const angularServe = runNpmScript('ng', ['serve']);
let shuttingDown = false;

const shutdown = (code = 0) => {
  if (shuttingDown) return;
  shuttingDown = true;

  if (!envWatcher.killed) {
    envWatcher.kill();
  }
  if (!angularServe.killed) {
    angularServe.kill();
  }
  process.exit(code);
};

envWatcher.on('exit', (code) => shutdown(code ?? 0));

angularServe.on('exit', (code) => shutdown(code ?? 0));

process.on('SIGINT', () => shutdown(0));
process.on('SIGTERM', () => shutdown(0));
