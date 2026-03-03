from __future__ import annotations

import re
import subprocess
import tempfile
import zlib
from pathlib import Path
from urllib.request import Request, urlopen

BASE_DIR = Path(__file__).resolve().parent
PLANTUML_SERVER = "https://www.plantuml.com/plantuml"
OUTPUT_DOCX = BASE_DIR / "Analisis_Seguridad_Cochera.docx"

DOCS = [
    BASE_DIR / "01-descripcion-del-sistema.md",
    BASE_DIR / "02-amenazas-y-vulnerabilidades-owasp.md",
    BASE_DIR / "03-analisis-codigo-inseguro.md",
    BASE_DIR / "04-propuesta-mejoras.md",
    BASE_DIR / "05-pruebas-sast-dast.md",
    BASE_DIR / "06-gestion-vulnerabilidades.md",
    BASE_DIR / "07-conclusiones.md",
]

PLANTUML_ALPHABET = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-_"


def _encode_6bit(value: int) -> str:
    return PLANTUML_ALPHABET[value & 0x3F]


def _append_3bytes(b1: int, b2: int, b3: int) -> str:
    c1 = b1 >> 2
    c2 = ((b1 & 0x3) << 4) | (b2 >> 4)
    c3 = ((b2 & 0xF) << 2) | (b3 >> 6)
    c4 = b3 & 0x3F
    return _encode_6bit(c1) + _encode_6bit(c2) + _encode_6bit(c3) + _encode_6bit(c4)


def plantuml_encode(text: str) -> str:
    data = zlib.compress(text.encode("utf-8"), 9)
    raw = data[2:-4]

    encoded: list[str] = []
    i = 0
    while i < len(raw):
        b1 = raw[i]
        b2 = raw[i + 1] if i + 1 < len(raw) else 0
        b3 = raw[i + 2] if i + 2 < len(raw) else 0
        encoded.append(_append_3bytes(b1, b2, b3))
        i += 3

    return "".join(encoded)


def render_plantuml_png(plantuml_source: str, output_file: Path) -> None:
    encoded = plantuml_encode(plantuml_source)
    url = f"{PLANTUML_SERVER}/png/{encoded}"
    request = Request(
        url,
        headers={
            "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
            "(KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36",
            "Accept": "image/png,*/*;q=0.8",
        },
    )
    with urlopen(request) as response:
        output_file.write_bytes(response.read())


def preprocess_markdown(md_path: Path, assets_dir: Path) -> str:
    content = md_path.read_text(encoding="utf-8")
    pattern = re.compile(r"```plantuml\s*\n(.*?)\n```", re.DOTALL)

    idx = 0

    def replacer(match: re.Match[str]) -> str:
        nonlocal idx
        idx += 1
        source = match.group(1).strip() + "\n"
        img_name = f"{md_path.stem}-puml-{idx:02d}.png"
        img_path = assets_dir / img_name
        render_plantuml_png(source, img_path)
        return f"![Diagrama {idx} - {md_path.stem}](assets/{img_name})"

    processed = pattern.sub(replacer, content)

    # Quitar reglas horizontales Markdown (---) para evitar líneas en DOCX
    processed = re.sub(r"(?m)^\s*---\s*$", "", processed)

    # Quitar numeración manual de encabezados para evitar duplicado con --number-sections
    # Ejemplos:
    # "# 01 — Descripción" -> "# Descripción"
    # "## 1.6 Superficie" -> "## Superficie"
    # "### 2.1.3 Flujo" -> "### Flujo"
    processed = re.sub(
        r"(?m)^(#{1,6}\s+)(?:\d+(?:\.\d+)*)(?:\s*[—-]\s*|\s+)(.+)$",
        r"\1\2",
        processed,
    )

    return processed


def build_docx() -> Path:
    for doc in DOCS:
        if not doc.exists():
            raise FileNotFoundError(f"No existe: {doc}")

    with tempfile.TemporaryDirectory(prefix="analisis_seguridad_") as temp_dir_str:
        temp_dir = Path(temp_dir_str)
        assets_dir = temp_dir / "assets"
        assets_dir.mkdir(parents=True, exist_ok=True)

        parts: list[str] = [
            "---",
            "title: \"Análisis de Seguridad — Cochera Inteligente\"",
            "lang: es-MX",
            "---",
            "",
        ]

        for i, doc in enumerate(DOCS):
            processed = preprocess_markdown(doc, assets_dir)
            parts.append(processed)
            if i < len(DOCS) - 1:
                parts.append("\n\\newpage\n")

        combined_md = temp_dir / "analisis_seguridad_completo.md"
        combined_md.write_text("\n".join(parts), encoding="utf-8")

        cmd = [
            "pandoc",
            str(combined_md),
            "--from",
            "markdown+pipe_tables+fenced_code_blocks+yaml_metadata_block",
            "--to",
            "docx",
            "--standalone",
            "--toc",
            "--number-sections",
            "--syntax-highlighting",
            "tango",
            "--resource-path",
            str(temp_dir),
            "-o",
            str(OUTPUT_DOCX),
        ]

        subprocess.run(cmd, check=True)

    return OUTPUT_DOCX


if __name__ == "__main__":
    output = build_docx()
    print(f"DOCX generado: {output}")
