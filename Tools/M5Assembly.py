from pathlib import Path
import json
root=Path(__file__).resolve().parents[1]
for relative in ['Scripts/Starfall.Runtime.asmdef','Editor/Starfall.Editor.asmdef','Tests/PlayMode/Starfall.PlayModeTests.asmdef']:
    path=root/'Assets/_Game'/relative; data=json.loads(path.read_text(encoding='utf-8-sig'))
    reference='Unity.RenderPipelines.Universal.2D.Runtime'
    if reference not in data['references']: data['references'].append(reference)
    path.write_text(json.dumps(data,indent=2),encoding='utf-8')
