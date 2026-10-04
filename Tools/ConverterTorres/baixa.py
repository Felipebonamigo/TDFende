# baixa.py [nome]: baixa o GLB (com textura) de cada torre de torres.json pela API do Meshy.
# Precisa de MESHY_API_KEY no ambiente (no Claude Code na nuvem, o proxy já põe a chave).
import json, os, sys, urllib.request
SCR = os.path.dirname(os.path.abspath(__file__))
CFG = json.load(open(os.path.join(SCR, 'torres.json')))
os.makedirs(os.path.join(SCR, 'glb'), exist_ok=True)
key = os.environ.get('MESHY_API_KEY')
for nome, c in CFG.items():
    if nome.startswith('_') or (len(sys.argv) > 1 and nome not in sys.argv[1:]): continue
    req = urllib.request.Request('https://api.meshy.ai/openapi/v2/text-to-3d/' + c['meshy'])
    if key: req.add_header('Authorization', 'Bearer ' + key)
    task = json.load(urllib.request.urlopen(req))
    if task['status'] != 'SUCCEEDED': sys.exit(f"{nome}: tarefa {c['meshy']} está {task['status']}")
    out = os.path.join(SCR, 'glb', nome + '.glb')
    urllib.request.urlretrieve(task['model_urls']['glb'], out)
    print(nome, round(os.path.getsize(out) / 1e6, 1), 'MB')
