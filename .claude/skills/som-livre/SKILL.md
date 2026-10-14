---
name: som-livre
description: 'Procura, confere a licença, trata e registra efeito sonoro e música do TDFende, na mesma linha da skill modelo-3d. Use antes de baixar, gravar ou gerar qualquer som, quando o Felipe pedir "põe um som de...", "troca a música" ou "falta áudio", e sempre que um som novo entrar no jogo.'
---
# Som livre: procurar, conferir, registrar

Regra do TDFende: a Fase 8 (`docs/roadmap/fase-08-som-e-sensacao.md`) quer 35 a 60 clipes CC0 de Kenney e Freesound, loudness
medido com ffmpeg, música sem registro no Content ID e tudo no manifesto. Comprado (camada privada) ou só CC0/CC BY ainda é
decisão do Felipe. O Claude não ouve: quem aprova a mixagem é o Felipe, de fone.

## Onde procurar, nesta ordem

1. O que o jogo já tem: nada ainda (BUG-03: o Unity só cria `AudioListener` quando falta câmera; o Unreal roda com `-NoSound`).
2. **Sonniss GDC Game Audio Bundle**: milhares de efeitos gravados por estúdios, livres para jogo comercial, sem crédito
   obrigatório. A licença proíbe redistribuir o som solto: o repositório é público, então som da Sonniss ou comprado vai para a camada privada (`Assets/_Privado`, entrada
   com `repoPublico: false`), nunca para o git.
3. **Kenney** (pacotes de áudio CC0) e **Freesound** filtrado por licença **CC0** (ou CC BY, com crédito). O download
   do Freesound em qualidade original exige login: quem baixa é o Felipe.
4. **OpenGameArt**: só itens CC0 ou CC BY; confira a licença item por item, não a do pacote.
5. Gerar ou encomendar: música ou voz por IA só com o Felipe decidindo antes, licença comercial clara e sem registro no Content ID.

Recusa: NC, ND, "uso pessoal", som de outro jogo ou filme, música com letra ou melodia conhecida, a BBC Sound Effects
(licença não comercial), e som de marca (vinheta, buzina registrada).

## Tratar antes de entrar

- Corte o silêncio do começo e do fim e confira se não estala no corte.
- Volume: normalize por categoria com o ffmpeg. Sem alvo escrito no jogo, use -23 LUFS integrado e pico de -1 dBTP:
  ```bash
  ffmpeg -i entrada.wav -af loudnorm=I=-23:TP=-1:LRA=11 -ar 48000 saida.wav
  ```
- Formato: OGG para o Unity (`Assets/Resources`, carregado pela `ArtLayers`); WAV 48 kHz para importar no Unreal; nada
  acima de 10 MB no git.

## Registrar (no mesmo commit)

Uma entrada em `docs/licencas/manifesto.json` (licença da lista do AuditaLicencas: CC0, ou CC-BY-4.0 com
`credito: obrigatorio` e `creditoTexto`; OFL ainda não está na lista) e o `AuditaLicencas --gera` no mesmo commit.
Som gerado por IA entra marcado e vai para a declaração (skill `declaracao-ia`).
