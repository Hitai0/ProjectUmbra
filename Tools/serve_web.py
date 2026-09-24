"""Serve an existing Unity Web build locally; binds only to localhost."""
from http.server import ThreadingHTTPServer, SimpleHTTPRequestHandler
from functools import partial
from pathlib import Path
import mimetypes

root=Path(__file__).resolve().parents[1]/'Builds'/'Web'
if not (root/'index.html').exists():
    raise SystemExit('Build first using Unity menu: Umbra > Build Web prototype')
mimetypes.add_type('application/wasm','.wasm')
class Handler(SimpleHTTPRequestHandler):
    def do_GET(self):
        if self.path in ('/','/index.html'):
            loader=next((root/'Build').glob('*.loader.js'),None)
            if loader is None:
                self.send_error(503,'Unity build is incomplete');return
            build_name=loader.name.removesuffix('.loader.js')
            html=(Path(__file__).parent/'web_shell.html').read_text(encoding='utf-8').replace('@@BUILD_NAME@@',build_name).encode('utf-8')
            self.send_response(200);self.send_header('Content-Type','text/html; charset=utf-8');self.send_header('Content-Length',str(len(html)));self.end_headers();self.wfile.write(html)
        else:
            super().do_GET()
    def end_headers(self):
        self.send_header('Cross-Origin-Opener-Policy','same-origin')
        self.send_header('Cross-Origin-Embedder-Policy','require-corp')
        super().end_headers()
server=ThreadingHTTPServer(('127.0.0.1',8080),partial(Handler,directory=str(root)))
print('Project Umbra: http://127.0.0.1:8080',flush=True)
server.serve_forever()
