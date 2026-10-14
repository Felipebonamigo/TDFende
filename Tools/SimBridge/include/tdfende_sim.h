/*
 * tdfende_sim.h — interface C da Sim do TDFende (U0 da migração para o Unreal).
 *
 * A biblioteca (tdfende_sim.dll / .so) é a Sim em C# compilada em NativeAOT. Contrato:
 *  - só tipos planos; os structs abaixo espelham Runtime/Sim/Bridge/BridgeTypes.cs (a vista confere com tdf_struct_sizes);
 *  - nenhuma exceção atravessa: as funções devolvem código de erro e tdf_last_error dá o texto (UTF-8);
 *  - a memória dos buffers é de quem chama; textos são UTF-8 terminados em zero;
 *  - UM host por processo (os catálogos são estáticos). Chame na thread do jogo, sem concorrência;
 *  - coordenadas são as da Sim: unidades de célula, Y para cima, X ao longo do corredor. A vista converte (Unreal: Z para cima, cm).
 * ABI_VERSION sobe quando um struct ou função muda.
 */
#ifndef TDFENDE_SIM_H
#define TDFENDE_SIM_H

#ifdef __cplusplus
extern "C" {
#endif

#define TDF_ABI_VERSION 2   /* 2 (U2): + tdf_catalog_text, tdf_can_build, tdf_upgrade_cost (só acréscimos) */

#if defined(_WIN32)
  #define TDF_API __declspec(dllimport)
#else
  #define TDF_API
#endif

/* Bits de TdfEnemy.Flags */
#define TDF_ENEMY_FLYING   1
#define TDF_ENEMY_BURNING  2
#define TDF_ENEMY_FROZEN   4
#define TDF_ENEMY_SLOWED   8
#define TDF_ENEMY_CARRIED 16

/* Tipos de comando de tdf_enqueue */
#define TDF_CMD_BUILD   0  /* x, y, arg = tipo da torre */
#define TDF_CMD_UPGRADE 1  /* x, y */
#define TDF_CMD_SELL    2  /* x, y */
#define TDF_CMD_SEND    3  /* arg = id do envio */

/* Tipos de TdfEvent.Kind (significado de a, b, c, f, x, y, z em BridgeTypes.cs, classe BridgeEventKind) */
#define TDF_EV_ENEMY_SPAWNED     1
#define TDF_EV_ENEMY_HIT         2
#define TDF_EV_STATUS_APPLIED    3
#define TDF_EV_BOUNTY_PAID       4
#define TDF_EV_INCOME_TICK       5
#define TDF_EV_SEND_BOUGHT       6
#define TDF_EV_LIFE_LOST         7
#define TDF_EV_COMMAND_REJECTED  8
#define TDF_EV_SUDDEN_DEATH      9
#define TDF_EV_MATCH_ENDED      10
#define TDF_EV_TOWER_FIRED      11
#define TDF_EV_TOWER_CHANGED    12
#define TDF_EV_TOWER_SOLD       13
#define TDF_EV_ENEMY_DESPAWNED  14

typedef struct { int Id, Lives, Gold, Income, TowerCount, EnemyCount;
                 int Dead, InSuddenDeath, TotalLeaked, KilledByTower, KilledByAttrition, TotalSent;
                 float MatchTime, GoalX, GoalY, GoalZ; } TdfLane;                  /* 64 bytes */

typedef struct { int Lane, Slot, Generation, TypeId, Flags;
                 float X, Y, Z, Hp, MaxHp; } TdfEnemy;                              /* 40 bytes */

typedef struct { int Lane, CellX, CellY, TypeId, Level, HasAim;
                 float X, Y, Z, AimX, AimY, AimZ; } TdfTower;                       /* 48 bytes */

typedef struct { int Lane, TowerType, Level, TargetFlies;
                 float X, Y, Z, Progress; } TdfProjectile;                          /* 32 bytes */

typedef struct { int Kind, Lane, A, B, C;
                 float X, Y, Z, F; } TdfEvent;                                      /* 36 bytes */

TDF_API int  tdf_abi_version(void);
TDF_API int  tdf_struct_sizes(int* out5);             /* lane, inimigo, torre, tiro, evento */

/* difficulty: 0 Fácil, 1 Normal, 2 Difícil. Textos de catálogo (envios.txt / torres.txt) podem ser NULL = fábrica. 0 = ok. */
TDF_API int  tdf_create(int seed, int difficulty, int width, int height, const char* sends_text, const char* towers_text);
TDF_API void tdf_destroy(void);
TDF_API int  tdf_last_error(char* buffer, int capacity);   /* devolve o tamanho necessário (sem o zero) */

TDF_API int  tdf_enqueue(int kind, int x, int y, int arg); /* 1 = entrou na fila; vale no próximo tique */
TDF_API int  tdf_step(int ticks);                           /* quantos tiques rodou (para quando a partida acaba) */
TDF_API int  tdf_tick(void);
TDF_API int  tdf_over(void);

TDF_API int  tdf_snapshot(void);                            /* fotografa; depois copie com as funções abaixo */
TDF_API int  tdf_lanes(TdfLane* out2);                      /* 2 lanes: 0 = jogador, 1 = IA */
TDF_API int  tdf_enemies(TdfEnemy* buf, int capacity);      /* devolve quantos EXISTEM (copia até capacity) */
TDF_API int  tdf_towers(TdfTower* buf, int capacity);
TDF_API int  tdf_projectiles(TdfProjectile* buf, int capacity);
TDF_API int  tdf_territory(unsigned char* buf, int capacity); /* 2*largura*altura bytes: lane 0, depois lane 1; 1 = dentro */
TDF_API int  tdf_events(TdfEvent* buf, int capacity);       /* copia e ESVAZIA os pendentes; devolve quantos copiou */

TDF_API int  tdf_fingerprint(char* buffer, int capacity);   /* prova de igualdade da partida (texto) */
TDF_API int  tdf_signature(char* buffer, int capacity);     /* "sends towers rules" (3 blocos de 16 hexa) */

/* Perguntas da vista (U2). Catálogo em texto UTF-8, uma linha por tipo, campos separados por ';' (números em ponto decimal):
   kind 0 = torres: chave;nome;custo;alcance;recarga;dano;raio_de_area;raio_de_fronteira;fator_de_lentidao
   kind 1 = envios: chave;nome;custo;vida;velocidade;quantidade;bonus_de_renda;recompensa;vidas_que_tira
   Campos novos entram sempre no fim da linha. Devolve o tamanho necessário (sem o zero), ou -1 para kind desconhecido. */
TDF_API int  tdf_catalog_text(int kind, char* buffer, int capacity);
/* Por que NÃO dá para construir a torre `type` na célula (x,y) da lane do jogador: 0 = dá; senão o RejectReason (SimEvents.cs). */
TDF_API int  tdf_can_build(int x, int y, int type);
/* Custo de subir a torre da célula (x,y) do jogador: -1 sem torre, 0 no nível máximo. */
TDF_API int  tdf_upgrade_cost(int x, int y);

#ifdef __cplusplus
}
#endif
#endif
