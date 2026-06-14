const { app, BrowserWindow, session } = require('electron');
const path = require('path');
const { pathToFileURL } = require('url');

let traceSeq = 0;
function trace(event, details = {}) {
  const fields = Object.entries(details)
    .filter(([, value]) => value !== undefined && value !== null)
    .map(([key, value]) => `${key}=${JSON.stringify(String(value))}`)
    .join(' ');
  console.log(`[main] seq=${++traceSeq} event=${event}${fields ? ' ' + fields : ''}`);
}

process.on('uncaughtException', error => {
  trace('uncaught-exception', { message: error && error.stack || error });
});
process.on('unhandledRejection', reason => {
  trace('unhandled-rejection', { reason: reason && reason.stack || reason });
});

function getArg(name) {
  const prefix = `--${name}=`;
  const arg = process.argv.find(a => a.startsWith(prefix));
  if (!arg) return null;
  let value = arg.slice(prefix.length);
  if ((value.startsWith('"') && value.endsWith('"')) || (value.startsWith("'") && value.endsWith("'"))) {
    value = value.slice(1, -1);
  }
  return value;
}

const hostResolverRules = getArg('host-resolver-rules') || process.env.SKUA_HOST_RESOLVER_RULES;
if (hostResolverRules) {
  app.commandLine.appendSwitch('host-resolver-rules', hostResolverRules);
  trace('host-resolver-rules', { value: hostResolverRules });
}

const flashPlugin = getArg('flash-plugin') || process.env.SKUA_FLASH_PLUGIN;
if (flashPlugin) {
  app.commandLine.appendSwitch('ppapi-flash-path', flashPlugin);
  app.commandLine.appendSwitch('ppapi-flash-version', '32.0.0.371');
  trace('flash-plugin', { path: flashPlugin });
}
app.commandLine.appendSwitch('ignore-gpu-blacklist');
app.commandLine.appendSwitch('disable-site-isolation-trials');
app.commandLine.appendSwitch('no-sandbox');
app.commandLine.appendSwitch('enable-logging');

let mainWindow = null;

function createWindow() {
  const ws = getArg('ws');
  const swf = getArg('swf');
  trace('create-window', { ws, swf, argv: process.argv.join(' ') });
  if (!ws || !swf) {
    console.error('Missing required --ws or --swf argument.');
    app.quit();
    return;
  }

  mainWindow = new BrowserWindow({
    title: 'Skua AQW',
    width: 960,
    height: 580,
    backgroundColor: '#000000',
    show: true,
    resizable: true,
    useContentSize: true,
    autoHideMenuBar: true,
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: false,
      enableRemoteModule: false,
      plugins: true,
      devTools: true
    }
  });

  mainWindow.webContents.on('console-message', (_event, level, message, line, sourceId) => {
    trace('renderer-console', { level, message, line, sourceId });
  });
  mainWindow.webContents.on('did-fail-load', (_event, errorCode, errorDescription, validatedURL) => {
    trace('renderer-load-failed', { errorCode, errorDescription, validatedURL });
  });
  mainWindow.webContents.on('crashed', () => {
    trace('renderer-crashed');
  });
  mainWindow.webContents.on('plugin-crashed', (_event, name, version) => {
    trace('plugin-crashed', { name, version });
  });
  mainWindow.on('unresponsive', () => trace('window-unresponsive'));
  mainWindow.on('responsive', () => trace('window-responsive'));

  session.defaultSession.webRequest.onBeforeRequest((details, callback) => {
    trace('net-request', { method: details.method, url: details.url, resourceType: details.resourceType });
    callback({ cancel: false });
  });
  session.defaultSession.webRequest.onCompleted((details) => {
    trace('net-done', { method: details.method, statusCode: details.statusCode, url: details.url });
  });
  session.defaultSession.webRequest.onErrorOccurred((details) => {
    trace('net-error', { method: details.method, error: details.error, url: details.url });
  });

  const hostUrl = getArg('host-url');
  const html = hostUrl || pathToFileURL(path.join(__dirname, 'skua.html')).toString();
  const swfUrl = swf.startsWith('file:') || /^https?:/.test(swf) ? swf : pathToFileURL(swf).toString();
  const separator = html.includes('?') ? '&' : '?';
  const payloads = process.env.SKUA_FLASH_TRACE_PAYLOADS === '1' ? '&payloads=1' : '';
  const url = `${html}${separator}ws=${encodeURIComponent(ws)}&swf=${encodeURIComponent(swfUrl)}${payloads}`;
  trace('load-url', { url });
  mainWindow.loadURL(url).catch(err => {
    trace('load-url-error', { message: err && err.stack || err });
    app.quit();
  });

  mainWindow.on('closed', () => {
    trace('window-closed');
    mainWindow = null;
    app.quit();
  });
}

app.whenReady().then(() => {
  trace('app-ready');
  createWindow();
});
app.on('window-all-closed', () => {
  trace('window-all-closed');
  app.quit();
});
