"""Validate delivered files and archive current M5 logs; no gameplay/image changes."""
import hashlib
import json
import re
import shutil
import struct
import xml.etree.ElementTree as ET
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT.parent / "appendix/M5"
assets = ROOT / "Assets"
missing_meta = []
guids = {}
for path in assets.rglob("*"):
    if path.name.endswith(".meta"):
        found = re.search(r"^guid: ([a-f0-9]{32})$", path.read_text(encoding="utf-8-sig"), re.M)
        if found:
            guids.setdefault(found.group(1), []).append(path.relative_to(ROOT).as_posix())
    elif not Path(str(path) + ".meta").is_file():
        missing_meta.append(path.relative_to(ROOT).as_posix())
duplicates = {key: values for key, values in guids.items() if len(values) > 1}
assert not missing_meta, missing_meta
assert not duplicates, duplicates

tests = {}
for name in ("editmode", "playmode", "interaction-final", "replay-test"):
    result = ET.parse(DEST / (name + ".xml")).getroot()
    tests[name] = dict(result.attrib)
    assert result.attrib["result"] == "Passed", name
    assert int(result.attrib.get("failed", "0")) == 0, name

screens = sorted((DEST / "screens").glob("*.png"))
assert len(screens) == 326
for path in screens:
    with Image.open(path) as screenshot:
        screenshot.verify()
replay = json.loads((DEST / "replay/capture-info.json").read_text(encoding="utf-8"))
frame_files = sorted((DEST / "replay/frames").glob("*.jpg"))
assert len(frame_files) == replay["original_frames"]
for path in frame_files:
    with Image.open(path) as frame:
        assert frame.size == (1280, 720)
        frame.verify()
avi = (DEST / "replay/first-stage-normal-input.avi").read_bytes()
assert avi[:4] == b"RIFF" and avi[8:12] == b"AVI "
assert struct.unpack_from("<I", avi, 4)[0] == len(avi) - 8
position = 12
movie = index = None
while position < len(avi):
    tag = avi[position:position + 4]
    size = struct.unpack_from("<I", avi, position + 4)[0]
    content = avi[position + 8:position + 8 + size]
    if tag == b"LIST" and content[:4] == b"movi":
        movie = content
    elif tag == b"idx1":
        index = content
    position += 8 + size + (size & 1)
assert position == len(avi) and movie is not None and index is not None
assert len(index) == replay["avi_frames"] * 16
for offset in range(0, len(index), 16):
    tag, flags, start, size = struct.unpack_from("<4sIII", index, offset)
    assert tag == movie[start:start + 4] == b"00dc" and flags == 0x10
    assert size == struct.unpack_from("<I", movie, start + 4)[0]
    assert movie[start + 8:start + 10] == b"\xff\xd8"

log = (ROOT / "Logs/M5-build.log").read_text(encoding="utf-8-sig")
reported = re.search(r"STARFALL_M5_BUILD_OK: (\d+) bytes, errors=(\d+)", log)
assert reported is not None and reported.group(2) == "0"
build = ROOT / "Builds/M5"
exe = build / "StarcoreLabyrinth.exe"
assert exe.is_file()
deployed = [path for path in build.rglob("*") if path.is_file() and
            not any("DontShip" in part or "DontShipItWithYourGame" in part for part in path.parts)]
for name in ("SourceHanSans-OFL.txt", "SourceHanSerif-OFL.txt", "Unity-UGUI-LICENSE.md"):
    assert (build / "Licenses" / name).is_file()
for src, target in (("M5-build.log", "build.log"), ("M5-setup.log", "assets-setup.log"),
                    ("M5-editmode.log", "editmode.log"), ("M5-playmode.log", "playmode.log"),
                    ("M5-interaction-final.log", "interaction-final.log"), ("M5-replay.log", "replay.log")):
    shutil.copy2(ROOT / "Logs" / src, DEST / target)
startup = (DEST / "player-startup.log").read_text(encoding="utf-8-sig")
assert not re.search(r"Exception|MissingReference|NullReference", startup)
info = {"date": "2026-10-03", "unity": "6000.6.4f1", "platform": "Windows x64",
        "options": "BuildOptions.None", "scene": "Assets/_Game/Scenes/Boot.unity",
        "exe": str(exe), "exe_sha256": hashlib.sha256(exe.read_bytes()).hexdigest(),
        "reported_build_bytes": int(reported.group(1)), "build_errors": 0,
        "deployable_file_bytes": sum(path.stat().st_size for path in deployed),
        "deployable_files": len(deployed), "startup_alive_seconds": 15,
        "startup_gpu": "NVIDIA GeForce RTX 4070 Laptop GPU",
        "startup_application_exceptions": False,
        "startup_diagnostic": "Optional D3D12 info queue query failed; device initialized successfully.",
        "tests": tests, "final_pngs": len(screens), "m5_matrix_pngs": sum(p.name.startswith("M5-") for p in screens),
        "replay": replay, "missing_asset_meta": missing_meta, "duplicate_asset_guids": duplicates,
        "not_measured": ["hardware hearing", "human aesthetics/feel/balance", "FPS/long-run performance"]}
(DEST / "build-info.json").write_text(json.dumps(info, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({key: info[key] for key in ("build_errors", "reported_build_bytes", "deployable_file_bytes", "final_pngs", "m5_matrix_pngs", "missing_asset_meta", "duplicate_asset_guids")}, ensure_ascii=False))
