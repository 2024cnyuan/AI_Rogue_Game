"""Copy the installed Unity TMP Essential shader sources into the game tree."""
from pathlib import Path
import tarfile
root=Path(__file__).resolve().parents[1]
package=next((root/'Library/PackageCache').glob('com.unity.ugui@*/Package Resources/TMP Essential Resources.unitypackage'))
with tarfile.open(package,'r:gz') as tar:
    count=0
    for entry in tar.getmembers():
        if not entry.name.endswith('/pathname'): continue
        name=tar.extractfile(entry).read().decode().strip()
        if '/Shaders/' not in name and 'LineBreaking ' not in name: continue
        base=entry.name.rsplit('/',1)[0]
        try: asset=tar.extractfile(base+'/asset')
        except KeyError: continue
        if asset is None: continue
        path=root/'Assets/_Game/UI/TMPShaders'/name.split('/Shaders/',1)[1] if '/Shaders/' in name else root/'Assets/_Game/UI/TMPResources'/Path(name).name
        path.parent.mkdir(parents=True,exist_ok=True); path.write_bytes(asset.read()); count+=1
        try: path.with_name(path.name+'.meta').write_bytes(tar.extractfile(base+'/asset.meta').read())
        except KeyError: pass
print(f'TMP bundled shaders copied: {count}')
