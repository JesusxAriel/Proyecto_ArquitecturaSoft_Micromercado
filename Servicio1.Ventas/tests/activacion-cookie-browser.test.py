"""Regresión real de SameSite: solo GET ficticios, nunca login/activación real.
Compilar antes el frontend en bin/activacion-usuarios. Requiere Chrome/Chromium.
CHROME_BIN permite seleccionar el ejecutable; usa un perfil temporal por prueba.
"""
import http.server
import os
from pathlib import Path
import shutil
import socket
import subprocess
import tempfile
import threading
import time
import unittest
import urllib.error
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
FRONTEND = ROOT / "frontend" / "PuntoMarket.Ventas.Frontend"
DLL = FRONTEND / "bin/activacion-usuarios/Debug/net10.0/PuntoMarket.Ventas.Frontend.dll"
TOKEN = "SoloFixtureBrowserNoEsTokenReal"

class Fixture(http.server.BaseHTTPRequestHandler):
    def log_message(self, *args):
        pass  # Nunca registrar URLs/cookies.

    def do_GET(self):
        if self.path.startswith("/correo"):
            destino = (f"http://localhost:{self.server.frontend_port}/Users/Activar?token={TOKEN}"
                       if self.path == "/correo-frontend" else f"http://localhost:{self.server.server_port}/inicio")
            self.send_response(200)
            self.send_header("Content-Type", "text/html; charset=utf-8")
            self.end_headers()
            self.wfile.write(f'<script>location.href="{destino}"</script>'.encode())
        elif self.path == "/inicio":
            self.send_response(302)
            self.send_header("Set-Cookie", "referencia=fixture; HttpOnly; SameSite=Strict; Path=/")
            self.send_header("Location", "/resultado")
            self.end_headers()
        else:
            presente = "referencia=fixture" in self.headers.get("Cookie", "")
            self.send_response(200)
            self.send_header("Content-Type", "text/html")
            self.end_headers()
            self.wfile.write(f'<p>cookie-presente={presente}</p>'.encode())

class ActivacionBrowserTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.chrome = os.environ.get("CHROME_BIN") or next((p for p in [
            r"C:\Program Files\Google\Chrome\Application\chrome.exe",
            r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
            shutil.which("chromium"), shutil.which("google-chrome")
        ] if p and Path(p).is_file()), None)
        if not cls.chrome:
            raise RuntimeError("Instale Chromium/Chrome o configure CHROME_BIN para esta regresión.")
        if not DLL.is_file():
            raise RuntimeError("Compile el frontend con -p:BaseOutputPath=bin/activacion-usuarios/ antes de ejecutar.")
        with socket.socket() as reserva:
            reserva.bind(("127.0.0.1", 0))
            cls.port = reserva.getsockname()[1]
        cls.proceso = subprocess.Popen([
            "dotnet", str(DLL), "--urls", f"http://127.0.0.1:{cls.port}",
            "--contentRoot", str(FRONTEND), "--environment", "Development",
            "--UsuariosApi:BaseUrl", "http://127.0.0.1:1/"
        ], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        cls.addClassCleanup(cls.detener_frontend)
        for _ in range(200):
            if cls.proceso.poll() is not None:
                raise RuntimeError("No arrancó el frontend de prueba.")
            try:
                urllib.request.urlopen(f"http://127.0.0.1:{cls.port}/Users/Activar", timeout=1).close()
                break
            except urllib.error.HTTPError:
                break  # 400 esperado sin enlace; confirma disponibilidad.
            except urllib.error.URLError:
                time.sleep(0.1)
        else:
            raise RuntimeError("Tiempo de arranque del frontend agotado.")
        cls.servidor = http.server.ThreadingHTTPServer(("127.0.0.1", 0), Fixture)
        cls.servidor.frontend_port = cls.port
        threading.Thread(target=cls.servidor.serve_forever, daemon=True).start()
        cls.addClassCleanup(cls.servidor.server_close)
        cls.addClassCleanup(cls.servidor.shutdown)

    @classmethod
    def detener_frontend(cls):
        cls.proceso.terminate()
        try:
            cls.proceso.wait(timeout=10)
        except subprocess.TimeoutExpired:
            cls.proceso.kill()
            cls.proceso.wait(timeout=5)

    def navegar(self, ruta):
        with tempfile.TemporaryDirectory(prefix="puntomarket-browser-fixture-", ignore_cleanup_errors=True) as perfil:
            r = subprocess.run([
                self.chrome, "--headless", "--no-first-run", "--no-default-browser-check", "--disable-gpu",
                "--user-data-dir=" + perfil, "--dump-dom", "--virtual-time-budget=3000",
                f"http://127.0.0.1:{self.servidor.server_port}/{ruta}"
            ], capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=30)
            self.assertEqual(0, r.returncode, "No pudo ejecutarse el navegador de prueba.")
            return r.stdout

    def test_01_reproduce_cookie_strict_omitida_en_redireccion_cross_site(self):
        # El navegador cambia de sitio 127.0.0.1 -> localhost, como webmail -> PuntoMarket.
        html = self.navegar("correo-control")
        self.assertIn("cookie-presente=False", html)

    def test_02_frontend_recupera_token_tras_enlace_cross_site_sin_exponerlo(self):
        html = self.navegar("correo-frontend")
        self.assertTrue('name="NuevaContrasena"' in html,
                        "Se perdió la correlación al redirigir desde el enlace externo.")
        self.assertIn('name="__RequestVerificationToken"', html)
        self.assertNotIn(TOKEN, html)
        self.assertNotIn('name="token"', html.lower())
        self.assertNotIn("Abra un enlace de activación válido", html)

if __name__ == "__main__":
    unittest.main(verbosity=2)
