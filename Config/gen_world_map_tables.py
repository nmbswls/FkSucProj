# 大地图：world_map_global（singleton）+ world_map_big_map，均在 map.xlsx。
# Config 目录: python gen_world_map_tables.py 后执行 gen.bat / Luban。
# 会删除 sheet：world_map_config、world_map_mini_map（若存在）；勿在有未备份策划数据时随意全量运行。
from pathlib import Path

import openpyxl

ROOT = Path(__file__).resolve().parent
DATAS = ROOT / "Datas"
MAP_XLSX = DATAS / "map.xlsx"
TABLES_XLSX = DATAS / "__tables__.xlsx"

REMOVE_SHEETS = ("world_map_config", "world_map_mini_map")
REMOVE_TABLE_ROWS = ("demo.TbWorldMapConfig", "demo.TbWorldMapMiniMapLayer")


def _append_meta(ws, var_row, type_row, group_row, note_row):
    ws.append(["##var"] + var_row)
    ws.append(["##type"] + type_row)
    ws.append(["##group"] + group_row)
    ws.append(["##"] + note_row)


def write_sheets():
    wb = openpyxl.load_workbook(MAP_XLSX)
    for name in REMOVE_SHEETS:
        if name in wb.sheetnames:
            del wb[name]

    # --- global (mode=one): 单行 ---
    gname = "world_map_global"
    if gname in wb.sheetnames:
        del wb[gname]
    wg = wb.create_sheet(gname)
    gcols = [
        "allow_open_when_area_unknown",
        "fallback_big_map_texture_resource_path",
        "fallback_world_min_x",
        "fallback_world_min_y",
        "fallback_world_max_x",
        "fallback_world_max_y",
        "global_npc_boss_landmark_cfg_ids",
        "global_interact_landmark_cfg_ids",
    ]
    gtypes = ["bool", "string", "float", "float", "float", "float", "string", "string"]
    ggroups = [None, "c,s", "c", "c", "c", "c", "c", "c"]
    gnotes = ["未知地图是否可开", "fallback 底图", "边界", "", "", "", "逗号分隔", "逗号分隔"]
    _append_meta(wg, gcols, gtypes, ggroups, gnotes)
    wg.append([None, True, "WorldMap/fake_map_fallback", -40, -40, 40, 40, "", ""])

    # --- big map layers ---
    bname = "world_map_big_map"
    if bname in wb.sheetnames:
        del wb[bname]
    wb_map = wb.create_sheet(bname)
    bcols = [
        "id",
        "map_id",
        "region_key",
        "room_id",
        "rule_priority",
        "forbid_open_world_map",
        "big_map_texture_resource_path",
        "world_min_x",
        "world_min_y",
        "world_max_x",
        "world_max_y",
    ]
    btypes = ["int", "string", "string", "string", "int", "bool", "string", "float", "float", "float", "float"]
    bgroups = [None, "c,s", "c", "c", "c", "c", "c", "c", "c", "c", "c"]
    bnotes = ["主键", "MapName", "策划分区名", "空/* 默认", "大优先", "禁止打开", "底图路径", "边界", "", "", ""]
    _append_meta(wb_map, bcols, btypes, bgroups, bnotes)
    big_rows = [
        (1, "base_01", "default", "", 0, False, "WorldMap/fake_map_base_01", -50, -50, 50, 50),
        (2, "village_01", "default", "", 0, False, "WorldMap/fake_map_village_01", -50, -50, 50, 50),
        (3, "game_init", "default", "", 0, False, "WorldMap/fake_map_game_init", -50, -50, 50, 50),
    ]
    for r in big_rows:
        wb_map.append([None, *r])

    wb.save(MAP_XLSX)


def patch_tables():
    wb = openpyxl.load_workbook(TABLES_XLSX)
    ws = wb.active

    for r in range(ws.max_row, 3, -1):
        v = ws.cell(r, 2).value
        if v is not None and str(v) in REMOVE_TABLE_ROWS:
            ws.delete_rows(r)

    want = [
        ("demo.TbWorldMapGlobal", "demo.WorldMapGlobal", True, "world_map_global@map.xlsx", None, "one"),
        ("demo.TbWorldMapBigMapLayer", "demo.WorldMapBigMapLayer", True, "world_map_big_map@map.xlsx", None, "list"),
    ]

    existing = {str(ws.cell(r, 2).value) for r in range(4, ws.max_row + 1) if ws.cell(r, 2).value}

    for full_name, value_type, read_schema, inp, index, mode in want:
        if full_name in existing:
            for r in range(4, ws.max_row + 1):
                if str(ws.cell(r, 2).value) == full_name:
                    ws.cell(r, 3, value_type)
                    ws.cell(r, 4, read_schema)
                    ws.cell(r, 5, inp)
                    ws.cell(r, 6, index)
                    ws.cell(r, 7, mode)
            continue
        nr = ws.max_row + 1
        ws.cell(nr, 1, None)
        ws.cell(nr, 2, full_name)
        ws.cell(nr, 3, value_type)
        ws.cell(nr, 4, read_schema)
        ws.cell(nr, 5, inp)
        ws.cell(nr, 6, index)
        ws.cell(nr, 7, mode)

    wb.save(TABLES_XLSX)


if __name__ == "__main__":
    write_sheets()
    patch_tables()
    print("ok")
