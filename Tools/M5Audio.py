"""Original Starfall M5 audio, deterministic synthesis. No third party samples."""
from pathlib import Path
import json, wave
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
RATE = 44100
rng = np.random.default_rng(501)
metrics = {}

def save(name, samples):
    samples = np.asarray(samples)
    peak = max(float(np.max(np.abs(samples))), .001)
    samples = samples * min(1, .82 / peak)
    samples[:128] *= np.linspace(0, 1, 128)[:, None] if samples.ndim == 2 else np.linspace(0, 1, 128)
    samples[-256:] *= np.linspace(1, 0, 256)[:, None] if samples.ndim == 2 else np.linspace(1, 0, 256)
    for base in (ROOT/'ArtSource/M5/Audio', ROOT/'Assets/_Game/Audio'):
        path = base / (name + '.wav'); path.parent.mkdir(parents=True, exist_ok=True)
        with wave.open(str(path), 'wb') as wav:
            wav.setnchannels(2 if samples.ndim == 2 else 1); wav.setsampwidth(2); wav.setframerate(RATE)
            wav.writeframes((samples * 32767).astype('<i2').tobytes())
    metrics[name] = {'seconds': len(samples)/RATE, 'peak': round(float(np.max(np.abs(samples))), 4), 'rms': round(float(np.sqrt(np.mean(samples*samples))), 4)}

def impact(length, frequency, weight, decay, grit=.25):
    t = np.arange(int(RATE*length))/RATE
    noise = rng.normal(0, 1, len(t)); smooth = np.convolve(noise, np.ones(9)/9, mode='same')
    body = np.sin(2*np.pi*(frequency*t + frequency*.016*(1-np.exp(-t*90))))
    return weight*body*np.exp(-t*decay) + grit*(noise*.4+smooth)*np.exp(-t*(decay+12))

guns = [('pistol', .32, 150, .58, 22, .35), ('shotgun', .64, 68, .73, 9, .7), ('smg', .24, 260, .4, 32, .32), ('crossbow', .48, 430, .4, 14, .12), ('launcher', .9, 45, .75, 5, .42), ('arc', .6, 680, .36, 10, .15), ('workshop_smg', .3, 185, .54, 24, .32)]
for name, length, freq, weight, decay, grit in guns:
    signal = impact(length, freq, weight, decay, grit)
    if name == 'arc':
        t=np.arange(len(signal))/RATE; signal += .23*np.sin(2*np.pi*1450*t)*np.sin(2*np.pi*70*t)*np.exp(-t*8)
    if name == 'crossbow':
        t=np.arange(len(signal))/RATE; signal += .2*np.sin(2*np.pi*2200*t)*np.exp(-t*38)
    save('SFX/Weapons/'+name+'_fire', signal)
save('SFX/shot', impact(.32,150,.58,22,.35))
save('SFX/hit', impact(.16,620,.35,34,.2))
save('SFX/hurt', impact(.38,96,.55,13,.2))
save('SFX/dodge', impact(.32,320,.15,10,.22))
def chime(notes, length, spacing=.09):
    t=np.arange(int(RATE*length))/RATE; result=np.zeros(len(t))
    for i, freq in enumerate(notes):
        age=t-i*spacing; active=age>=0; a=np.maximum(age,0)
        result += active*(np.sin(2*np.pi*freq*a)+.16*np.sin(2*np.pi*freq*2*a))*.19*np.minimum(a*150,1)*np.exp(-a*5)
    return result
save('SFX/pickup', chime([659.25,987.77],.64))
save('SFX/ui', chime([523.25,783.99],.24,.025))
save('SFX/clear', chime([261.63,329.63,392,523.25,783.99],1.9,.14))
save('SFX/boss', chime([146.83,138.59,110],1.2,.18))
# Quiet, seamless modal pad. Frequencies use whole cycles in the 32 second loop.
t=np.arange(RATE*32)/RATE
left=np.zeros(len(t)); right=np.zeros(len(t))
for i,freq in enumerate([110,164.8125,220,261.625,329.625]):
    freq=round(freq*32)/32
    envelope=.018*(.7+.3*np.cos(2*np.pi*(i+1)*t/32))
    left += np.sin(2*np.pi*freq*t+i*.5)*envelope
    right += np.sin(2*np.pi*freq*t+i*.7)*envelope
save('Ambience/starport',np.column_stack([left,right]))
out=ROOT/'ArtSource/M5/Audio/analysis.json'; out.write_text(json.dumps(metrics,indent=2),encoding='utf-8')
print(json.dumps({'clips':len(metrics),'peakMax':max(v['peak'] for v in metrics.values()),'silent':sum(v['rms']==0 for v in metrics.values())}))
