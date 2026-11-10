#!/usr/bin/env python3
"""Run VerifyUi through the Unity Pipeline bridge without shell quoting pain.

Windows PowerShell 5.1 strips the double quotes out of a `--args '["..."]'` argument
before the command line reaches `unity`, and `run_script` then rejects the payload as
"not a JArray". This wrapper builds the JSON argument in-process, so the shell only ever
sees plain words.

Usage (from the project root):

    python .opencode/skills/verify/scripts/unity-run.py state
    python .opencode/skills/verify/scripts/unity-run.py buttons
    python .opencode/skills/verify/scripts/unity-run.py texts
    python .opencode/skills/verify/scripts/unity-run.py click Canvas/M6TitleRoot/M6MainMenuRoot/M6NewGameButton

`click` takes the ASCII hierarchy path printed by `buttons`, never a Chinese label: the
label would have to survive the console code page on the way to the Editor.
"""

import json
import subprocess
import sys

HELPER = ".opencode/skills/verify/scripts/VerifyUi.cs"

ENTRIES = {
    "state": ("VerifyUi.State", None),
    "buttons": ("VerifyUi.Buttons", None),
    "texts": ("VerifyUi.Texts", None),
    "click": ("VerifyUi.ClickPath", "path"),
    "probe": ("VerifyUi.Text", "path"),
}


def main(argv):
    if len(argv) < 2 or argv[1] not in ENTRIES:
        sys.stderr.write(__doc__)
        return 2

    entry, needs = ENTRIES[argv[1]]
    command = ["unity", "command", "run_script", "--file", HELPER, "--entry", entry]

    if needs is not None:
        if len(argv) < 3:
            sys.stderr.write(f"'{argv[1]}' needs a {needs} argument\n")
            return 2
        command += ["--args", json.dumps([argv[2]])]

    return subprocess.call(command)


if __name__ == "__main__":
    sys.exit(main(sys.argv))
