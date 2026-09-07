"""Build the shipped Korean font from the official OFL variable font. Development only."""
import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).parent / ".python-deps"))
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont

source, destination = map(pathlib.Path, sys.argv[1:3])
font = TTFont(source)
print("Source axes:", [(a.axisTag, a.defaultValue) for a in font["fvar"].axes])
font = instantiateVariableFont(font, {"wght": 500}, inplace=True)
names = {1: "AfterSignal Sans KR", 2: "Regular", 3: "AfterSignalSansKR-500", 4: "AfterSignal Sans KR Regular", 6: "AfterSignalSansKR-Regular", 16: "AfterSignal Sans KR", 17: "Regular"}
for record in font["name"].names:
    if record.nameID in names:
        record.string = names[record.nameID].encode(record.getEncoding(), errors="replace")
font.save(destination)
print("Saved static 500-weight font:", destination)
