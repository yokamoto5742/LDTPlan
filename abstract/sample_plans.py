"""計画作成時間の計測に使う計画書を、本番DBのコピーから無作為に抽出する。

使い方:
    python sample_plans.py <DBファイル> <シード> [件数]

DBは読み取り専用で開く。結果はタブ区切りで画面に出すだけで、ファイルには保存しない。
患者IDを含むので、出力をリポジトリにコミットしない。
"""

import random
import sqlite3
import sys

PERIOD_START = "2024-09-01"
PERIOD_END = "2026-08-31"


def main() -> None:
    if len(sys.argv) < 3:
        print(__doc__)
        sys.exit(1)
    db_path, seed = sys.argv[1], int(sys.argv[2])
    count = int(sys.argv[3]) if len(sys.argv) > 3 else 10

    con = sqlite3.connect(f"file:{db_path}?mode=ro", uri=True)
    ids = [
        row[0]
        for row in con.execute(
            "SELECT id FROM patient_info"
            " WHERE substr(issue_date, 1, 10) BETWEEN ? AND ? ORDER BY id",
            (PERIOD_START, PERIOD_END),
        )
    ]
    print(f"母集団: {len(ids)}件 ({PERIOD_START}～{PERIOD_END})  シード: {seed}")

    picked = random.Random(seed).sample(ids, count)
    print("No\tid\tpatient_id\t作成日\t作成回数\t主病名\tシート名\t前回のid")
    for no, plan_id in enumerate(picked, start=1):
        patient_id, issue_date, creation_count, diagnosis, sheet = con.execute(
            "SELECT patient_id, substr(issue_date, 1, 10), creation_count,"
            " main_diagnosis, sheet_name FROM patient_info WHERE id = ?",
            (plan_id,),
        ).fetchone()
        previous = con.execute(
            "SELECT id FROM patient_info WHERE patient_id = ?"
            " AND substr(issue_date, 1, 10) < ? ORDER BY issue_date DESC, id DESC LIMIT 1",
            (patient_id, issue_date),
        ).fetchone()
        print(
            f"{no}\t{plan_id}\t{patient_id}\t{issue_date}\t{creation_count}"
            f"\t{diagnosis}\t{sheet}\t{previous[0] if previous else ''}"
        )


if __name__ == "__main__":
    main()
