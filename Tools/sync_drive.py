import argparse
import hashlib
import io
import json
import sys
import time
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
CONFIG_PATH = ROOT / "Tools" / "drive_sync.json"
STATE_PATH = ROOT / "Tools" / ".drive_sync_state.json"
MANIFEST_NAME = ".sync_manifest.json"
LABELS = {"added": "추가", "updated": "갱신", "moved": "이동", "removed": "삭제"}

GOOGLE_EXPORTS = {
    "application/vnd.google-apps.document": (
        "https://docs.google.com/document/d/{id}/export?format={fmt}", ["md", "txt"]),
    "application/vnd.google-apps.spreadsheet": (
        "https://docs.google.com/spreadsheets/d/{id}/export?format={fmt}", ["csv"]),
    "application/vnd.google-apps.presentation": (
        "https://docs.google.com/presentation/d/{id}/export/{fmt}", ["pdf"]),
}


def load_json(path, default):
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return default


def save_json(path, data):
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def folder_id_from(url):
    return url.rstrip("/").split("/folders/")[-1].split("?")[0]


def list_tree(sess, folder_url):
    from gdown.download_folder import _download_and_parse_google_drive_link
    return _download_and_parse_google_drive_link(
        sess=sess, folder_id=folder_id_from(folder_url), quiet=True, verify=True)


def descend(tree, subfolder):
    if not subfolder:
        return tree
    for child in tree.children:
        if child.is_folder() and child.name == subfolder:
            return child
    return None


def flatten(node, prefix):
    for child in node.children:
        if child.is_folder():
            yield from flatten(child, prefix / child.name)
        else:
            yield child, prefix / child.name


def fetch(sess, file, source):
    export = GOOGLE_EXPORTS.get(file.type)
    if export:
        if not source.get("google_exports", True):
            return None, None
        template, formats = export
        for fmt in formats:
            resp = sess.get(template.format(id=file.id, fmt=fmt), timeout=60)
            if resp.status_code == 200 and not resp.headers.get("content-type", "").startswith("text/html"):
                return resp.content, f".{fmt}"
        raise RuntimeError(f"export 실패 {file.name}")
    if file.type.startswith("application/vnd.google-apps."):
        return None, None
    extensions = set(e.lower() for e in source.get("extensions", []))
    suffix = Path(file.name).suffix.lower()
    if extensions and suffix and suffix not in extensions:
        return None, None
    resp = sess.get(f"https://drive.google.com/uc?id={file.id}&export=download", timeout=300)
    if resp.status_code != 200 or resp.headers.get("content-type", "").startswith("text/html"):
        raise RuntimeError(f"다운로드 실패 {file.name}")
    if suffix:
        return resp.content, ""
    guessed = guess_suffix(resp.content)
    if extensions and guessed not in extensions:
        return None, None
    return resp.content, guessed


def guess_suffix(content):
    if content.startswith(b"%PDF"):
        return ".pdf"
    if content[:2] == b"PK":
        return ".docx"
    try:
        text = content.decode("utf-8")
    except UnicodeDecodeError:
        return ""
    stripped = text.lstrip(chr(0xFEFF) + " " + chr(13) + chr(10) + chr(9))
    if stripped.startswith("{") or stripped.startswith("["):
        return ".json"
    newline = chr(10)
    if stripped.startswith("#") or (newline + "#") in text or (newline + "- ") in text:
        return ".md"
    return ".txt"


def expand(file_id, rel, content, source):
    if rel.suffix.lower() != ".zip":
        yield file_id, rel.as_posix(), content
        return
    extensions = set(e.lower() for e in source.get("extensions", []))
    folder = rel.with_suffix("")
    try:
        archive = zipfile.ZipFile(io.BytesIO(content))
    except zipfile.BadZipFile:
        raise RuntimeError(f"zip 열기 실패 {rel.name}")
    for info in archive.infolist():
        if info.is_dir():
            continue
        name = info.filename
        if not info.flag_bits & 0x800:
            try:
                name = name.encode("cp437").decode("cp949")
            except (UnicodeEncodeError, UnicodeDecodeError):
                pass
        member = Path(name)
        if extensions and member.suffix.lower() not in extensions:
            continue
        parts = member.parts[1:] if len(member.parts) > 1 and member.parts[0] == folder.name else member.parts
        key = (folder / Path(*parts)).as_posix()
        yield f"{file_id}#{member.as_posix()}", key, archive.read(info)


def move(target, old_key, new_key):
    for suffix in ("", ".meta"):
        src = target / (old_key + suffix)
        if src.exists():
            dst = target / (new_key + suffix)
            dst.parent.mkdir(parents=True, exist_ok=True)
            src.rename(dst)
    prune_empty_dirs(target, (target / old_key).parent)


def remove(target, key):
    for suffix in ("", ".meta"):
        path = target / (key + suffix)
        if path.exists():
            path.unlink()
    prune_empty_dirs(target, (target / key).parent)


def prune_empty_dirs(target, folder):
    target = target.resolve()
    try:
        folder = folder.resolve()
    except OSError:
        return
    while folder != target and target in folder.parents:
        if not folder.is_dir() or any(folder.iterdir()):
            return
        meta = folder.parent / (folder.name + ".meta")
        try:
            folder.rmdir()
        except OSError:
            return
        if meta.exists():
            meta.unlink()
        folder = folder.parent


def sync(source, sess):
    target = ROOT / source["target"]
    target.mkdir(parents=True, exist_ok=True)
    manifest_path = target / MANIFEST_NAME
    manifest = load_json(manifest_path, {})
    by_id = {v["id"]: k for k, v in manifest.items()}

    tree = descend(list_tree(sess, source["folder"]), source.get("subfolder"))
    if tree is None:
        raise RuntimeError(f"Drive에 `{source.get('subfolder')}` 폴더가 없다")

    result = dict(added=[], updated=[], moved=[], removed=[], errors=[], skipped=0)
    seen = {}

    for file, rel in flatten(tree, Path()):
        try:
            content, suffix = fetch(sess, file, source)
        except Exception as e:
            result["errors"].append(str(e))
            continue
        if content is None:
            continue
        if suffix and rel.suffix.lower() != suffix:
            rel = rel.with_name(rel.name + suffix)
        for entry_id, key, data in expand(file.id, rel, content, source):
            old_key = by_id.get(entry_id)
            if old_key and old_key != key and (target / old_key).exists():
                move(target, old_key, key)
                result["moved"].append(f"{old_key} -> {key}")
            previous = manifest.get(old_key or key)

            digest = hashlib.sha256(data).hexdigest()
            seen[key] = {"id": entry_id, "sha256": digest}
            dest = target / key
            if dest.exists() and previous and previous.get("sha256") == digest:
                result["skipped"] += 1
                continue
            dest.parent.mkdir(parents=True, exist_ok=True)
            dest.write_bytes(data)
            result["updated" if previous else "added"].append(key)

    seen_ids = {v["id"] for v in seen.values()}
    for key, info in manifest.items():
        if key in seen or info["id"] in seen_ids:
            continue
        remove(target, key)
        result["removed"].append(key)

    save_json(manifest_path, seen)
    return result


def changed(r):
    return any(r[k] for k in ("added", "updated", "moved", "removed", "errors"))


def report(name, r, hint):
    tag = f"[{name} 동기화]"
    if not changed(r):
        print(f"{tag} 변경 없음 (파일 {r['skipped']}개)")
        return
    print(f"{tag} 추가 {len(r['added'])}, 갱신 {len(r['updated'])}, 이동 {len(r['moved'])}, "
          f"삭제 {len(r['removed'])}, 유지 {r['skipped']}")
    for kind, label in LABELS.items():
        for item in r[kind]:
            print(f"  {label}: {item}")
    for err in r["errors"]:
        print(f"  오류: {err}")
    if hint and (r["added"] or r["updated"]):
        print(f"  {hint}")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", help="설정의 소스 이름. 없으면 전부")
    parser.add_argument("--hook", action="store_true", help="hook: true인 소스만")
    parser.add_argument("--min-interval", type=int, default=0)
    parser.add_argument("--quiet-if-unchanged", action="store_true")
    args = parser.parse_args()

    if args.min_interval:
        state = load_json(STATE_PATH, {})
        if time.time() - state.get("last_run", 0) < args.min_interval:
            return 0

    config = load_json(CONFIG_PATH, None)
    if not config:
        print(f"[Drive 동기화] 설정 없음: {CONFIG_PATH}")
        return 0

    try:
        import requests
        import gdown  # noqa: F401
    except ImportError:
        print("[Drive 동기화] gdown이 없다. `python -m pip install gdown`. 동기화를 건너뛴다.")
        return 0

    sess = requests.Session()
    for name, source in config.items():
        if args.source and name != args.source:
            continue
        if args.hook and not source.get("hook"):
            continue
        try:
            result = sync(source, sess)
        except Exception as e:
            print(f"[{name} 동기화] 실패: {e}. 대상 폴더는 마지막 동기화 상태다.")
            continue
        if args.quiet_if_unchanged and not changed(result):
            continue
        report(name, result, source.get("hint", ""))

    if args.hook or args.min_interval:
        save_json(STATE_PATH, {"last_run": time.time()})
    return 0


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    sys.exit(main())
