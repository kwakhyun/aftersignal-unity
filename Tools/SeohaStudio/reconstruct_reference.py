"""Private TripoSR feasibility study; never a gameplay asset replacement.

The neural reconstruction is unmodified. Surface extraction uses scikit-image on
CPU instead of the optional CUDA C++ torchmcubes extension on Windows. Input is
the image_gen-produced square reference; this script does not repaint it.
"""
from pathlib import Path
import argparse
import json
import os
import sys
import time
import types

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Artifacts/SeohaStudio/Reconstruction"
os.environ.setdefault("HF_HOME", str(ROOT / "Artifacts/Tools/TripoSR-Models"))
sys.path.insert(0, str(ROOT / "Artifacts/Tools/TripoSR"))

import numpy as np
import torch
from PIL import Image
from skimage.measure import marching_cubes


def cpu_marching_cubes(level, threshold):
    # torchmcubes exposes reversed axes; TSR reverses those axes after this call.
    vertices, faces, _, _ = marching_cubes(
        level.detach().float().cpu().numpy(), threshold, allow_degenerate=False
    )
    return (torch.from_numpy(vertices[:, ::-1].copy()),
            torch.from_numpy(faces.astype(np.int64).copy()))


compat = types.ModuleType("torchmcubes")
compat.marching_cubes = cpu_marching_cubes
sys.modules["torchmcubes"] = compat

from tsr.system import TSR


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--resolution", type=int, default=384)
    parser.add_argument("--chunk-size", type=int, default=4096)
    parser.add_argument("--input", type=Path, default=ROOT / "Artifacts/SeohaStudio/Seoha-Reconstruction-Input.png")
    args = parser.parse_args()
    OUT.mkdir(parents=True, exist_ok=True)
    started = time.time()
    if not torch.cuda.is_available():
        raise RuntimeError("This local reconstruction study requires the installed CUDA runtime.")
    torch.set_num_threads(8)
    torch.manual_seed(20260913)
    print("Loading official MIT-licensed TripoSR weights", flush=True)
    model = TSR.from_pretrained("stabilityai/TripoSR", config_name="config.yaml", weight_name="model.ckpt")
    model.renderer.set_chunk_size(args.chunk_size)
    model.to("cuda:0").eval()
    reference = Image.open(args.input).convert("RGB")
    if reference.width != reference.height:
        raise ValueError("Supply a square reference to avoid distorted body proportions.")
    print("Reconstructing neural volume", flush=True)
    with torch.inference_mode():
        scene_codes = model([reference], device="cuda:0")
        torch.save(scene_codes.cpu(), OUT / "scene-codes.pt")
        print("Extracting actual colored mesh", flush=True)
        mesh = model.extract_mesh(scene_codes, True, resolution=args.resolution)[0]
    mesh.fix_normals()
    export_mesh = mesh.copy()
    export_mesh.apply_transform(np.array([
        [1, 0, 0, 0], [0, 0, 1, 0], [0, -1, 0, 0], [0, 0, 0, 1]
    ]))  # TripoSR Z-up -> glTF Y-up.
    export_mesh.export(OUT / "Seoha-TripoSR.glb")
    report = {
        "pipeline": "TripoSR", "license": "MIT", "input": str(args.input.relative_to(ROOT)),
        "vertices": len(mesh.vertices), "triangles": len(mesh.faces),
        "bounds": mesh.bounds.tolist(), "finite_vertices": bool(np.isfinite(mesh.vertices).all()),
        "elapsed_seconds": round(time.time() - started, 1),
        "gpu_peak_allocated_mib": round(torch.cuda.max_memory_allocated() / 1024**2),
        "surface_extractor": "scikit-image CPU marching cubes", "rigged": False,
        "output_coordinates": "glTF Y-up; reported raw bounds are TripoSR Z-up",
        "artistic_acceptance": False, "gameplay_replacement": False,
        "note": "Single-view neural study. It does not establish illustration-level likeness, animation topology or production readiness."
    }
    (OUT / "report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, indent=2), flush=True)


if __name__ == "__main__":
    main()
