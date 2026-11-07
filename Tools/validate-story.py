#!/usr/bin/env python3
"""Validate the story data loaded by the runtime (standard library only).

  Assets/AfterSignal/Resources/Story/CityChronicle.json    -> CityChronicle.cs / CampaignBattle.cs
  Assets/AfterSignal/Resources/Story/FourCityCampaign.json -> FourCityCampaign.cs

Usage: python3 Tools/validate-story.py [--root <repo>] [--strict]
Exit status 1 when an error is found (with --strict, warnings count as errors).
JsonUtility silently ignores unknown or misspelled keys, so unknown keys are reported as errors.
"""
import argparse
import json
import os
import re
import sys

# Field names must match the [Serializable] classes in Runtime/Campaign/CityChronicle.cs.
QUEST_KEYS = {"id", "title", "arc", "after", "summary", "endingA", "endingB", "main", "tutorial", "finale", "unlockAt", "reward", "steps"}
STEP_KEYS = {"site", "kind", "person", "label", "line", "battleMode", "outro", "choiceA", "choiceB", "portrait", "formation",
             "waves", "enemyCount", "enemyHealth", "duration", "timeLimit", "world", "position"}
BATTLE_ONLY = {"battleMode", "outro", "formation", "waves", "enemyCount", "enemyHealth", "duration", "timeLimit"}
KINDS = {"meet", "scan", "deliver", "choice", "combat", "battle"}
NPC_KINDS = {"meet", "deliver", "choice"}          # spawn a witness NPC (CityChronicle.Spawn)
BATTLE_MODES = {"assault", "defend", "sabotage", "rescue", "escape", "boss"}
FORMATIONS = {"", "stronghold"}
SITE_COUNT = 40                                     # urban interior sites addressable by non-world steps
SAVED_STEP_COUNT = 3                                # released saves store step indices for 3-step quests
PEOPLE_ART = {"OfficeMan", "OfficeWoman", "Worker", "Police", "CivilianWoman", "CivilianMan", "ElderWoman", "ElderMan", "Bartender",
              "Nurse", "Doctor", "TeacherWoman", "TeacherMan", "StudentGirl", "StudentBoy", "Soldier"}
FACILITY_SHEETS = ["Aircrew", "Travellers", "Harbour", "Coast", "NightMarket", "Utilities", "Pelagic", "NeonTrade", "Arcology", "CanalLife", "CivicFuture", "SkyWard"]
CHOICE_LABEL_MAX = 24                               # story choice buttons are 440 px wide at 16 px (journal buttons 474 px)

# Field names must match FourCityCampaign.Chapter in Runtime/Worlds/FourCityCampaign.cs.
CHAPTER_KEYS = {"id", "title", "speaker", "line", "objective", "prompt", "choiceA", "choiceB", "resultA", "resultB",
                "facility", "floor", "enemies", "sync", "elite", "decision", "finale"}
SAVED_CHAPTERS = 12                                 # AFTERSIGNAL.Unity.FourCities.Chapter of released saves reaches 12


class Report:
    def __init__(self):
        self.errors, self.warnings = [], []

    def error(self, where, text): self.errors.append("%s: %s" % (where, text))
    def warn(self, where, text): self.warnings.append("%s: %s" % (where, text))


def text(value):
    return isinstance(value, str) and value.strip() != ""


def number(value):
    return isinstance(value, (int, float)) and not isinstance(value, bool)


def integer(value):
    return isinstance(value, int) and not isinstance(value, bool)


def check_range(r, where, obj, key, low, high, required=False, whole=False):
    if key not in obj:
        if required: r.error(where, "missing '%s'" % key)
        return
    value = obj[key]
    if not (integer(value) if whole else number(value)):
        r.error(where, "'%s' must be %s, got %r" % (key, "an integer" if whole else "a number", value)); return
    if not low <= value <= high:
        r.error(where, "'%s'=%r outside %r..%r" % (key, value, low, high))


def check_step(r, where, step):
    unknown = set(step) - STEP_KEYS
    if unknown: r.error(where, "unknown keys %s" % sorted(unknown))
    kind = step.get("kind")
    if kind not in KINDS:
        r.error(where, "unknown kind %r (known: %s)" % (kind, ", ".join(sorted(KINDS)))); return
    for key in ("label", "line"):
        if not text(step.get(key)): r.error(where, "%s step needs a non-empty '%s'" % (kind, key))
    check_range(r, where, step, "site", 0, SITE_COUNT - 1, whole=True)
    world = step.get("world", False)
    if not isinstance(world, bool): r.error(where, "'world' must be true/false")
    if world:
        pos = step.get("position")
        if not isinstance(pos, dict) or set(pos) != {"x", "y", "z"} or not all(number(pos[a]) for a in "xyz"):
            r.error(where, "world step needs position {x,y,z}")
        else:
            if abs(pos["x"]) > 10000 or abs(pos["z"]) > 10000: r.error(where, "position %r is outside the map" % pos)
            if not -200 <= pos["y"] <= 500: r.error(where, "position y=%r is implausible" % pos["y"])
    elif "position" in step:
        r.warn(where, "'position' is ignored unless 'world' is true")
    if kind in NPC_KINDS and not text(step.get("person")):
        r.error(where, "%s step needs a 'person' (the witness NPC)" % kind)
    if kind == "choice":
        a, b = step.get("choiceA"), step.get("choiceB")
        if not text(a) or not text(b): r.error(where, "choice step needs 'choiceA' and 'choiceB' option labels")
        elif a.strip() == b.strip(): r.error(where, "choiceA and choiceB are identical")
        else:
            for key, label in (("choiceA", a), ("choiceB", b)):
                if len(label) > CHOICE_LABEL_MAX: r.warn(where, "%s is %d characters; buttons fit about %d" % (key, len(label), CHOICE_LABEL_MAX))
    elif "choiceA" in step or "choiceB" in step:
        r.warn(where, "choiceA/choiceB only apply to choice steps")
    if "portrait" in step:
        art = step["portrait"]
        if not text(art) or not (art in PEOPLE_ART or re.fullmatch(r"Facility(%s)[0-3]" % "|".join(FACILITY_SHEETS), art)):
            r.error(where, "portrait %r is not a PeopleArt key (%s or Facility<Sheet><0-3>)" % (art, ", ".join(sorted(PEOPLE_ART))))
        if kind not in NPC_KINDS: r.warn(where, "'portrait' only applies to meet/deliver/choice steps")
    if kind == "battle":
        if world is not True: r.error(where, "battle steps run in the open city and need 'world': true with a position")
        mode = step.get("battleMode")
        if mode not in BATTLE_MODES: r.error(where, "battleMode %r not in %s" % (mode, sorted(BATTLE_MODES)))
        check_range(r, where, step, "waves", 1, 5, required=True, whole=True)
        check_range(r, where, step, "enemyCount", 3, 12, required=True, whole=True)
        check_range(r, where, step, "duration", 5, 300, required=mode == "defend")
        check_range(r, where, step, "timeLimit", 0, 600)
        check_range(r, where, step, "enemyHealth", 0, 20000, whole=True)
        if integer(step.get("enemyHealth")) and 0 < step["enemyHealth"] < 20:
            r.error(where, "enemyHealth %r is too low (0 = automatic scaling)" % step["enemyHealth"])
        if mode == "escape":
            limit = step.get("timeLimit", 0)
            if not number(limit) or limit < 30: r.error(where, "escape step needs 'timeLimit' >= 30 seconds")
            elif number(step.get("waves")) and number(step.get("enemyCount")) and limit < 15 + step["waves"] * step["enemyCount"] * 4:
                r.warn(where, "timeLimit %ss looks too short for %s waves of %s" % (limit, step["waves"], step["enemyCount"]))
        if step.get("formation", "") not in FORMATIONS: r.error(where, "formation %r not in %s" % (step.get("formation"), sorted(FORMATIONS - {""})))
        if "outro" in step and not text(step["outro"]): r.warn(where, "empty 'outro' (a default radio line is used)")
    else:
        extra = BATTLE_ONLY & set(step)
        if extra: r.warn(where, "battle-only keys on a %s step: %s" % (kind, sorted(extra)))
    return kind


def chain_root(by_id, quest):
    """Follow `after` links back to a root. Returns (path, problem)."""
    path, seen, current = [], set(), quest
    while current is not None:
        if current["id"] in seen: return path, "cycle through %s" % current["id"]
        seen.add(current["id"]); path.append(current["id"])
        after = current.get("after", "")
        if not after: return path, None
        current = by_id.get(after)
        if current is None: return path, "broken link to %r" % after
    return path, None


def validate_chronicle(r, path):
    name = os.path.basename(path)
    try:
        data = json.load(open(path, encoding="utf-8"))
    except (OSError, ValueError) as exc:
        r.error(name, "cannot read JSON: %s" % exc); return None
    quests = data.get("quests") if isinstance(data, dict) else None
    if not isinstance(quests, list) or not quests:
        r.error(name, "top level needs a non-empty 'quests' array"); return None
    if set(data) - {"quests"}: r.error(name, "unknown top-level keys %s" % sorted(set(data) - {"quests"}))
    by_id, kinds = {}, {}
    for index, q in enumerate(quests):
        where = "%s quest[%d]" % (name, index)
        if not isinstance(q, dict): r.error(where, "quest must be an object"); continue
        qid = q.get("id")
        if not text(qid): r.error(where, "missing 'id'"); continue
        where = "%s %s" % (name, qid)
        if qid in by_id: r.error(where, "duplicate id")
        by_id[qid] = q
        unknown = set(q) - QUEST_KEYS
        if unknown: r.error(where, "unknown keys %s" % sorted(unknown))
        for key in ("title", "arc", "summary"):
            if not text(q.get(key)): r.error(where, "needs a non-empty '%s'" % key)
        for key in ("main", "tutorial", "finale"):
            if key in q and not isinstance(q[key], bool): r.error(where, "'%s' must be true/false" % key)
        check_range(r, where, q, "reward", 0, 100000, required=True, whole=True)
        steps = q.get("steps")
        if not isinstance(steps, list) or not 1 <= len(steps) <= 8:
            r.error(where, "needs 1..8 steps"); continue
        if len(steps) != SAVED_STEP_COUNT:
            r.warn(where, "%d steps; saved progress assumes %d - add a migration in CityChronicle.Awake when changing a released quest" % (len(steps), SAVED_STEP_COUNT))
        for i, step in enumerate(steps):
            if not isinstance(step, dict): r.error("%s step %d" % (where, i), "step must be an object"); continue
            kind = check_step(r, "%s step %d" % (where, i), step)
            if kind: kinds[kind] = kinds.get(kind, 0) + 1
    mains = [q for q in by_id.values() if q.get("main")]
    for qid, q in by_id.items():
        where = "%s %s" % (name, qid)
        after = q.get("after", "")
        if after and after not in by_id: r.error(where, "'after' references unknown quest %r" % after)
        if after == qid: r.error(where, "'after' references itself")
        if not q.get("main") and "unlockAt" in q: check_range(r, where, q, "unlockAt", 0, len(mains), whole=True)
        if q.get("main") and isinstance(q.get("steps"), list) and not any(isinstance(s, dict) and s.get("kind") == "battle" for s in q["steps"]):
            r.warn(where, "main quest has no battle step")
        _, problem = chain_root(by_id, q)
        if problem and problem.startswith("cycle"): r.error(where, "'after' chain has a %s" % problem)  # broken links are reported at their source
        for key in ("endingA", "endingB"):
            if key in q and not q.get("finale"): r.warn(where, "'%s' is only read on the finale quest" % key)
    tutorials = [q for q in by_id.values() if q.get("tutorial")]
    if len(tutorials) > 1: r.error(name, "more than one tutorial quest: %s" % [q["id"] for q in tutorials])
    for q in tutorials:
        if not q.get("main") or q.get("after"): r.error("%s %s" % (name, q["id"]), "the tutorial must be a main quest without 'after'")
    opening = tutorials[0] if tutorials else quests[0]
    finales = [q for q in by_id.values() if q.get("finale")]
    line, on_line = [], set()
    if len(finales) != 1:
        r.error(name, "exactly one quest must have \"finale\": true (found %d)" % len(finales))
    else:
        finale = finales[0]; where = "%s %s" % (name, finale["id"])
        if not finale.get("main"): r.error(where, "the finale must be a main quest")
        line, problem = chain_root(by_id, finale)
        if problem: r.error(where, "finale is not reachable: %s" % problem)
        elif line[-1] != opening.get("id"): r.error(where, "finale chain starts at %r, not at the opening quest %r" % (line[-1], opening.get("id")))
        for key in ("endingA", "endingB"):
            if not text(finale.get(key)): r.warn(where, "missing '%s' (the built-in ending text is used)" % key)
        steps = finale.get("steps") or []
        if steps and isinstance(steps[-1], dict) and steps[-1].get("kind") == "battle":
            r.warn(where, "the last finale step is a battle; the ending then appears in a separate dialogue")
        on_line = set(line) if not problem else set()
        votes = sum(1 for qid in on_line for s in by_id[qid].get("steps", []) if isinstance(s, dict) and s.get("kind") == "choice")
        if on_line and votes == 0: r.error(where, "no choice step on the main line, so the ending can never vary")
        elif on_line and votes % 2 == 0: r.warn(where, "%d choice steps on the main line; a tie falls back to the consent ending" % votes)
        for qid, q in by_id.items():
            if on_line and qid not in on_line and any(isinstance(s, dict) and s.get("kind") == "choice" for s in q.get("steps", [])):
                r.warn("%s %s" % (name, qid), "choice step outside the main line also counts toward the ending")
    return dict(quests=len(by_id), main=len(mains), side=len(by_id) - len(mains), steps=sum(kinds.values()), kinds=kinds,
                line=len(on_line), choices=kinds.get("choice", 0))


def venue_ids(root):
    """Venue ids from FourCityCatalog.CreateVenues (index = facility), or None when the source is unavailable."""
    source = os.path.join(root, "Assets", "AfterSignal", "Runtime", "Worlds", "FourCityCatalog.cs")
    try:
        block = open(source, encoding="utf-8").read().split("CreateVenues(){", 1)[1].split("};", 1)[0]
    except (OSError, IndexError):
        return None
    return re.findall(r'new\("([^"]+)"', block) or None


def validate_four_city(r, path, root):
    name = os.path.basename(path)
    try:
        data = json.load(open(path, encoding="utf-8"))
    except (OSError, ValueError) as exc:
        r.error(name, "cannot read JSON: %s" % exc); return None
    chapters = data.get("chapters") if isinstance(data, dict) else None
    if not isinstance(chapters, list) or not chapters:
        r.error(name, "top level needs a non-empty 'chapters' array"); return None
    if len(chapters) < SAVED_CHAPTERS:
        r.error(name, "%d chapters; saved progress reaches chapter %d, removing chapters would skip players ahead" % (len(chapters), SAVED_CHAPTERS))
    venues = venue_ids(root)
    ids, decision = set(), None
    for index, c in enumerate(chapters):
        where = "%s chapter[%d]" % (name, index)
        if not isinstance(c, dict): r.error(where, "chapter must be an object"); continue
        if text(c.get("id")):
            where = "%s %s" % (name, c["id"])
            if c["id"] in ids: r.error(where, "duplicate id")
            ids.add(c["id"])
        else:
            r.error(where, "missing 'id'")
        unknown = set(c) - CHAPTER_KEYS
        if unknown: r.error(where, "unknown keys %s" % sorted(unknown))
        for key in ("title", "speaker", "line", "objective"):
            if not text(c.get(key)): r.error(where, "needs a non-empty '%s'" % key)
        for key in ("sync", "elite", "decision", "finale"):
            if key in c and not isinstance(c[key], bool): r.error(where, "'%s' must be true/false" % key)
        check_range(r, where, c, "facility", 0, (len(venues) - 1) if venues else 200, required=True, whole=True)
        check_range(r, where, c, "floor", 0, 5, required=True, whole=True)
        check_range(r, where, c, "enemies", 0, 24, required=True, whole=True)
        if c.get("elite") and not c.get("enemies"): r.warn(where, "'elite' has no effect without enemies")
        if c.get("sync") and venues and integer(c.get("facility")) and c["facility"] < len(venues) and venues[c["facility"]] != "erebos-vault":
            r.error(where, "the SYNC cores are built in erebos-vault, but this sync chapter is at %s" % venues[c["facility"]])
        if c.get("decision"):
            if decision is not None: r.error(where, "more than one decision chapter")
            decision = index
            if not text(c.get("prompt")): r.error(where, "decision chapter needs a 'prompt'")
            a, b = c.get("choiceA"), c.get("choiceB")
            if not text(a) or not text(b): r.error(where, "decision chapter needs 'choiceA' and 'choiceB' option labels")
            elif a.strip() == b.strip(): r.error(where, "choiceA and choiceB are identical")
        elif any(k in c for k in ("prompt", "choiceA", "choiceB")):
            r.warn(where, "prompt/choiceA/choiceB only apply to the decision chapter")
        if "resultA" in c or "resultB" in c:
            if not text(c.get("resultA")) or not text(c.get("resultB")): r.error(where, "needs both 'resultA' and 'resultB'")
            if decision is None or index <= decision: r.error(where, "result lines must come after the decision chapter")
    if decision is None: r.error(name, "no decision chapter (\"decision\": true)")
    if sum(1 for c in chapters if isinstance(c, dict) and c.get("sync")) > 1: r.error(name, "more than one sync chapter")
    finales = [i for i, c in enumerate(chapters) if isinstance(c, dict) and c.get("finale")]
    if finales != [len(chapters) - 1]: r.error(name, "exactly one finale, and it must be the last chapter (found at %s)" % finales)
    return dict(chapters=len(chapters), decision=decision)


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--root", default=os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    parser.add_argument("--strict", action="store_true", help="treat warnings as errors")
    args = parser.parse_args()
    story = os.path.join(args.root, "Assets", "AfterSignal", "Resources", "Story")
    r = Report()
    chronicle = validate_chronicle(r, os.path.join(story, "CityChronicle.json"))
    four = validate_four_city(r, os.path.join(story, "FourCityCampaign.json"), args.root)
    for w in r.warnings: print("warning: " + w)
    for e in r.errors: print("error: " + e)
    if chronicle:
        print("CityChronicle.json: %(quests)d quests (%(main)d main, %(side)d side), %(steps)d steps, main line %(line)d quests, %(choices)d choice steps" % chronicle)
        print("  step kinds: " + ", ".join("%s %d" % kv for kv in sorted(chronicle["kinds"].items())))
    if four:
        print("FourCityCampaign.json: %(chapters)d chapters, decision at chapter %(decision)s" % dict(four, decision=None if four["decision"] is None else four["decision"] + 1))
    failed = bool(r.errors) or (args.strict and bool(r.warnings))
    print("story data %s (%d errors, %d warnings)" % ("INVALID" if failed else "OK", len(r.errors), len(r.warnings)))
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
