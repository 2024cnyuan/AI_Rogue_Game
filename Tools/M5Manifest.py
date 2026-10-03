"""Read-only resource inventory; never alters image or audio content."""
import hashlib
import json
import wave
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
records = []
for folder in ("Assets/_Game/Art", "Assets/_Game/UI", "Assets/_Game/Audio", "Assets/_Game/Data/Rendering", "Assets/_Game/Prefabs/Visuals"):
    for p in sorted((ROOT / folder).rglob("*")):
        if not p.is_file() or p.suffix == ".meta":
            continue
        entry = {"path": p.relative_to(ROOT).as_posix(), "bytes": p.stat().st_size,
                 "sha256": hashlib.sha256(p.read_bytes()).hexdigest()}
        if p.suffix.lower() == ".png":
            with Image.open(p) as im:
                entry.update(width=im.width, height=im.height, mode=im.mode)
                if "A" in im.getbands():
                    lo, hi = im.getchannel("A").getextrema()
                    entry.update(alpha_min=lo, alpha_max=hi)
        if p.suffix.lower() == ".wav":
            with wave.open(str(p), "rb") as audio:
                entry.update(channels=audio.getnchannels(), sample_rate=audio.getframerate(),
                             seconds=round(audio.getnframes()/audio.getframerate(), 3))
        records.append(entry)
dest = ROOT / "docs/M5/asset-manifest.json"
dest.write_text(json.dumps({"stage": "M5", "resources": records}, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
print(f"M5 manifest: {len(records)} files, {sum(r['bytes'] for r in records)/1024**2:.1f} MiB")
