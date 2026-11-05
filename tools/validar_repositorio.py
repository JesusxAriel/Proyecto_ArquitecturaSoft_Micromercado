"""Validaciones locales sin Docker, SQL, secretos externos ni escrituras de negocio."""
from pathlib import Path
import hashlib
import json
import re
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
DEMO = ROOT / "Servicio1.Ventas/database/demo-env"


class RepositorioTests(unittest.TestCase):
    def test_solucion_solo_referencia_proyectos_existentes_de_los_servicios(self):
        proyectos = [x.attrib["Path"] for x in ET.parse(ROOT / "PuntoMarket.slnx").getroot()]
        self.assertTrue(proyectos)
        self.assertEqual(len(proyectos), len(set(proyectos)))
        for nombre in proyectos:
            self.assertNotIn(".runtime-validation", nombre)
            self.assertTrue(nombre.startswith(("Servicio1.Ventas/", "Servicio2.Usuarios/", "Servicio3.Reportes/")))
            archivo = ROOT / nombre
            self.assertTrue(archivo.is_file(), nombre)
            for ref in ET.parse(archivo).getroot().iter("ProjectReference"):
                destino = (archivo.parent / ref.attrib["Include"].replace("\\", "/")).resolve()
                self.assertTrue(destino.is_file(), nombre)
                self.assertTrue(destino.is_relative_to(ROOT / nombre.split("/")[0]))

    def test_manifiesto_reconciliado_con_fuente_y_copia(self):
        m = json.loads((DEMO / "manifest.json").read_text(encoding="utf-8-sig"))
        for p, clave in [(ROOT / m["fuente"], "fuenteSha256"), (DEMO / m["copia"], "copiaSha256")]:
            self.assertEqual(hashlib.sha256(p.read_bytes()).hexdigest().upper(), m[clave].upper())

    def test_sql_publicable_no_contiene_hashes_contactos_reales_ni_borrado(self):
        for p in [ROOT / "bdMicroMercadoArqui.sql", DEMO / "sql/PuntoMarket.Ventas.Demo.sql"]:
            s = p.read_text(encoding="utf-8-sig")
            self.assertIsNone(re.search(r"\$2[aby]\$\d{2}\$[./A-Za-z0-9]{53}", s))
            self.assertIsNone(re.search(r"(?im)^\s*(DROP\s|TRUNCATE\s|DELETE\s+FROM|CREATE\s+OR\s+REPLACE)", s))
            self.assertTrue(all(c.endswith("@example.invalid") for c in re.findall(r"[\w.+-]+@[\w.-]+\.[A-Za-z]{2,}", s)))
            self.assertIn("@ventas_demo_admin_password_hash", s)
            self.assertIn("@ventas_demo_cajero_password_hash", s)

    def test_objetos_y_columna_de_actualizador_presentes(self):
        for p in [ROOT / "bdMicroMercadoArqui.sql", DEMO / "sql/PuntoMarket.Ventas.Demo.sql"]:
            s = p.read_text(encoding="utf-8-sig")
            for tipo, cantidad in [("TABLE", 11), ("VIEW", 4), ("TRIGGER", 23), ("PROCEDURE", 1)]:
                self.assertEqual(cantidad, len(re.findall(r"(?m)^CREATE " + tipo + r"\s", s)))
            tabla = re.search("CREATE TABLE " + chr(96) + "USUARIO" + chr(96) + "(.*?)ENGINE=InnoDB", s, re.S).group(1)
            self.assertIn("idUsuarioActualizacion", tabla)
            self.assertIn("FK_Usuario_Actualizador", tabla)
            self.assertIn("ON DELETE RESTRICT ON UPDATE CASCADE", tabla)

    def test_arbol_activo_no_contiene_el_despliegue_historico(self):
        for nombre in ["Proyecto_Arquitectura_Micromercado", "Proyecto_Arquitectura_Micromercado.Tests",
                       "Dockerfile", "docker-compose.yml", "nginx", "backups",
                       "PuntoMarket_Servicio1_Ventas_Entrega.zip", "Proyecto_Arquitectura_Micromercado.slnx", "doc/.gitkeep"]:
            self.assertFalse((ROOT / nombre).exists(), nombre)

    def test_scripts_vigentes_no_cargan_fuentes_historicas(self):
        for servicio in ["Servicio1.Ventas", "Servicio2.Usuarios", "Servicio3.Reportes", "tools"]:
            for p in (ROOT / servicio).rglob("*.ps1"):
                if any(x in ["bin", "obj"] for x in p.parts):
                    continue
                s = p.read_text(encoding="utf-8-sig")
                self.assertNotIn("Proyecto_Arquitectura_Micromercado/", s, str(p.relative_to(ROOT)))
                self.assertNotIn("ArchivoSqlFuente", s, str(p.relative_to(ROOT)))
        cuentas = (ROOT / "tools/Preparar-CuentasLocales.ps1").read_text(encoding="utf-8-sig")
        self.assertIn("if (-not $Aplicar)", cuentas)
        self.assertIn("REQUIRE SSL", cuentas)
        self.assertNotIn("ALTER USER", cuentas)
        self.assertNotIn("GRANT ALL", cuentas)

    def test_exclusiones_permiten_publicos_y_protegen_privados(self):
        for nombre in [".gitignore", ".dockerignore"]:
            s = (ROOT / nombre).read_text(encoding="utf-8-sig")
            for patron in [".env", "!.env.example", "*.zip", "*.pfx", "*.key", "*backup*.sql"]:
                self.assertIn(patron, s)
            self.assertNotIn("**/appsettings*.json", s)
            self.assertNotIn("\n*.sql\n", s)

    def test_enlaces_de_documentacion_nueva_existen(self):
        for nombre in ["README.md", "doc/integracion/INTEGRACION_VENTAS_REPORTES.md", "doc/despliegue/ESTADO_DOCKER.md"]:
            p = ROOT / nombre
            for enlace in re.findall(r"\]\(([^)]+)\)", p.read_text(encoding="utf-8-sig")):
                if "://" not in enlace and not enlace.startswith("#"):
                    self.assertTrue((p.parent / enlace.split("#")[0]).is_file(), nombre + ": " + enlace)


if __name__ == "__main__":
    unittest.main(verbosity=2)
