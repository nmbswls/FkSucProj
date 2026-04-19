# 维护 Datas/__beans__.xlsx：与 Luban 官方示例相同表头（*fields 区已合并 J:P），便于以后在表内追加 bean。
# 当前无数据行；需要定义 bean 时在 *fields 区按行追加字段即可。
# 在 Config 目录执行: python gen_luban_beans_xlsx.py
from pathlib import Path

import openpyxl

ROOT = Path(__file__).resolve().parent
OUT = ROOT / "Datas" / "__beans__.xlsx"


def write_beans_workbook():
    wb = openpyxl.Workbook()
    ws = wb.active
    ws.title = "Sheet1"

    # 与 luban_examples DataTables/Datas/__beans__.xlsx 一致（列顺序：valueType 后为 alias、sep）
    # *fields 区须合并单元格 J:P（与官方示例一致），共 7 列对应 __FieldInfo__
    ws.append(
        [
            "##var",
            "full_name",
            "parent",
            "valueType",
            "alias",
            "sep",
            "comment",
            "tags",
            "group",
            "*fields",
            None,
            None,
            None,
            None,
            None,
            None,
        ]
    )
    ws.merge_cells("J1:P1")
    ws.append(
        [
            "##var",
            None,
            None,
            None,
            None,
            None,
            None,
            None,
            None,
            "name",
            "alias",
            "type",
            "group",
            "comment",
            "tags",
            "variants",
        ]
    )
    ws.append(
        [
            "##",
            "full_name",
            "parent",
            "is_value_type",
            "alias",
            "sep",
            "comment",
            "tags",
            "group",
            "field_name",
            "field_alias",
            "field_type",
            "field_group",
            "field_comment",
            "field_tags",
            "field_variants",
        ]
    )

    OUT.parent.mkdir(parents=True, exist_ok=True)
    wb.save(OUT)


if __name__ == "__main__":
    write_beans_workbook()
    print("ok")
