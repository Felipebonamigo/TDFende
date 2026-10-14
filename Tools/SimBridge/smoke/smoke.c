/* Teste de fumaça da ponte nativa: carrega a biblioteca, roda um replay de texto e imprime o fingerprint.
 * Uso: smoke ./tdfende_sim.so replay.txt   (compare com: dotnet run --project Tools/FlowSim -v quiet -- replay replay.txt --forcar) */
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <dlfcn.h>
#include "../include/tdfende_sim.h"

#define LOAD(name) name##_t name = (name##_t)dlsym(lib, #name); if (!name) { fprintf(stderr, "falta %s\n", #name); return 2; }
typedef int (*tdf_abi_version_t)(void);
typedef int (*tdf_struct_sizes_t)(int*);
typedef int (*tdf_create_t)(int,int,int,int,const char*,const char*);
typedef int (*tdf_last_error_t)(char*,int);
typedef int (*tdf_enqueue_t)(int,int,int,int);
typedef int (*tdf_step_t)(int);
typedef int (*tdf_snapshot_t)(void);
typedef int (*tdf_lanes_t)(TdfLane*);
typedef int (*tdf_enemies_t)(TdfEnemy*,int);
typedef int (*tdf_towers_t)(TdfTower*,int);
typedef int (*tdf_events_t)(TdfEvent*,int);
typedef int (*tdf_fingerprint_t)(char*,int);
typedef int (*tdf_signature_t)(char*,int);
typedef int (*tdf_tick_t)(void);

int main(int argc, char** argv) {
  if (argc < 3) { fprintf(stderr, "uso: smoke lib replay\n"); return 1; }
  void* lib = dlopen(argv[1], RTLD_NOW);
  if (!lib) { fprintf(stderr, "dlopen: %s\n", dlerror()); return 1; }
  LOAD(tdf_abi_version) LOAD(tdf_struct_sizes) LOAD(tdf_create) LOAD(tdf_last_error) LOAD(tdf_enqueue) LOAD(tdf_step)
  LOAD(tdf_snapshot) LOAD(tdf_lanes) LOAD(tdf_enemies) LOAD(tdf_towers) LOAD(tdf_events) LOAD(tdf_fingerprint) LOAD(tdf_signature) LOAD(tdf_tick)

  int sizes[5];
  tdf_struct_sizes(sizes);
  if (tdf_abi_version() != TDF_ABI_VERSION || sizes[0] != sizeof(TdfLane) || sizes[1] != sizeof(TdfEnemy) ||
      sizes[2] != sizeof(TdfTower) || sizes[4] != sizeof(TdfEvent)) {
    fprintf(stderr, "ABI ou tamanhos de struct diferentes: %d,%d,%d,%d,%d\n", sizes[0], sizes[1], sizes[2], sizes[3], sizes[4]);
    return 3;
  }

  FILE* f = fopen(argv[2], "r");
  if (!f) { perror("replay"); return 1; }
  int seed = 0, diff = 1, w = 24, h = 16, ticks = 0;
  static int ct[20000], ck[20000], cx[20000], cy[20000], ca[20000];
  int n = 0; char line[256];
  while (fgets(line, sizeof line, f)) {
    char word[32]; int a, b, c, d;
    if (sscanf(line, "seed %d", &seed) == 1) continue;
    if (strncmp(line, "difficulty", 10) == 0) { diff = strstr(line, "Dif") ? 2 : strstr(line, "F") ? 0 : 1; continue; }
    if (sscanf(line, "grid %d %d", &w, &h) == 2) continue;
    if (sscanf(line, "ticks %d", &ticks) == 1) continue;
    if (sscanf(line, "%d %31s", &a, word) == 2) {
      ct[n] = a; cx[n] = cy[n] = ca[n] = 0;
      if (!strcmp(word, "build") && sscanf(line, "%d %*s %d %d %d", &a, &b, &c, &d) >= 3) { ck[n] = TDF_CMD_BUILD; cx[n] = b; cy[n] = c; ca[n] = sscanf(line, "%d %*s %d %d %d", &a,&b,&c,&d) == 4 ? d : 0; n++; }
      else if (!strcmp(word, "upgrade") && sscanf(line, "%d %*s %d %d", &a, &b, &c) == 3) { ck[n] = TDF_CMD_UPGRADE; cx[n] = b; cy[n] = c; n++; }
      else if (!strcmp(word, "sell") && sscanf(line, "%d %*s %d %d", &a, &b, &c) == 3) { ck[n] = TDF_CMD_SELL; cx[n] = b; cy[n] = c; n++; }
      else if (!strcmp(word, "send") && sscanf(line, "%d %*s %d", &a, &b) == 2) { ck[n] = TDF_CMD_SEND; ca[n] = b; n++; }
    }
  }
  fclose(f);

  if (tdf_create(seed, diff, w, h, NULL, NULL) != 0) {
    char e[256]; tdf_last_error(e, sizeof e); fprintf(stderr, "create: %s\n", e); return 4;
  }
  int next = 0; long events = 0;
  static TdfEvent evbuf[4096];
  for (int t = 0; t < ticks; t++) {
    while (next < n && ct[next] <= t) { tdf_enqueue(ck[next], cx[next], cy[next], ca[next]); next++; }
    tdf_step(1);
    tdf_snapshot();
    events += tdf_events(evbuf, 4096);
  }
  TdfLane lanes[2]; tdf_lanes(lanes);
  static TdfEnemy en[4096]; int ne = tdf_enemies(en, 4096);
  static TdfTower tw[1024]; int nt = tdf_towers(tw, 1024);
  char fp[256], sg[128];
  tdf_fingerprint(fp, sizeof fp); tdf_signature(sg, sizeof sg);
  printf("tique %d | vidas %d x %d | inimigos %d | torres %d | eventos %ld | assinatura %s\n", tdf_tick(), lanes[0].Lives, lanes[1].Lives, ne, nt, events, sg);
  printf("fingerprint: %s\n", fp);
  return 0;
}
