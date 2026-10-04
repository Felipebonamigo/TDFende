# Baixa o GLB de cada bicho de bichos.json pela API do Sketchfab.
# Precisa de token (conta grátis): https://sketchfab.com/settings/password -> API token
#   SKETCHFAB_TOKEN=xxxx python3 baixa.py
import json, os, time, urllib.request
SCR = os.path.dirname(os.path.abspath(__file__))
os.makedirs(os.path.join(SCR, 'glb'), exist_ok=True)
tok = os.environ.get('SKETCHFAB_TOKEN')
for nome, c in json.load(open(os.path.join(SCR, 'bichos.json'))).items():
    out = os.path.join(SCR, 'glb', c['uid'] + '.glb')
    if os.path.exists(out): continue
    req = urllib.request.Request(f"https://api.sketchfab.com/v3/models/{c['uid']}/download",
                                 headers={'Authorization': f'Token {tok}'} if tok else {})
    d = json.load(urllib.request.urlopen(req))
    urllib.request.urlretrieve(d['glb']['url'], out)
    print(nome, os.path.getsize(out) // 1000, 'KB')
    time.sleep(3)  # a API devolve 429 se apressar
