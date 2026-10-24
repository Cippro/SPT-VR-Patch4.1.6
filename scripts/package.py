from pathlib import Path, PurePosixPath
import hashlib
import zipfile

root = Path(__file__).resolve().parent.parent
original = root / 'SPT-VR-1.3.0.zip'
dll = root / 'artifacts/SPT-VR.dll'
output = root / 'artifacts/SPT-VR-4.1.6-test.zip'
plugin_path = 'BepInEx/plugins/sptvr/SPT-VR.dll'
with zipfile.ZipFile(original) as source, zipfile.ZipFile(output, 'w', compression=zipfile.ZIP_DEFLATED) as target:
    bad = source.testzip()
    if bad:
        raise RuntimeError('Base release archive CRC failure: ' + bad)
    replaced = False
    for entry in source.infolist():
        path = PurePosixPath(entry.filename)
        if path.is_absolute() or '..' in path.parts:
            raise RuntimeError('Unsafe base release archive path: ' + entry.filename)
        if entry.is_dir() or 'fika' in entry.filename.lower():
            continue
        if path.name in ['Assembly-CSharp.dll', 'Assembly-CSharp-firstpass.dll']:
            raise RuntimeError('Game references must not be shipped in the mod package')
        if entry.filename == plugin_path:
            target.writestr(entry.filename, dll.read_bytes())
            replaced = True
        else:
            target.writestr(entry.filename, source.read(entry))
    if not replaced:
        raise RuntimeError('Base archive did not contain the expected plugin path')
    target.writestr('SPT-VR-4.1.6-TESTING.md', (root / 'TESTING.md').read_text(encoding='utf-8'))
with zipfile.ZipFile(output) as archive:
    assert archive.testzip() is None
    assert archive.read(plugin_path) == dll.read_bytes()
    assert not any('fika' in name.lower() for name in archive.namelist())
    assert not any('Assembly-CSharp' in name for name in archive.namelist())
for path in [dll, output]:
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    path.with_name(path.name + '.sha256').write_text(digest + '  ' + path.name + '\n')
    print(path.name, digest)
