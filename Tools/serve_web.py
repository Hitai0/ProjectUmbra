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
    def end_headers(self):
        self.send_header('Cross-Origin-Opener-Policy','same-origin')
        self.send_header('Cross-Origin-Embedder-Policy','require-corp')
        super().end_headers()
server=ThreadingHTTPServer(('127.0.0.1',8080),partial(Handler,directory=str(root)))
print('Project Umbra: http://127.0.0.1:8080',flush=True)
server.serve_forever()
