# Optional: regenerate UIBedroomBedAct.prefab YAML for Unity.
# Run from repo root: python tools/gen_uibedroom_bed_act_prefab.py
import os

OUT = os.path.normpath(
    os.path.join(os.path.dirname(__file__), "..", "Assets", "Resources", "UI", "Prefabs", "UIBedroomBedAct.prefab")
)

G = {
    "img": "fe87c0e1cc204ed48ad3b37840f39efc",
    "btn": "4e29b1a8efbd4b44bb3f3716e73f07ff",
    "tmp": "f4688fdb7df04437aeb418b961361dc5",
    "panel": "f1e2d3c4b5a6978877665544332211a",
    "hlg": "30649d3a9faa99c48a7b1166b86bf2a0",
    "vlg": "59f8146938fff824cb5fd77236b75775",
    "le": "306cc8c2b49d7114eaa3623786fc2126",
    "csf": "3245ec927659c4140ac4f8d17403cc18",
    "builtin": "0000000000000000f000000000000000",
}
FONT = "8f586378b4e144a9851e7b34d9b748ee"

# Stable fileIDs for cross-references
ids = {
    "root_go": 3774556582192696012,
    "root_rt": 1213843799512640489,
    "cg": 938470001,
    "panel": 938470002,
    "dim_go": 931000010,
    "dim_rt": 931000011,
    "dim_cr": 931000012,
    "dim_im": 931000013,
    "main_go": 931000020,
    "main_rt": 931000021,
    "main_cr": 931000022,
    "main_im": 931000023,
    "hdr_go": 931000030,
    "hdr_rt": 931000031,
    "hdr_hlg": 931000032,
    "tabH_go": 931000040,
    "tabH_rt": 931000041,
    "tabH_cr": 931000042,
    "tabH_im": 931000043,
    "tabH_btn": 931000044,
    "tabH_txt_go": 931000045,
    "tabH_txt_rt": 931000046,
    "tabH_txt_cr": 931000047,
    "tabH_txt_tmp": 931000048,
    "tabH_le": 931000049,
    "tabD_go": 931000050,
    "tabD_rt": 931000051,
    "tabD_cr": 931000052,
    "tabD_im": 931000053,
    "tabD_btn": 931000054,
    "tabD_txt_go": 931000055,
    "tabD_txt_rt": 931000056,
    "tabD_txt_cr": 931000057,
    "tabD_txt_tmp": 931000058,
    "tabD_le": 931000059,
    "sp_go": 931000060,
    "sp_rt": 931000061,
    "sp_le": 931000062,
    "cls_go": 931000070,
    "cls_rt": 931000071,
    "cls_cr": 931000072,
    "cls_im": 931000073,
    "cls_btn": 931000074,
    "cls_txt_go": 931000075,
    "cls_txt_rt": 931000076,
    "cls_txt_cr": 931000077,
    "cls_txt_tmp": 931000078,
    "cls_le": 931000079,
    "body_go": 931000080,
    "body_rt": 931000081,
    "ph_go": 931000090,
    "ph_rt": 931000091,
    "ph_hlg": 931000092,
    "left_go": 931000100,
    "left_rt": 931000101,
    "left_cr": 931000102,
    "left_im": 931000103,
    "left_le": 931000104,
    "cnt_go": 931000130,
    "cnt_rt": 931000131,
    "cnt_vlg": 931000132,
    "cnt_csf": 931000133,
    "tpl_go": 931000140,
    "tpl_rt": 931000141,
    "tpl_cr": 931000142,
    "tpl_im": 931000143,
    "tpl_btn": 931000144,
    "tpl_le": 931000145,
    "tpl_txt_go": 931000150,
    "tpl_txt_rt": 931000151,
    "tpl_txt_cr": 931000152,
    "tpl_txt_tmp": 931000153,
    "right_go": 931000200,
    "right_rt": 931000201,
    "right_vlg": 931000202,
    "right_le": 931000203,
    "th_go": 931000210,
    "th_rt": 931000211,
    "th_cr": 931000212,
    "th_im": 931000213,
    "th_le": 931000214,
    "ds_go": 931000220,
    "ds_rt": 931000221,
    "ds_cr": 931000222,
    "ds_tmp": 931000223,
    "ds_le": 931000224,
    "tp_go": 931000230,
    "tp_rt": 931000231,
    "tp_cr": 931000232,
    "tp_im": 931000233,
    "tp_btn": 931000234,
    "tp_le": 931000235,
    "tp_txt_go": 931000236,
    "tp_txt_rt": 931000237,
    "tp_txt_cr": 931000238,
    "tp_txt_tmp": 931000239,
    "pd_go": 931000500,
    "pd_rt": 931000501,
    "pd_vlg": 931000502,
    "dh_go": 931000510,
    "dh_rt": 931000511,
    "dh_cr": 931000512,
    "dh_tmp": 931000513,
    "bd_go": 931000520,
    "bd_rt": 931000521,
    "bd_cr": 931000522,
    "bd_im": 931000523,
    "bd_btn": 931000524,
    "bd_le": 931000525,
    "bd_txt_go": 931000526,
    "bd_txt_rt": 931000527,
    "bd_txt_cr": 931000528,
    "bd_txt_tmp": 931000529,
}
i = ids


def img_block(fid, go, color, sprite_line, raycast=1):
    return f"""--- !u!114 &{fid}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {G["img"]}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Material: {{fileID: 0}}
  m_Color: {color}
  m_RaycastTarget: {raycast}
  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  {sprite_line}
  m_Type: 1
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
"""


def btn_block(fid, go, target_graphic, interactable=1):
    return f"""--- !u!114 &{fid}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {G["btn"]}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Navigation:
    m_Mode: 3
    m_WrapAround: 0
    m_SelectOnUp: {{fileID: 0}}
    m_SelectOnDown: {{fileID: 0}}
    m_SelectOnLeft: {{fileID: 0}}
    m_SelectOnRight: {{fileID: 0}}
  m_Transition: 1
  m_Colors:
    m_NormalColor: {{r: 1, g: 1, b: 1, a: 1}}
    m_HighlightedColor: {{r: 0.96, g: 0.96, b: 0.96, a: 1}}
    m_PressedColor: {{r: 0.78, g: 0.78, b: 0.78, a: 1}}
    m_SelectedColor: {{r: 0.96, g: 0.96, b: 0.96, a: 1}}
    m_DisabledColor: {{r: 0.78, g: 0.78, b: 0.78, a: 0.5}}
    m_ColorMultiplier: 1
    m_FadeDuration: 0.1
  m_SpriteState:
    m_HighlightedSprite: {{fileID: 0}}
    m_PressedSprite: {{fileID: 0}}
    m_SelectedSprite: {{fileID: 0}}
    m_DisabledSprite: {{fileID: 0}}
  m_AnimationTriggers:
    m_NormalTrigger: Normal
    m_HighlightedTrigger: Highlighted
    m_PressedTrigger: Pressed
    m_SelectedTrigger: Selected
    m_DisabledTrigger: Disabled
  m_Interactable: {interactable}
  m_TargetGraphic: {{fileID: {target_graphic}}}
  m_OnClick:
    m_PersistentCalls:
      m_Calls: []
"""


def tmp_block(fid, go, text, font_size, h_align, v_align):
    return f"""--- !u!114 &{fid}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {G["tmp"]}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Material: {{fileID: 0}}
  m_Color: {{r: 1, g: 1, b: 1, a: 1}}
  m_RaycastTarget: 1
  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_text: {text}
  m_isRightToLeft: 0
  m_fontAsset: {{fileID: 11400000, guid: {FONT}, type: 2}}
  m_sharedMaterial: {{fileID: 2180264, guid: {FONT}, type: 2}}
  m_fontSharedMaterials: []
  m_fontMaterial: {{fileID: 0}}
  m_fontMaterials: []
  m_fontColor32:
    serializedVersion: 2
    rgba: 4294967295
  m_fontColor: {{r: 1, g: 1, b: 1, a: 1}}
  m_enableVertexGradient: 0
  m_colorMode: 3
  m_fontColorGradient:
    topLeft: {{r: 1, g: 1, b: 1, a: 1}}
    topRight: {{r: 1, g: 1, b: 1, a: 1}}
    bottomLeft: {{r: 1, g: 1, b: 1, a: 1}}
    bottomRight: {{r: 1, g: 1, b: 1, a: 1}}
  m_fontColorGradientPreset: {{fileID: 0}}
  m_spriteAsset: {{fileID: 0}}
  m_tintAllSprites: 0
  m_StyleSheet: {{fileID: 0}}
  m_TextStyleHashCode: -1183493901
  m_overrideHtmlColors: 0
  m_faceColor:
    serializedVersion: 2
    rgba: 4294967295
  m_fontSize: {font_size}
  m_fontSizeBase: {font_size}
  m_fontWeight: 400
  m_enableAutoSizing: 0
  m_fontSizeMin: 18
  m_fontSizeMax: 72
  m_fontStyle: 0
  m_HorizontalAlignment: {h_align}
  m_VerticalAlignment: {v_align}
  m_textAlignment: 65535
  m_characterSpacing: 0
  m_wordSpacing: 0
  m_lineSpacing: 0
  m_lineSpacingMax: 0
  m_paragraphSpacing: 0
  m_charWidthMaxAdj: 0
  m_enableWordWrapping: 1
  m_wordWrappingRatios: 0.4
  m_overflowMode: 0
  m_linkedTextComponent: {{fileID: 0}}
  parentLinkedComponent: {{fileID: 0}}
  m_enableKerning: 1
  m_enableExtraPadding: 0
  checkPaddingRequired: 0
  m_isRichText: 1
  m_parseCtrlCharacters: 1
  m_isOrthographic: 1
  m_isCullingEnabled: 0
  m_horizontalMapping: 0
  m_verticalMapping: 0
  m_uvLineOffset: 0
  m_geometrySortingOrder: 0
  m_IsTextObjectScaleStatic: 0
  m_VertexBufferAutoSizeReduction: 0
  m_useMaxVisibleDescender: 1
  m_pageToDisplay: 1
  m_margin: {{x: 0, y: 0, z: 0, w: 0}}
  m_isUsingLegacyAnimationComponent: 0
  m_isVolumetricText: 0
  m_hasFontAssetChanged: 0
  m_baseMaterial: {{fileID: 0}}
  m_maskOffset: {{x: 0, y: 0, z: 0, w: 0}}
"""


def rt_block(fid, go, father, pack, children=None):
    if children is None:
        children = []
    ch = "\n".join(f"  - {{fileID: {c}}}" for c in children) if children else "  []"
    amin, amax, apos, sd = pack
    return f"""--- !u!224 &{fid}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:
{ch}
  m_Father: {{fileID: {father}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: {amin[0]}, y: {amin[1]}}}
  m_AnchorMax: {{x: {amax[0]}, y: {amax[1]}}}
  m_AnchoredPosition: {{x: {apos[0]}, y: {apos[1]}}}
  m_SizeDelta: {{x: {sd[0]}, y: {sd[1]}}}
  m_Pivot: {{x: 0.5, y: 0.5}}
"""


def go_block(fid, name, comps, layer=5, active=1):
    cl = "\n".join(f"  - component: {{fileID: {c}}}" for c in comps)
    return f"""--- !u!1 &{fid}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
{cl}
  m_Layer: {layer}
  m_Name: {name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: {active}
"""


def cr_block(fid, go):
    return f"""--- !u!222 &{fid}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_CullTransparentMesh: 1
"""


def cg_block(fid, go):
    return f"""--- !u!225 &{fid}
CanvasGroup:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_Alpha: 1
  m_Interactable: 1
  m_BlocksRaycasts: 1
  m_IgnoreParentGroups: 0
"""


def hlg_block(fid, go, spacing, pad, cfw, cfh, ccw, cch):
    return f"""--- !u!114 &{fid}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {G["hlg"]}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Padding:
    m_Left: {pad[0]}
    m_Right: {pad[1]}
    m_Top: {pad[2]}
    m_Bottom: {pad[3]}
  m_ChildAlignment: 3
  m_Spacing: {spacing}
  m_ChildForceExpandWidth: {cfw}
  m_ChildForceExpandHeight: {cfh}
  m_ChildControlWidth: {ccw}
  m_ChildControlHeight: {cch}
  m_ChildScaleWidth: 0
  m_ChildScaleHeight: 0
  m_ReverseArrangement: 0
"""


def vlg_block(fid, go, spacing, pad, align, cfw, cfh, ccw, cch):
    return f"""--- !u!114 &{fid}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {G["vlg"]}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Padding:
    m_Left: {pad[0]}
    m_Right: {pad[1]}
    m_Top: {pad[2]}
    m_Bottom: {pad[3]}
  m_ChildAlignment: {align}
  m_Spacing: {spacing}
  m_ChildForceExpandWidth: {cfw}
  m_ChildForceExpandHeight: {cfh}
  m_ChildControlWidth: {ccw}
  m_ChildControlHeight: {cch}
  m_ChildScaleWidth: 0
  m_ChildScaleHeight: 0
  m_ReverseArrangement: 0
"""


def le_block(fid, go, pref_w, pref_h, flex_w, flex_h):
    pw = pref_w if pref_w is not None else -1
    ph = pref_h if pref_h is not None else -1
    fw = flex_w if flex_w is not None else -1
    fh = flex_h if flex_h is not None else -1
    return f"""--- !u!114 &{fid}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {G["le"]}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_IgnoreLayout: 0
  m_MinWidth: -1
  m_MinHeight: -1
  m_PreferredWidth: {pw}
  m_PreferredHeight: {ph}
  m_FlexibleWidth: {fw}
  m_FlexibleHeight: {fh}
  m_LayoutPriority: 1
"""


def csf_block(fid, go, hf, vf):
    return f"""--- !u!114 &{fid}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {G["csf"]}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_HorizontalFit: {hf}
  m_VerticalFit: {vf}
"""


builtin_sprite = 'm_Sprite: {fileID: 10905, guid: 0000000000000000f000000000000000, type: 0}'

parts = []
parts.append("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n")

# Root
parts.append(
    go_block(
        i["root_go"],
        "UIBedroomBedAct",
        [i["root_rt"], i["cg"], i["panel"]],
    )
)
parts.append(
    rt_block(
        i["root_rt"],
        i["root_go"],
        0,
        ((0, 0), (1, 1), (0, 0), (0, 0)),
        [i["dim_rt"], i["main_rt"]],
    )
)
parts.append(cg_block(i["cg"], i["root_go"]))
parts.append(
    f"""--- !u!114 &{i["panel"]}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {i["root_go"]}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {G["panel"]}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  panelId: UIBedroomBedAct
  canvasGroup: {{fileID: {i["cg"]}}}
  pageHunt: {{fileID: {i["ph_rt"]}}}
  pageDream: {{fileID: {i["pd_rt"]}}}
  tabHunt: {{fileID: {i["tabH_btn"]}}}
  tabDream: {{fileID: {i["tabD_btn"]}}}
  mapListContent: {{fileID: {i["cnt_rt"]}}}
  mapRowTemplate: {{fileID: {i["tpl_btn"]}}}
  detailThumb: {{fileID: {i["th_im"]}}}
  detailDesc: {{fileID: {i["ds_tmp"]}}}
  btnTeleport: {{fileID: {i["tp_btn"]}}}
  btnDream: {{fileID: {i["bd_btn"]}}}
  btnClose: {{fileID: {i["cls_btn"]}}}
"""
)

# Dim
parts.append(go_block(i["dim_go"], "Dim", [i["dim_rt"], i["dim_cr"], i["dim_im"]]))
parts.append(rt_block(i["dim_rt"], i["dim_go"], i["root_rt"], ((0, 0), (1, 1), (0, 0), (0, 0)), []))
parts.append(cr_block(i["dim_cr"], i["dim_go"]))
parts.append(
    img_block(
        i["dim_im"],
        i["dim_go"],
        "{r: 0, g: 0, b: 0, a: 0.5}",
        builtin_sprite,
        1,
    )
)

# Main card
parts.append(go_block(i["main_go"], "Main", [i["main_rt"], i["main_cr"], i["main_im"]]))
parts.append(
    rt_block(
        i["main_rt"],
        i["main_go"],
        i["root_rt"],
        ((0.5, 0.5), (0.5, 0.5), (0, 0), (900, 520)),
        [i["hdr_rt"], i["body_rt"]],
    )
)
parts.append(cr_block(i["main_cr"], i["main_go"]))
parts.append(
    img_block(
        i["main_im"],
        i["main_go"],
        "{r: 0.12, g: 0.13, b: 0.16, a: 0.98}",
        builtin_sprite,
        1,
    )
)

# Header
parts.append(go_block(i["hdr_go"], "Header", [i["hdr_rt"], i["hdr_hlg"]]))
parts.append(
    rt_block(
        i["hdr_rt"],
        i["hdr_go"],
        i["main_rt"],
        ((0, 1), (1, 1), (0, -4), (0, 48)),
        [i["tabH_rt"], i["tabD_rt"], i["sp_rt"], i["cls_rt"]],
    )
)
parts.append(hlg_block(i["hdr_hlg"], i["hdr_go"], 8, (12, 12, 6, 6), 0, 1, 1, 1))

# Tab hunt
parts.append(
    go_block(
        i["tabH_go"],
        "TabHunt",
        [i["tabH_rt"], i["tabH_cr"], i["tabH_im"], i["tabH_btn"], i["tabH_le"]],
    )
)
parts.append(
    rt_block(
        i["tabH_rt"],
        i["tabH_go"],
        i["hdr_rt"],
        ((0, 0), (0, 0), (0, 0), (120, 40)),
        [i["tabH_txt_rt"]],
    )
)
parts.append(cr_block(i["tabH_cr"], i["tabH_go"]))
parts.append(le_block(i["tabH_le"], i["tabH_go"], 120, 40, -1, -1))
parts.append(
    img_block(
        i["tabH_im"],
        i["tabH_go"],
        "{r: 0.22, g: 0.24, b: 0.3, a: 1}",
        builtin_sprite,
        1,
    )
)
parts.append(btn_block(i["tabH_btn"], i["tabH_go"], i["tabH_im"], 1))
parts.append(go_block(i["tabH_txt_go"], "Text", [i["tabH_txt_rt"], i["tabH_txt_cr"], i["tabH_txt_tmp"]]))
parts.append(
    rt_block(
        i["tabH_txt_rt"],
        i["tabH_txt_go"],
        i["tabH_rt"],
        ((0, 0), (1, 1), (0, 0), (0, 0)),
        [],
    )
)
parts.append(cr_block(i["tabH_txt_cr"], i["tabH_txt_go"]))
parts.append(tmp_block(i["tabH_txt_tmp"], i["tabH_txt_go"], '"\\u730e\\u7329"', 18, 2, 512))

# Tab dream
parts.append(
    go_block(
        i["tabD_go"],
        "TabDream",
        [i["tabD_rt"], i["tabD_cr"], i["tabD_im"], i["tabD_btn"], i["tabD_le"]],
    )
)
parts.append(
    rt_block(
        i["tabD_rt"],
        i["tabD_go"],
        i["hdr_rt"],
        ((0, 0), (0, 0), (0, 0), (120, 40)),
        [i["tabD_txt_rt"]],
    )
)
parts.append(cr_block(i["tabD_cr"], i["tabD_go"]))
parts.append(le_block(i["tabD_le"], i["tabD_go"], 120, 40, -1, -1))
parts.append(
    img_block(
        i["tabD_im"],
        i["tabD_go"],
        "{r: 0.22, g: 0.24, b: 0.3, a: 1}",
        builtin_sprite,
        1,
    )
)
parts.append(btn_block(i["tabD_btn"], i["tabD_go"], i["tabD_im"], 1))
parts.append(go_block(i["tabD_txt_go"], "Text", [i["tabD_txt_rt"], i["tabD_txt_cr"], i["tabD_txt_tmp"]]))
parts.append(
    rt_block(
        i["tabD_txt_rt"],
        i["tabD_txt_go"],
        i["tabD_rt"],
        ((0, 0), (1, 1), (0, 0), (0, 0)),
        [],
    )
)
parts.append(cr_block(i["tabD_txt_cr"], i["tabD_txt_go"]))
parts.append(tmp_block(i["tabD_txt_tmp"], i["tabD_txt_go"], '"\\u5165\\u68a6"', 18, 2, 512))

# Spacer
parts.append(go_block(i["sp_go"], "Spacer", [i["sp_rt"], i["sp_le"]]))
parts.append(
    rt_block(
        i["sp_rt"],
        i["sp_go"],
        i["hdr_rt"],
        ((0, 0), (0, 0), (0, 0), (10, 10)),
        [],
    )
)
parts.append(le_block(i["sp_le"], i["sp_go"], 10, 10, 1, -1))

# Close
parts.append(
    go_block(
        i["cls_go"],
        "BtnClose",
        [i["cls_rt"], i["cls_cr"], i["cls_im"], i["cls_btn"], i["cls_le"]],
    )
)
parts.append(
    rt_block(
        i["cls_rt"],
        i["cls_go"],
        i["hdr_rt"],
        ((0, 0), (0, 0), (0, 0), (88, 36)),
        [i["cls_txt_rt"]],
    )
)
parts.append(cr_block(i["cls_cr"], i["cls_go"]))
parts.append(le_block(i["cls_le"], i["cls_go"], 88, 36, -1, -1))
parts.append(
    img_block(
        i["cls_im"],
        i["cls_go"],
        "{r: 0.22, g: 0.24, b: 0.3, a: 1}",
        builtin_sprite,
        1,
    )
)
parts.append(btn_block(i["cls_btn"], i["cls_go"], i["cls_im"], 1))
parts.append(go_block(i["cls_txt_go"], "Text", [i["cls_txt_rt"], i["cls_txt_cr"], i["cls_txt_tmp"]]))
parts.append(
    rt_block(
        i["cls_txt_rt"],
        i["cls_txt_go"],
        i["cls_rt"],
        ((0, 0), (1, 1), (0, 0), (0, 0)),
        [],
    )
)
parts.append(cr_block(i["cls_txt_cr"], i["cls_txt_go"]))
parts.append(tmp_block(i["cls_txt_tmp"], i["cls_txt_go"], '"\\u5173\\u95ed"', 18, 2, 512))

# Body
parts.append(go_block(i["body_go"], "Body", [i["body_rt"]]))
parts.append(
    rt_block(
        i["body_rt"],
        i["body_go"],
        i["main_rt"],
        ((0, 0), (1, 1), (0, 0), (-24, -64)),
        [i["ph_rt"], i["pd_rt"]],
    )
)

# PageHunt
parts.append(go_block(i["ph_go"], "PageHunt", [i["ph_rt"], i["ph_hlg"]]))
parts.append(
    rt_block(
        i["ph_rt"],
        i["ph_go"],
        i["body_rt"],
        ((0, 0), (1, 1), (0, 0), (0, 0)),
        [i["left_rt"], i["right_rt"]],
    )
)
parts.append(hlg_block(i["ph_hlg"], i["ph_go"], 12, (0, 0, 0, 0), 0, 1, 1, 1))

# Left + list
parts.append(
    go_block(
        i["left_go"],
        "Left",
        [i["left_rt"], i["left_cr"], i["left_im"], i["left_le"]],
    )
)
parts.append(
    rt_block(
        i["left_rt"],
        i["left_go"],
        i["ph_rt"],
        ((0, 0), (0, 1), (0, 0), (260, 0)),
        [i["cnt_rt"]],
    )
)
parts.append(cr_block(i["left_cr"], i["left_go"]))
parts.append(
    img_block(
        i["left_im"],
        i["left_go"],
        "{r: 0.08, g: 0.09, b: 0.11, a: 1}",
        builtin_sprite,
        1,
    )
)
parts.append(le_block(i["left_le"], i["left_go"], 260, -1, 0, 1))

parts.append(
    go_block(
        i["cnt_go"],
        "MapListContent",
        [i["cnt_rt"], i["cnt_vlg"], i["cnt_csf"]],
    )
)
parts.append(
    rt_block(
        i["cnt_rt"],
        i["cnt_go"],
        i["left_rt"],
        ((0, 0), (1, 1), (0, 0), (-8, -8)),
        [i["tpl_rt"]],
    )
)
parts.append(vlg_block(i["cnt_vlg"], i["cnt_go"], 4, (6, 6, 6, 6), 1, 1, 0, 1, 1))
parts.append(csf_block(i["cnt_csf"], i["cnt_go"], 0, 2))

# Row template (inactive)
parts.append(
    go_block(
        i["tpl_go"],
        "MapRowTemplate",
        [i["tpl_rt"], i["tpl_cr"], i["tpl_im"], i["tpl_btn"], i["tpl_le"]],
        active=0,
    )
)
parts.append(
    rt_block(
        i["tpl_rt"],
        i["tpl_go"],
        i["cnt_rt"],
        ((0, 0), (1, 0), (0, 0), (0, 40)),
        [i["tpl_txt_rt"]],
    )
)
parts.append(cr_block(i["tpl_cr"], i["tpl_go"]))
parts.append(
    img_block(
        i["tpl_im"],
        i["tpl_go"],
        "{r: 0.2, g: 0.22, b: 0.28, a: 1}",
        builtin_sprite,
        1,
    )
)
parts.append(btn_block(i["tpl_btn"], i["tpl_go"], i["tpl_im"], 1))
parts.append(le_block(i["tpl_le"], i["tpl_go"], -1, 40, 1, -1))
parts.append(go_block(i["tpl_txt_go"], "Text", [i["tpl_txt_rt"], i["tpl_txt_cr"], i["tpl_txt_tmp"]]))
parts.append(
    rt_block(
        i["tpl_txt_rt"],
        i["tpl_txt_go"],
        i["tpl_rt"],
        ((0, 0), (1, 1), (0, 0), (-16, 0)),
        [],
    )
)
parts.append(cr_block(i["tpl_txt_cr"], i["tpl_txt_go"]))
parts.append(tmp_block(i["tpl_txt_tmp"], i["tpl_txt_go"], '"\\u6a21\\u677f"', 17, 1, 512))

# Right column
parts.append(
    go_block(
        i["right_go"],
        "Right",
        [i["right_rt"], i["right_vlg"], i["right_le"]],
    )
)
parts.append(
    rt_block(
        i["right_rt"],
        i["right_go"],
        i["ph_rt"],
        ((0, 0), (1, 1), (0, 0), (0, 0)),
        [i["th_rt"], i["ds_rt"], i["tp_rt"]],
    )
)
parts.append(vlg_block(i["right_vlg"], i["right_go"], 10, (8, 8, 8, 8), 0, 1, 0, 1, 1))
parts.append(le_block(i["right_le"], i["right_go"], -1, -1, 1, 1))

# Thumb
parts.append(go_block(i["th_go"], "Thumb", [i["th_rt"], i["th_cr"], i["th_im"], i["th_le"]]))
parts.append(
    rt_block(
        i["th_rt"],
        i["th_go"],
        i["right_rt"],
        ((0, 0), (0, 0), (0, 0), (0, 200)),
        [],
    )
)
parts.append(cr_block(i["th_cr"], i["th_go"]))
parts.append(
    img_block(
        i["th_im"],
        i["th_go"],
        "{r: 0.18, g: 0.19, b: 0.22, a: 1}",
        builtin_sprite,
        1,
    )
)
parts.append(le_block(i["th_le"], i["th_go"], -1, 200, 1, 0))

# Desc
parts.append(
    go_block(
        i["ds_go"],
        "Desc",
        [i["ds_rt"], i["ds_cr"], i["ds_tmp"], i["ds_le"]],
    )
)
parts.append(
    rt_block(
        i["ds_rt"],
        i["ds_go"],
        i["right_rt"],
        ((0, 0), (0, 0), (0, 0), (0, 120)),
        [],
    )
)
parts.append(cr_block(i["ds_cr"], i["ds_go"]))
parts.append(tmp_block(i["ds_tmp"], i["ds_go"], '""', 18, 1, 256))
parts.append(le_block(i["ds_le"], i["ds_go"], -1, 120, 1, 1))

# Teleport
parts.append(
    go_block(
        i["tp_go"],
        "BtnTeleport",
        [i["tp_rt"], i["tp_cr"], i["tp_im"], i["tp_btn"], i["tp_le"]],
    )
)
parts.append(
    rt_block(
        i["tp_rt"],
        i["tp_go"],
        i["right_rt"],
        ((0, 0), (0, 0), (0, 0), (0, 40)),
        [i["tp_txt_rt"]],
    )
)
parts.append(cr_block(i["tp_cr"], i["tp_go"]))
parts.append(
    img_block(
        i["tp_im"],
        i["tp_go"],
        "{r: 0.22, g: 0.24, b: 0.3, a: 1}",
        builtin_sprite,
        1,
    )
)
parts.append(btn_block(i["tp_btn"], i["tp_go"], i["tp_im"], 1))
parts.append(le_block(i["tp_le"], i["tp_go"], -1, 40, 1, 0))
parts.append(go_block(i["tp_txt_go"], "Text", [i["tp_txt_rt"], i["tp_txt_cr"], i["tp_txt_tmp"]]))
parts.append(
    rt_block(
        i["tp_txt_rt"],
        i["tp_txt_go"],
        i["tp_rt"],
        ((0, 0), (1, 1), (0, 0), (0, 0)),
        [],
    )
)
parts.append(cr_block(i["tp_txt_cr"], i["tp_txt_go"]))
parts.append(tmp_block(i["tp_txt_tmp"], i["tp_txt_go"], '"\\u4f20\\u9001"', 18, 2, 512))

# PageDream
parts.append(
    go_block(
        i["pd_go"],
        "PageDream",
        [i["pd_rt"], i["pd_vlg"]],
        active=0,
    )
)
parts.append(
    rt_block(
        i["pd_rt"],
        i["pd_go"],
        i["body_rt"],
        ((0, 0), (1, 1), (0, 0), (0, 0)),
        [i["dh_rt"], i["bd_rt"]],
    )
)
parts.append(vlg_block(i["pd_vlg"], i["pd_go"], 16, (24, 24, 24, 24), 4, 1, 0, 1, 0))

parts.append(go_block(i["dh_go"], "DreamHint", [i["dh_rt"], i["dh_cr"], i["dh_tmp"]]))
parts.append(
    rt_block(
        i["dh_rt"],
        i["dh_go"],
        i["pd_rt"],
        ((0, 0), (1, 0), (0, 0), (0, 48)),
        [],
    )
)
parts.append(cr_block(i["dh_cr"], i["dh_go"]))
parts.append(
    tmp_block(
        i["dh_tmp"],
        i["dh_go"],
        '"\\u8fdb\\u5165\\u5165\\u68a6\\u5c0f\\u6e38\\u620f\\u3002"',
        20,
        2,
        512,
    )
)

parts.append(
    go_block(
        i["bd_go"],
        "BtnDream",
        [i["bd_rt"], i["bd_cr"], i["bd_im"], i["bd_btn"], i["bd_le"]],
    )
)
parts.append(
    rt_block(
        i["bd_rt"],
        i["bd_go"],
        i["pd_rt"],
        ((0, 0), (0, 0), (0, 0), (220, 44)),
        [i["bd_txt_rt"]],
    )
)
parts.append(cr_block(i["bd_cr"], i["bd_go"]))
parts.append(
    img_block(
        i["bd_im"],
        i["bd_go"],
        "{r: 0.22, g: 0.24, b: 0.3, a: 1}",
        builtin_sprite,
        1,
    )
)
parts.append(btn_block(i["bd_btn"], i["bd_go"], i["bd_im"], 1))
parts.append(le_block(i["bd_le"], i["bd_go"], 220, 44, -1, -1))
parts.append(go_block(i["bd_txt_go"], "Text", [i["bd_txt_rt"], i["bd_txt_cr"], i["bd_txt_tmp"]]))
parts.append(
    rt_block(
        i["bd_txt_rt"],
        i["bd_txt_go"],
        i["bd_rt"],
        ((0, 0), (1, 1), (0, 0), (0, 0)),
        [],
    )
)
parts.append(cr_block(i["bd_txt_cr"], i["bd_txt_go"]))
parts.append(
    tmp_block(
        i["bd_txt_tmp"],
        i["bd_txt_go"],
        '"\\u8fdb\\u5165\\u68a6\\u5883"',
        18,
        2,
        512,
    )
)

text = "".join(parts)
os.makedirs(os.path.dirname(OUT), exist_ok=True)
with open(OUT, "w", encoding="utf-8", newline="\n") as f:
    f.write(text)
print("Wrote", OUT)
