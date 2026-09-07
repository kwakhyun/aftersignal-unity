"""Import the six user-supplied WAV masters unchanged and measure playback gain.

Run with the bundled Python; ffmpeg is the existing local QA dependency.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import uuid
import wave
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
FILES = ["01_서하의집.wav", "02_애프터라이트.wav", "08_중앙역.wav",
         "09_밤의객실.wav", "11_과부하지휘자.wav", "16_유리잔향.wav"]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, default=Path.home() / "Downloads")
    args = parser.parse_args()
    encoder = ROOT / "Tools/.python-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe"
    target = ROOT / "Assets/AfterSignal/Resources/Audio/Music"
    target.mkdir(parents=True, exist_ok=True)
    report = []
    for name in FILES:
        source = args.source / name
        digest = hashlib.sha256(source.read_bytes()).hexdigest()
        destination = target / name
        if destination.exists() and hashlib.sha256(destination.read_bytes()).hexdigest() != digest:
            raise RuntimeError(f"Refusing to overwrite a different music master: {destination}")
        shutil.copy2(source, destination)
        meta = destination.with_suffix(".wav.meta")
        if not meta.exists():
            meta.write_text(f"""fileFormatVersion: 2
guid: {uuid.uuid4().hex}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 8
  defaultSettings:
    serializedVersion: 2
    loadType: 2
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 1
    quality: 0.8
    conversionMode: 0
    preloadAudioData: 0
  platformSettingOverrides: {{}}
  forceToMono: 0
  normalize: 0
  loadInBackground: 1
  ambisonic: 0
  3D: 0
  userData: User-supplied Suno instrumental; original WAV preserved
  assetBundleName:
  assetBundleVariant:
""", encoding="utf-8")
        measured = subprocess.run([str(encoder), "-hide_banner", "-i", str(source),
                                   "-af", "loudnorm=I=-18:TP=-1.5:LRA=11:print_format=json",
                                   "-f", "null", "-"], capture_output=True, check=True)
        log = measured.stderr.decode("utf-8", errors="replace")
        stats, _ = json.JSONDecoder().raw_decode(log[log.rfind("{"):])
        gain_db = min(-18 - float(stats["input_i"]), -1.5 - float(stats["input_tp"]))
        item = dict(file=name, bytes=source.stat().st_size, sha256=digest,
                    integrated_lufs=float(stats["input_i"]), true_peak_db=float(stats["input_tp"]),
                    playback_gain=round(10 ** (gain_db / 20), 6))
        with wave.open(str(source)) as wav:
            if wav.getsampwidth() != 2:
                raise ValueError("Expected the supplied 16-bit WAV masters")
            rate, channels = wav.getframerate(), wav.getnchannels()
            samples = np.frombuffer(wav.readframes(wav.getnframes()), dtype="<i2").astype(np.float32) / 32768
        block = rate * channels // 10
        rms = np.sqrt(np.mean(samples[:len(samples)//block*block].reshape(-1, block)**2, axis=1))
        audible = np.flatnonzero(rms > 0.01)  # -40 dBFS, measured in 100 ms windows
        item.update(duration_seconds=round(len(samples)/(rate*channels), 3),
                    loop_start_seconds=round(max(0, int(audible[0])/10 - 0.1), 3),
                    loop_end_seconds=round(min(len(samples)/(rate*channels), (int(audible[-1])+1)/10), 3))
        report.append(item)
        print(json.dumps(item, ensure_ascii=True), flush=True)
    output = ROOT / "Documentation/Audio"
    output.mkdir(parents=True, exist_ok=True)
    (output / "music-sources.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
