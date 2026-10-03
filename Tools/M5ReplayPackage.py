"""Package Unity's original JPEG frames; never draw or modify image pixels."""
import bisect
import json
import math
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
REPLAY = ROOT.parent / "appendix/M5/replay"
entries = []
for row in (REPLAY / "frames.txt").read_text(encoding="utf-8").splitlines():
    seconds, filename = row.split("|", 1)
    entries.append((float(seconds), filename))
assert len(entries) > 120
times = [item[0] for item in entries]
fps = 12
count = math.ceil(times[-1] * fps) + 1
originals = {name: (REPLAY / "frames" / name).read_bytes() for _, name in entries}
frames = [originals[entries[max(0, bisect.bisect_right(times, i / fps) - 1)][1]]
          for i in range(count)]
maximum = max(map(len, frames))


def chunk(tag, data):
    return tag + struct.pack("<I", len(data)) + data + (b"\0" if len(data) & 1 else b"")


def listing(tag, data):
    return chunk(b"LIST", tag + data)


avih = struct.pack("<14I", round(1_000_000 / fps), maximum * fps, 0, 0x10,
                   count, 0, 1, maximum, 1280, 720, 0, 0, 0, 0)
strh = struct.pack("<4s4sIHHIIIIIIIIhhhh", b"vids", b"MJPG", 0, 0, 0,
                   0, 1, fps, 0, count, maximum, 0xFFFFFFFF, 0, 0, 0, 1280, 720)
strf = struct.pack("<IiiHH4sIiiII", 40, 1280, 720, 1, 24, b"MJPG",
                   1280 * 720 * 3, 0, 0, 0, 0)
header = listing(b"hdrl", chunk(b"avih", avih) +
                 listing(b"strl", chunk(b"strh", strh) + chunk(b"strf", strf)))
movie = bytearray()
index = bytearray()
for frame in frames:
    index.extend(struct.pack("<4sIII", b"00dc", 0x10, len(movie) + 4, len(frame)))
    movie.extend(chunk(b"00dc", frame))
body = b"AVI " + header + listing(b"movi", movie) + chunk(b"idx1", index)
(REPLAY / "first-stage-normal-input.avi").write_bytes(chunk(b"RIFF", body))

frame_data = json.dumps([[t, "frames/" + name] for t, name in entries])
html = '''<!doctype html><html lang="zh-CN"><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>M5 第一关正常输入录像</title><style>
body{margin:0;background:#071017;color:#eee7d5;font:17px system-ui,sans-serif}
main{max-width:1280px;margin:auto;padding:20px}h1{font-size:24px}
img{display:block;width:100%;aspect-ratio:16/9;background:#0b1820}
.bar{display:flex;gap:16px;align-items:center;margin:16px 0}input{flex:1}
button{background:#286d61;color:white;border:1px solid #64ac98;border-radius:4px;padding:10px 22px;font:inherit}
small{line-height:1.7;color:#bbc7c5}</style><main>
<h1>第一关 · 正常输入实际渲染录像</h1><img id="frame" alt="Unity 原始渲染帧">
<div class="bar"><button id="play">播放</button><input id="seek" type="range" min="0" step="0.01"><span id="clock"></span></div>
<small>正常键鼠自动操作，包含移动、开火、武器箱、E 拾取与 F 开门、信标及首领。
未注入伤害、传送、无敌或调整速度。原始 1280×720 Unity JPEG 帧按实际时间播放，未补绘或插值。
这是无声录像，声音软件链路证据另见 audio-runtime.txt；不替代真人听感、手感和节奏验收。</small>
</main><script>
const frames=FRAME_DATA, picture=document.getElementById('frame'),seek=document.getElementById('seek'),play=document.getElementById('play'),clock=document.getElementById('clock');
let playing=false,time=0,last=0,current=-1;const duration=frames.at(-1)[0];seek.max=duration;
function show(){let lo=0,hi=frames.length;while(lo<hi){const mid=(lo+hi)>>1;if(frames[mid][0]<=time)lo=mid+1;else hi=mid;}const idx=Math.max(0,lo-1);if(idx!==current){picture.src=frames[idx][1];current=idx;}seek.value=time;clock.textContent=time.toFixed(1)+' / '+duration.toFixed(1)+' 秒';}
play.onclick=()=>{if(time>=duration)time=0;playing=!playing;play.textContent=playing?'暂停':'播放';last=performance.now();show();};
seek.oninput=()=>{time=Number(seek.value);show();};
function tick(now){if(playing){time=Math.min(duration,time+(now-last)/1000);if(time>=duration){playing=false;play.textContent='播放';}show();}last=now;requestAnimationFrame(tick);}show();requestAnimationFrame(tick);
</script></html>'''.replace("FRAME_DATA", frame_data)
(REPLAY / "index.html").write_text(html, encoding="utf-8")
summary = {"source": "Unity 6000.6.4f1 URP actual rendering, normal keyboard/mouse bot",
           "width": 1280, "height": 720, "original_frames": len(entries),
           "actual_seconds": round(times[-1], 3), "avi_fps": fps, "avi_frames": count,
           "audio": "none; software audio evidence is separate",
           "avi_bytes": (REPLAY / "first-stage-normal-input.avi").stat().st_size,
           "normal_input": True, "teleport_or_injected_damage": False}
(REPLAY / "capture-info.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps(summary, ensure_ascii=False))
