# busca.py [peça...]: procura modelos na galeria pública da comunidade do Meshy (só licença CC0) e
# monta uma folha de miniaturas numeradas por peça, para escolher a olho.
#   out/<peça>.jpg   folha de miniaturas (número, autor, triângulos)
#   out/<peça>.json  os mesmos itens, com o link de cada um no site
# O GLB não sai por aqui: o Meshy só libera o download logado no site (e conta na cota do
# plano). Escolhido o modelo, baixe pelo link, salve em ../ConverterTorres/glb/<peça>.glb e
# converta com o ConverterTorres.
import json, os, sys, io, urllib.request, urllib.parse
from concurrent.futures import ThreadPoolExecutor
from PIL import Image, ImageDraw

SCR = os.path.dirname(os.path.abspath(__file__))
BUSCAS = json.load(open(os.path.join(SCR, 'buscas.json')))
OUT = os.path.join(SCR, 'out'); os.makedirs(OUT, exist_ok=True)
API = 'https://api.meshy.ai/web/public/v2/showcases'
POR_BUSCA, MAX_FOLHA, LADO = 40, 40, 200

def get(url):
    req = urllib.request.Request(url, headers={'User-Agent': 'TDFende-busca/1.0'})
    return urllib.request.urlopen(req, timeout=60).read()

def buscar(termo):
    q = urllib.parse.urlencode({'search': termo, 'pageNum': 1, 'pageSize': POR_BUSCA})
    return json.loads(get(f'{API}?{q}'))['result'] or []

def miniatura(item):
    # a capa (cdn.meshy.ai) pode estar fora da rede; a prévia da tarefa sai pelo api.meshy.ai
    for url in (item['thumb'], None):
        try:
            if url is None:
                t = json.loads(get(f"https://api.meshy.ai/web/public/v2/tasks/{item['tarefa']}"))['result']
                url = t['result']['previewUrl']
            return Image.open(io.BytesIO(get(url))).convert('RGB')
        except Exception:
            continue
    return None

pecas = [p for p in BUSCAS if not p.startswith('_') and (len(sys.argv) < 2 or p in sys.argv[1:])]
for peca in pecas:
    vistos, itens = set(), []
    cota = MAX_FOLHA // len(BUSCAS[peca])  # a relevância cai rápido: os primeiros de cada termo
    for termo in BUSCAS[peca]:
        pegos = 0
        for r in buscar(termo):
            if pegos >= cota: break
            if r['id'] in vistos or r.get('license') != 'cc0' or r.get('isNSFW') or not r.get('resultId'): continue
            vistos.add(r['id'])
            itens.append({'n': len(itens) + 1, 'busca': termo, 'id': r['id'], 'tarefa': r['resultId'],
                          'autor': r.get('author', ''), 'prompt': (r.get('objectPrompt') or '')[:200],
                          'triangulos': r.get('triangleCount', 0), 'downloads': r.get('downloads', 0),
                          'licenca': r['license'], 'link': f"https://www.meshy.ai/discover?showcaseId={r['id']}",
                          'thumb': r.get('solidThumbnailUrl') or r.get('thumbnailUrl')})
            pegos += 1
    with ThreadPoolExecutor(8) as ex: imgs = list(ex.map(miniatura, itens))
    col = 8; lin = (len(itens) + col - 1) // col
    folha = Image.new('RGB', (col * LADO, max(1, lin) * (LADO + 28)), (30, 30, 30)); d = ImageDraw.Draw(folha)
    for i, (it, im) in enumerate(zip(itens, imgs)):
        x, y = (i % col) * LADO, (i // col) * (LADO + 28)
        if im: im.thumbnail((LADO, LADO)); folha.paste(im, (x + (LADO - im.width) // 2, y))
        d.rectangle([x, y, x + 34, y + 18], fill=(0, 0, 0)); d.text((x + 3, y + 3), str(it['n']), fill='yellow')
        d.text((x + 3, y + LADO + 2), f"{it['autor'][:16]}", fill='white')
        d.text((x + 3, y + LADO + 14), f"{it['triangulos'] // 1000}k tri  {it['downloads']} dl", fill=(170, 170, 170))
    folha.save(os.path.join(OUT, peca + '.jpg'), quality=85)
    json.dump(itens, open(os.path.join(OUT, peca + '.json'), 'w'), ensure_ascii=False, indent=1)
    print(peca, len(itens), 'modelos CC0')
