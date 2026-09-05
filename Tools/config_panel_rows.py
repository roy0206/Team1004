# -*- coding: utf-8 -*-
import hashlib
import io
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8")

PREFAB = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                      "Team1004", "Assets", "GameAssets", "UI", "SettingsPanel.prefab")

GUID_TEXT = "5f7201a12d95ffc409449d95f23cf332"
GUID_IMAGE = "fe87c0e1cc204ed48ad3b37840f39efc"
GUID_BUTTON = "4e29b1a8efbd4b44bb3f3716e73f07ff"
GUID_INPUTFIELD = "d199490a83bb2b844b9695cbf13b01ef"
GUID_TOGGLE = "9085046f02f69544eb97fd06b6048fe2"
GUID_SCROLLRECT = "1aa08ab6e0800fa44ae55d278d1423e3"
GUID_MASK = "31a19414c41e5ae4aae2af33fee712f6"
GUID_VLG = "59f8146938fff824cb5fd77236b75775"
GUID_CSF = "3245ec927659c4140ac4f8d17403cc18"
GUID_CONFIG_PANEL = "ef31d6dff6041b147bbbacada6fe7a94"
GUID_CONFIG_FIELD_ROW = "332462c646ca4b319c7a9c344ac1d0c3"

FONT = "{fileID: 12800000, guid: 743451263bb3aa44b84a9ed9ebc76680, type: 3}"
SPRITE_SQUARE = "{fileID: 7482667652216324306, guid: 52cd25d660d66fa4f883743b6f3c5e9e, type: 3}"
SPRITE_BUILTIN = "{fileID: 10905, guid: 0000000000000000f000000000000000, type: 0}"

WINDOW_W, WINDOW_H = 1100, 800
TITLE_Y, TITLE_W, TITLE_H = 340, 1000, 60
LIST_Y, LIST_W, LIST_H = 10, 1040, 560
STATUS_Y, STATUS_W, STATUS_H = -305, 1040, 36
BUTTON_Y, BUTTON_W, BUTTON_H = -360, 300, 64
BUTTON_X = {"ApplyButton": -330, "ResetButton": 0, "CloseButton": 330}

ROW_H, HEADER_H = 44, 40
LABEL_W, HINT_W, HINT_X = 420, 290, 428
EDITOR_W, EDITOR_H = 260, 36
PAD_L, PAD_R, PAD_T, PAD_B = 20, 20, 12, 12
SPACING = 6

C_VIEWPORT = (0.05, 0.09, 0.12, 0.9)
C_HEADER = (0.4, 0.88, 0.7, 1)
C_LABEL = (0.92, 0.95, 0.97, 1)
C_HINT = (0.5, 0.58, 0.64, 1)
C_FIELD_BG = (0.95, 0.96, 0.97, 1)
C_FIELD_TEXT = (0.1, 0.12, 0.15, 1)
C_PLACEHOLDER = (0.5, 0.5, 0.5, 1)
C_CHECK = (0.13, 0.5, 0.38, 1)

FLOAT, INT, BOOL, ARRAY = 0, 1, 2, 3

SECTIONS = [
    ("이동", [
        ("laneY", "레인 높이(위·중·아래)", ARRAY),
        ("playerX", "연어 x 위치", FLOAT),
        ("laneMoveDuration", "레인 이동 시간(초)", FLOAT),
        ("scrollSpeed", "스크롤 속도", FLOAT),
    ]),
    ("점프", [
        ("jumpDuration", "점프 지속(초)", FLOAT),
        ("jumpCooldown", "점프 쿨타임(초)", FLOAT),
        ("jumpHeight", "점프 높이", FLOAT),
        ("waterSurfaceY", "수면 높이", FLOAT),
    ]),
    ("구간", [
        ("sectionDurations", "구간 시간(1~4, 초)", ARRAY),
        ("spawnSeed", "스폰 시드(0=매번 랜덤)", INT),
        ("hitStopDuration", "피격 정지(초)", FLOAT),
        ("hitPushDistance", "피격 밀림 거리", FLOAT),
    ]),
    ("보스", [
        ("bossDuration", "보스 시간(초)", FLOAT),
        ("bossTelegraphDuration", "보스 예고(초)", FLOAT),
        ("bossFullLaneTelegraphDuration", "전체 레인 예고(초)", FLOAT),
        ("bossAttackDuration", "보스 공격(초)", FLOAT),
        ("bossRecoveryDuration", "보스 휴식(초)", FLOAT),
        ("bossLaneBandWidth", "보스 판정 띠 폭", FLOAT),
        ("bossLaneBandHeight", "보스 판정 띠 높이", FLOAT),
        ("bossBannerDuration", "보스 배너(초)", FLOAT),
        ("bossClearBannerDuration", "클리어 배너(초)", FLOAT),
    ]),
    ("표시", [
        ("playerScale", "연어 크기 배율", FLOAT),
        ("playerHitboxScale", "연어 판정 배율", FLOAT),
        ("controlHintDuration", "조작 안내 시간(초)", FLOAT),
    ]),
    ("디버그", [
        ("debugEnabled", "디버그 모드(숫자키 구간 이동)", BOOL),
        ("debugInvincible", "무적", BOOL),
    ]),
]

PLACEHOLDER = {FLOAT: "숫자", INT: "정수", ARRAY: "예: 1.1, 0, -1.1"}
CONTENT_TYPE = {FLOAT: (3, 2, 2), INT: (2, 2, 1), ARRAY: (0, 0, 0)}


class Doc(object):
    def __init__(self, cls, fid, body):
        self.cls = cls
        self.fid = fid
        self.body = body

    @property
    def kind(self):
        m = re.match(r"^(\w+):\s*$", self.body.splitlines()[0])
        return m.group(1) if m else "?"

    def text(self):
        return "--- !u!%d &%d\n%s" % (self.cls, self.fid, self.body)


def parse(path):
    raw = io.open(path, encoding="utf-8").read()
    header, docs, cur = [], [], None
    for line in raw.splitlines(True):
        m = re.match(r"^--- !u!(\d+) &(\d+)", line)
        if m:
            cur = Doc(int(m.group(1)), int(m.group(2)), "")
            docs.append(cur)
        elif cur is None:
            header.append(line)
        else:
            cur.body += line
    return "".join(header), docs


def field(doc, key):
    m = re.search(r"^  %s: (.*)$" % re.escape(key), doc.body, re.M)
    return m.group(1).strip() if m else None


def set_field(doc, key, value):
    doc.body, n = re.subn(r"^(  %s: ).*$" % re.escape(key), lambda m: m.group(1) + value, doc.body, count=1, flags=re.M)
    if n != 1:
        raise RuntimeError("field %s not found" % key)


def ref(value):
    m = re.match(r"\{fileID: (\d+)\}", value or "")
    return int(m.group(1)) if m else 0


def children(doc):
    m = re.search(r"^  m_Children:\n((?:  - \{fileID: \d+\}\n)*)", doc.body, re.M)
    return [int(x) for x in re.findall(r"fileID: (\d+)", m.group(1))] if m else []


def set_children(doc, ids):
    block = "  m_Children:\n" if not ids else "  m_Children:\n" + "".join("  - {fileID: %d}\n" % i for i in ids)
    if not ids:
        block = "  m_Children: []\n"
    doc.body = re.sub(r"^  m_Children:(?: \[\]\n|\n(?:  - \{fileID: \d+\}\n)*)", block, doc.body, count=1, flags=re.M)


def components(doc):
    return [int(x) for x in re.findall(r"- component: \{fileID: (\d+)\}", doc.body)]


def script_guid(doc):
    m = re.search(r"m_Script: \{fileID: 11500000, guid: (\w+),", doc.body)
    return m.group(1) if m else None


def esc(text):
    if text == "":
        return ""
    if all(32 <= ord(c) < 127 for c in text) and not re.match(r"^[\s\-?:,\[\]{}#&*!|>'\"%@`]", text) and ": " not in text:
        return text
    out = ['"']
    for c in text:
        if c == "\\":
            out.append("\\\\")
        elif c == '"':
            out.append('\\"')
        elif 32 <= ord(c) < 127:
            out.append(c)
        else:
            out.append("\\u%04X" % ord(c))
    out.append('"')
    return "".join(out)


def color(c):
    return "{r: %s, g: %s, b: %s, a: %s}" % tuple(("%g" % v) for v in c)


def vec2(v):
    return "{x: %g, y: %g}" % (v[0], v[1])


class Builder(object):
    def __init__(self, used):
        self.used = set(used)
        self.docs = []

    def new_id(self, key):
        digest = hashlib.blake2b(key.encode("utf-8"), digest_size=8).digest()
        value = int.from_bytes(digest, "big") % (9 * 10 ** 18) + 10 ** 17
        while value in self.used:
            value += 1
        self.used.add(value)
        return value

    def add(self, cls, fid, body):
        self.docs.append(Doc(cls, fid, body))
        return fid

    def game_object(self, fid, name, comps, active=1):
        lines = ["GameObject:",
                 "  m_ObjectHideFlags: 0",
                 "  m_CorrespondingSourceObject: {fileID: 0}",
                 "  m_PrefabInstance: {fileID: 0}",
                 "  m_PrefabAsset: {fileID: 0}",
                 "  serializedVersion: 6",
                 "  m_Component:"]
        lines += ["  - component: {fileID: %d}" % c for c in comps]
        lines += ["  m_Layer: 5",
                  "  m_Name: %s" % esc(name),
                  "  m_TagString: Untagged",
                  "  m_Icon: {fileID: 0}",
                  "  m_NavMeshLayer: 0",
                  "  m_StaticEditorFlags: 0",
                  "  m_IsActive: %d" % active]
        return self.add(1, fid, "\n".join(lines) + "\n")

    def rect(self, fid, go, father, kids, amin, amax, apos, size, pivot):
        kid_lines = "  m_Children: []\n" if not kids else "  m_Children:\n" + "".join(
            "  - {fileID: %d}\n" % k for k in kids)
        body = ("RectTransform:\n"
                "  m_ObjectHideFlags: 0\n"
                "  m_CorrespondingSourceObject: {fileID: 0}\n"
                "  m_PrefabInstance: {fileID: 0}\n"
                "  m_PrefabAsset: {fileID: 0}\n"
                "  m_GameObject: {fileID: %d}\n"
                "  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n"
                "  m_LocalPosition: {x: 0, y: 0, z: 0}\n"
                "  m_LocalScale: {x: 1, y: 1, z: 1}\n"
                "  m_ConstrainProportionsScale: 0\n"
                "%s"
                "  m_Father: {fileID: %d}\n"
                "  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n"
                "  m_AnchorMin: %s\n"
                "  m_AnchorMax: %s\n"
                "  m_AnchoredPosition: %s\n"
                "  m_SizeDelta: %s\n"
                "  m_Pivot: %s\n") % (go, kid_lines, father, vec2(amin), vec2(amax), vec2(apos), vec2(size),
                                      vec2(pivot))
        return self.add(224, fid, body)

    def canvas_renderer(self, fid, go):
        body = ("CanvasRenderer:\n"
                "  m_ObjectHideFlags: 0\n"
                "  m_CorrespondingSourceObject: {fileID: 0}\n"
                "  m_PrefabInstance: {fileID: 0}\n"
                "  m_PrefabAsset: {fileID: 0}\n"
                "  m_GameObject: {fileID: %d}\n"
                "  m_CullTransparentMesh: 1\n") % go
        return self.add(223, fid, body)

    def _mono_head(self, go, guid, identifier):
        return ("MonoBehaviour:\n"
                "  m_ObjectHideFlags: 0\n"
                "  m_CorrespondingSourceObject: {fileID: 0}\n"
                "  m_PrefabInstance: {fileID: 0}\n"
                "  m_PrefabAsset: {fileID: 0}\n"
                "  m_GameObject: {fileID: %d}\n"
                "  m_Enabled: 1\n"
                "  m_EditorHideFlags: 0\n"
                "  m_Script: {fileID: 11500000, guid: %s, type: 3}\n"
                "  m_Name: \n"
                "  m_EditorClassIdentifier: %s\n") % (go, guid, identifier)

    def graphic(self, go, col, raycast):
        return ("  m_Material: {fileID: 0}\n"
                "  m_Color: %s\n"
                "  m_RaycastTarget: %d\n"
                "  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n"
                "  m_Maskable: 1\n"
                "  m_OnCullStateChanged:\n"
                "    m_PersistentCalls:\n"
                "      m_Calls: []\n") % (color(col), raycast)

    def image(self, fid, go, col, sprite=SPRITE_BUILTIN, image_type=1, raycast=1):
        body = self._mono_head(go, GUID_IMAGE, "UnityEngine.UI::UnityEngine.UI.Image")
        body += self.graphic(go, col, raycast)
        body += ("  m_Sprite: %s\n"
                 "  m_Type: %d\n"
                 "  m_PreserveAspect: 0\n"
                 "  m_FillCenter: 1\n"
                 "  m_FillMethod: 4\n"
                 "  m_FillAmount: 1\n"
                 "  m_FillClockwise: 1\n"
                 "  m_FillOrigin: 0\n"
                 "  m_UseSpriteMesh: 0\n"
                 "  m_PixelsPerUnitMultiplier: 1\n") % (sprite, image_type)
        return self.add(114, fid, body)

    def text(self, fid, go, value, col, size, align, style=0, raycast=0, h_overflow=1, v_overflow=1):
        body = self._mono_head(go, GUID_TEXT, "UnityEngine.UI::UnityEngine.UI.Text")
        body += self.graphic(go, col, raycast)
        body += ("  m_FontData:\n"
                 "    m_Font: %s\n"
                 "    m_FontSize: %d\n"
                 "    m_FontStyle: %d\n"
                 "    m_BestFit: 0\n"
                 "    m_MinSize: 10\n"
                 "    m_MaxSize: 40\n"
                 "    m_Alignment: %d\n"
                 "    m_AlignByGeometry: 0\n"
                 "    m_RichText: 0\n"
                 "    m_HorizontalOverflow: %d\n"
                 "    m_VerticalOverflow: %d\n"
                 "    m_LineSpacing: 1\n"
                 "  m_Text: %s\n") % (FONT, size, style, align, h_overflow, v_overflow, esc(value))
        return self.add(114, fid, body)

    def selectable(self, target):
        return ("  m_Navigation:\n"
                "    m_Mode: 3\n"
                "    m_WrapAround: 0\n"
                "    m_SelectOnUp: {fileID: 0}\n"
                "    m_SelectOnDown: {fileID: 0}\n"
                "    m_SelectOnLeft: {fileID: 0}\n"
                "    m_SelectOnRight: {fileID: 0}\n"
                "  m_Transition: 1\n"
                "  m_Colors:\n"
                "    m_NormalColor: {r: 1, g: 1, b: 1, a: 1}\n"
                "    m_HighlightedColor: {r: 0.9607843, g: 0.9607843, b: 0.9607843, a: 1}\n"
                "    m_PressedColor: {r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 1}\n"
                "    m_SelectedColor: {r: 0.9607843, g: 0.9607843, b: 0.9607843, a: 1}\n"
                "    m_DisabledColor: {r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 0.5019608}\n"
                "    m_ColorMultiplier: 1\n"
                "    m_FadeDuration: 0.1\n"
                "  m_SpriteState:\n"
                "    m_HighlightedSprite: {fileID: 0}\n"
                "    m_PressedSprite: {fileID: 0}\n"
                "    m_SelectedSprite: {fileID: 0}\n"
                "    m_DisabledSprite: {fileID: 0}\n"
                "  m_AnimationTriggers:\n"
                "    m_NormalTrigger: Normal\n"
                "    m_HighlightedTrigger: Highlighted\n"
                "    m_PressedTrigger: Pressed\n"
                "    m_SelectedTrigger: Selected\n"
                "    m_DisabledTrigger: Disabled\n"
                "  m_Interactable: 1\n"
                "  m_TargetGraphic: {fileID: %d}\n") % target

    def input_field(self, fid, go, background, text_comp, placeholder, kind):
        content_type, keyboard, validation = CONTENT_TYPE[kind]
        body = self._mono_head(go, GUID_INPUTFIELD, "UnityEngine.UI::UnityEngine.UI.InputField")
        body += self.selectable(background)
        body += ("  m_TextComponent: {fileID: %d}\n"
                 "  m_Placeholder: {fileID: %d}\n"
                 "  m_ContentType: %d\n"
                 "  m_InputType: 0\n"
                 "  m_AsteriskChar: 42\n"
                 "  m_KeyboardType: %d\n"
                 "  m_LineType: 0\n"
                 "  m_HideMobileInput: 0\n"
                 "  m_CharacterValidation: %d\n"
                 "  m_CharacterLimit: 0\n"
                 "  m_OnSubmit:\n"
                 "    m_PersistentCalls:\n"
                 "      m_Calls: []\n"
                 "  m_OnDidEndEdit:\n"
                 "    m_PersistentCalls:\n"
                 "      m_Calls: []\n"
                 "  m_OnValueChanged:\n"
                 "    m_PersistentCalls:\n"
                 "      m_Calls: []\n"
                 "  m_CaretColor: {r: 0.19607843, g: 0.19607843, b: 0.19607843, a: 1}\n"
                 "  m_CustomCaretColor: 0\n"
                 "  m_SelectionColor: {r: 0.65882355, g: 0.80784315, b: 1, a: 0.7529412}\n"
                 "  m_Text: \n"
                 "  m_CaretBlinkRate: 0.85\n"
                 "  m_CaretWidth: 1\n"
                 "  m_ReadOnly: 0\n"
                 "  m_ShouldActivateOnSelect: 1\n") % (text_comp, placeholder, content_type, keyboard, validation)
        return self.add(114, fid, body)

    def toggle(self, fid, go, background, checkmark):
        body = self._mono_head(go, GUID_TOGGLE, "UnityEngine.UI::UnityEngine.UI.Toggle")
        body += self.selectable(background)
        body += ("  toggleTransition: 1\n"
                 "  graphic: {fileID: %d}\n"
                 "  m_Group: {fileID: 0}\n"
                 "  onValueChanged:\n"
                 "    m_PersistentCalls:\n"
                 "      m_Calls: []\n"
                 "  m_IsOn: 0\n") % checkmark
        return self.add(114, fid, body)

    def scroll_rect(self, fid, go, content, viewport):
        body = self._mono_head(go, GUID_SCROLLRECT, "UnityEngine.UI::UnityEngine.UI.ScrollRect")
        body += ("  m_Content: {fileID: %d}\n"
                 "  m_Horizontal: 0\n"
                 "  m_Vertical: 1\n"
                 "  m_MovementType: 2\n"
                 "  m_Elasticity: 0.1\n"
                 "  m_Inertia: 1\n"
                 "  m_DecelerationRate: 0.135\n"
                 "  m_ScrollSensitivity: 25\n"
                 "  m_Viewport: {fileID: %d}\n"
                 "  m_HorizontalScrollbar: {fileID: 0}\n"
                 "  m_VerticalScrollbar: {fileID: 0}\n"
                 "  m_HorizontalScrollbarVisibility: 0\n"
                 "  m_VerticalScrollbarVisibility: 0\n"
                 "  m_HorizontalScrollbarSpacing: 0\n"
                 "  m_VerticalScrollbarSpacing: 0\n"
                 "  m_OnValueChanged:\n"
                 "    m_PersistentCalls:\n"
                 "      m_Calls: []\n") % (content, viewport)
        return self.add(114, fid, body)

    def mask(self, fid, go):
        body = self._mono_head(go, GUID_MASK, "UnityEngine.UI::UnityEngine.UI.Mask")
        body += "  m_ShowMaskGraphic: 1\n"
        return self.add(114, fid, body)

    def vertical_layout(self, fid, go):
        body = self._mono_head(go, GUID_VLG, "UnityEngine.UI::UnityEngine.UI.VerticalLayoutGroup")
        body += ("  m_Padding:\n"
                 "    m_Left: %d\n"
                 "    m_Right: %d\n"
                 "    m_Top: %d\n"
                 "    m_Bottom: %d\n"
                 "  m_ChildAlignment: 0\n"
                 "  m_Spacing: %d\n"
                 "  m_ChildForceExpandWidth: 1\n"
                 "  m_ChildForceExpandHeight: 0\n"
                 "  m_ChildControlWidth: 1\n"
                 "  m_ChildControlHeight: 0\n"
                 "  m_ChildScaleWidth: 0\n"
                 "  m_ChildScaleHeight: 0\n"
                 "  m_ReverseArrangement: 0\n") % (PAD_L, PAD_R, PAD_T, PAD_B, SPACING)
        return self.add(114, fid, body)

    def size_fitter(self, fid, go):
        body = self._mono_head(go, GUID_CSF, "UnityEngine.UI::UnityEngine.UI.ContentSizeFitter")
        body += "  m_HorizontalFit: 0\n  m_VerticalFit: 2\n"
        return self.add(114, fid, body)

    def field_row(self, fid, go, key, kind, label, hint, value_field, value_toggle):
        body = self._mono_head(go, GUID_CONFIG_FIELD_ROW, "Game.Settings::Game.Settings.ConfigFieldRow")
        body += ("  key: %s\n"
                 "  kind: %d\n"
                 "  labelText: {fileID: %d}\n"
                 "  keyHintText: {fileID: %d}\n"
                 "  valueField: {fileID: %d}\n"
                 "  valueToggle: {fileID: %d}\n") % (key, kind, label, hint, value_field, value_toggle)
        return self.add(114, fid, body)


def build_list(builder, parent_rt, keys_seen):
    root_go = builder.new_id("list.go")
    root_rt = builder.new_id("list.rt")
    view_go = builder.new_id("view.go")
    view_rt = builder.new_id("view.rt")
    view_cr = builder.new_id("view.cr")
    view_img = builder.new_id("view.img")
    view_mask = builder.new_id("view.mask")
    content_go = builder.new_id("content.go")
    content_rt = builder.new_id("content.rt")
    content_vlg = builder.new_id("content.vlg")
    content_csf = builder.new_id("content.csf")

    content_kids = []
    row_components = []
    total = PAD_T + PAD_B
    count = 0

    for index, (section, rows) in enumerate(SECTIONS):
        prefix = "header." + section
        go = builder.new_id(prefix + ".go")
        rt = builder.new_id(prefix + ".rt")
        cr = builder.new_id(prefix + ".cr")
        txt = builder.new_id(prefix + ".text")
        builder.game_object(go, "Header_%d" % index, [rt, cr, txt])
        builder.rect(rt, go, content_rt, [], (0, 1), (1, 1), (0, 0), (0, HEADER_H), (0.5, 1))
        builder.canvas_renderer(cr, go)
        builder.text(txt, go, section, C_HEADER, 26, 3, style=1)
        content_kids.append(rt)
        total += HEADER_H
        count += 1

        for key, label, kind in rows:
            if key in keys_seen:
                raise RuntimeError("duplicate key " + key)
            keys_seen.add(key)

            base = "row." + key
            go = builder.new_id(base + ".go")
            rt = builder.new_id(base + ".rt")
            comp = builder.new_id(base + ".row")
            label_go = builder.new_id(base + ".label.go")
            label_rt = builder.new_id(base + ".label.rt")
            label_cr = builder.new_id(base + ".label.cr")
            label_txt = builder.new_id(base + ".label.text")
            hint_go = builder.new_id(base + ".hint.go")
            hint_rt = builder.new_id(base + ".hint.rt")
            hint_cr = builder.new_id(base + ".hint.cr")
            hint_txt = builder.new_id(base + ".hint.text")

            builder.rect(label_rt, label_go, rt, [], (0, 0.5), (0, 0.5), (0, 0), (LABEL_W, 40), (0, 0.5))
            builder.canvas_renderer(label_cr, label_go)
            builder.text(label_txt, label_go, label, C_LABEL, 26, 3)
            builder.game_object(label_go, "Label", [label_rt, label_cr, label_txt])

            builder.rect(hint_rt, hint_go, rt, [], (0, 0.5), (0, 0.5), (HINT_X, 0), (HINT_W, 40), (0, 0.5))
            builder.canvas_renderer(hint_cr, hint_go)
            builder.text(hint_txt, hint_go, key, C_HINT, 20, 3)
            builder.game_object(hint_go, "KeyHint", [hint_rt, hint_cr, hint_txt])

            if kind == BOOL:
                editor_go = builder.new_id(base + ".toggle.go")
                editor_rt = builder.new_id(base + ".toggle.rt")
                editor_comp = builder.new_id(base + ".toggle")
                bg_go = builder.new_id(base + ".bg.go")
                bg_rt = builder.new_id(base + ".bg.rt")
                bg_cr = builder.new_id(base + ".bg.cr")
                bg_img = builder.new_id(base + ".bg.img")
                mark_go = builder.new_id(base + ".mark.go")
                mark_rt = builder.new_id(base + ".mark.rt")
                mark_cr = builder.new_id(base + ".mark.cr")
                mark_img = builder.new_id(base + ".mark.img")

                builder.rect(mark_rt, mark_go, bg_rt, [], (0, 0), (1, 1), (0, 0), (-10, -10), (0.5, 0.5))
                builder.canvas_renderer(mark_cr, mark_go)
                builder.image(mark_img, mark_go, C_CHECK, sprite=SPRITE_SQUARE, image_type=0, raycast=0)
                builder.game_object(mark_go, "Checkmark", [mark_rt, mark_cr, mark_img])

                builder.rect(bg_rt, bg_go, editor_rt, [mark_rt], (0, 0.5), (0, 0.5), (0, 0), (34, 34), (0, 0.5))
                builder.canvas_renderer(bg_cr, bg_go)
                builder.image(bg_img, bg_go, C_FIELD_BG)
                builder.game_object(bg_go, "Background", [bg_rt, bg_cr, bg_img])

                builder.rect(editor_rt, editor_go, rt, [bg_rt], (1, 0.5), (1, 0.5), (0, 0), (EDITOR_W, EDITOR_H),
                             (1, 0.5))
                builder.toggle(editor_comp, editor_go, bg_img, mark_img)
                builder.game_object(editor_go, "Toggle", [editor_rt, editor_comp])

                builder.field_row(comp, go, key, kind, label_txt, hint_txt, 0, editor_comp)
            else:
                editor_go = builder.new_id(base + ".input.go")
                editor_rt = builder.new_id(base + ".input.rt")
                editor_cr = builder.new_id(base + ".input.cr")
                editor_img = builder.new_id(base + ".input.img")
                editor_comp = builder.new_id(base + ".input")
                ph_go = builder.new_id(base + ".ph.go")
                ph_rt = builder.new_id(base + ".ph.rt")
                ph_cr = builder.new_id(base + ".ph.cr")
                ph_txt = builder.new_id(base + ".ph.text")
                tx_go = builder.new_id(base + ".tx.go")
                tx_rt = builder.new_id(base + ".tx.rt")
                tx_cr = builder.new_id(base + ".tx.cr")
                tx_txt = builder.new_id(base + ".tx.text")

                builder.rect(ph_rt, ph_go, editor_rt, [], (0, 0), (1, 1), (0, 0), (-20, -12), (0.5, 0.5))
                builder.canvas_renderer(ph_cr, ph_go)
                builder.text(ph_txt, ph_go, PLACEHOLDER[kind], C_PLACEHOLDER, 22, 3, style=2)
                builder.game_object(ph_go, "Placeholder", [ph_rt, ph_cr, ph_txt])

                builder.rect(tx_rt, tx_go, editor_rt, [], (0, 0), (1, 1), (0, 0), (-20, -12), (0.5, 0.5))
                builder.canvas_renderer(tx_cr, tx_go)
                builder.text(tx_txt, tx_go, "", C_FIELD_TEXT, 24, 3)
                builder.game_object(tx_go, "Text", [tx_rt, tx_cr, tx_txt])

                builder.rect(editor_rt, editor_go, rt, [ph_rt, tx_rt], (1, 0.5), (1, 0.5), (0, 0),
                             (EDITOR_W, EDITOR_H), (1, 0.5))
                builder.canvas_renderer(editor_cr, editor_go)
                builder.image(editor_img, editor_go, C_FIELD_BG)
                builder.input_field(editor_comp, editor_go, editor_img, tx_txt, ph_txt, kind)
                builder.game_object(editor_go, "InputField", [editor_rt, editor_cr, editor_img, editor_comp])

                builder.field_row(comp, go, key, kind, label_txt, hint_txt, editor_comp, 0)

            builder.rect(rt, go, content_rt, [label_rt, hint_rt, editor_rt], (0, 1), (1, 1), (0, 0), (0, ROW_H),
                         (0.5, 1))
            builder.game_object(go, "Row_" + key, [rt, comp])
            content_kids.append(rt)
            row_components.append(comp)
            total += ROW_H
            count += 1

    total += SPACING * max(0, count - 1)

    builder.rect(content_rt, content_go, view_rt, content_kids, (0, 1), (1, 1), (0, 0), (0, total), (0.5, 1))
    builder.vertical_layout(content_vlg, content_go)
    builder.size_fitter(content_csf, content_go)
    builder.game_object(content_go, "Content", [content_rt, content_vlg, content_csf])

    builder.rect(view_rt, view_go, root_rt, [content_rt], (0, 0), (1, 1), (0, 0), (0, 0), (0, 1))
    builder.canvas_renderer(view_cr, view_go)
    builder.image(view_img, view_go, C_VIEWPORT, sprite=SPRITE_SQUARE, image_type=0, raycast=1)
    builder.mask(view_mask, view_go)
    builder.game_object(view_go, "Viewport", [view_rt, view_cr, view_img, view_mask])

    scroll = builder.new_id("list.scroll")
    builder.rect(root_rt, root_go, parent_rt, [view_rt], (0.5, 0.5), (0.5, 0.5), (0, LIST_Y), (LIST_W, LIST_H),
                 (0.5, 0.5))
    builder.scroll_rect(scroll, root_go, content_rt, view_rt)
    builder.game_object(root_go, "FieldList", [root_rt, scroll])

    return root_rt, scroll, row_components, total


def main():
    header, docs = parse(PREFAB)
    by_id = {d.fid: d for d in docs}

    panel_mono = next(d for d in docs if d.cls == 114 and script_guid(d) == GUID_CONFIG_PANEL)
    panel_go = by_id[ref(field(panel_mono, "m_GameObject"))]
    panel_rt = next(by_id[c] for c in components(panel_go) if by_id[c].cls == 224)
    window_rt = by_id[children(panel_rt)[0]]
    window_go = by_id[ref(field(window_rt, "m_GameObject"))]
    if field(window_go, "m_Name") != "Window":
        raise RuntimeError("ConfigPanel/Window not found")

    named = {}
    for child in children(window_rt):
        go = by_id[ref(field(by_id[child], "m_GameObject"))]
        named[field(go, "m_Name")] = (by_id[child], go)

    doomed = set()

    def collect(rt_id):
        rt = by_id[rt_id]
        go_id = ref(field(rt, "m_GameObject"))
        doomed.add(go_id)
        doomed.update(components(by_id[go_id]))
        for kid in children(rt):
            collect(kid)

    for name in ("JsonField", "FieldList"):
        if name in named:
            collect(named[name][0].fid)

    docs = [d for d in docs if d.fid not in doomed]
    by_id = {d.fid: d for d in docs}

    builder = Builder(by_id.keys())
    keys_seen = set()
    list_rt, scroll, row_components, total = build_list(builder, window_rt.fid, keys_seen)

    title_rt, title_go = named["Title"]
    title_text = next(by_id[c] for c in components(title_go) if by_id[c].cls == 114)
    set_field(title_text, "m_Text", esc("개발자 설정 (GameConfig)"))
    set_field(title_rt, "m_AnchoredPosition", vec2((0, TITLE_Y)))
    set_field(title_rt, "m_SizeDelta", vec2((TITLE_W, TITLE_H)))

    status_rt, status_go = named["Status"]
    status_text = next(by_id[c] for c in components(status_go) if by_id[c].cls == 114)
    set_field(status_rt, "m_AnchoredPosition", vec2((0, STATUS_Y)))
    set_field(status_rt, "m_SizeDelta", vec2((STATUS_W, STATUS_H)))
    status_text.body = re.sub(r"^(    m_FontSize: ).*$", r"\g<1>22", status_text.body, count=1, flags=re.M)

    buttons = {}
    for name, x in BUTTON_X.items():
        rt, go = named[name]
        set_field(rt, "m_AnchoredPosition", vec2((x, BUTTON_Y)))
        set_field(rt, "m_SizeDelta", vec2((BUTTON_W, BUTTON_H)))
        buttons[name] = next(by_id[c] for c in components(go) if by_id[c].cls == 114
                             and script_guid(by_id[c]) == GUID_BUTTON).fid

    set_field(window_rt, "m_SizeDelta", vec2((WINDOW_W, WINDOW_H)))
    set_children(window_rt, [title_rt.fid, list_rt, status_rt.fid,
                             named["ApplyButton"][0].fid, named["ResetButton"][0].fid, named["CloseButton"][0].fid])

    rows_block = "  rows: []\n" if not row_components else "  rows:\n" + "".join(
        "  - {fileID: %d}\n" % c for c in row_components)
    panel_mono.body = (builder._mono_head(panel_go.fid, GUID_CONFIG_PANEL, "Game.Settings::Game.Settings.ConfigPanel")
                       + "  scrollRect: {fileID: %d}\n" % scroll
                       + rows_block
                       + "  applyButton: {fileID: %d}\n" % buttons["ApplyButton"]
                       + "  resetButton: {fileID: %d}\n" % buttons["ResetButton"]
                       + "  closeButton: {fileID: %d}\n" % buttons["CloseButton"]
                       + "  statusText: {fileID: %d}\n" % status_text.fid)

    docs = docs + builder.docs
    docs.sort(key=lambda d: d.fid)
    io.open(PREFAB, "w", encoding="utf-8", newline="\n").write(header + "".join(d.text() for d in docs))

    print("행 %d개, 섹션 %d개, 새 오브젝트 %d개, Content 높이 %dpx"
          % (len(row_components), len(SECTIONS), sum(1 for d in builder.docs if d.cls == 1), total))
    print("FieldList rt=%d  ScrollRect=%d" % (list_rt, scroll))


if __name__ == "__main__":
    main()
