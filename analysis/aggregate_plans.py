"""抄録用に計画書エクスポートCSVを集計する。

使い方: python analysis/aggregate_plans.py [CSVパス]
既定のCSVは confidential/export.csv（Shift_JIS）。結果は標準出力にMarkdownで出す。
"""
import csv
import statistics
import sys
from collections import Counter, defaultdict
from datetime import date
from pathlib import Path

DEFAULT_CSV = Path(__file__).resolve().parent.parent / "confidential" / "export.csv"

# 医師が入力する項目。id・患者ID・日付・医師/診療科・作成回数は比較対象外
CONTENT_FIELDS = [
    "main_diagnosis", "sheet_name", "target_weight", "target_bp", "target_hba1c",
    "goal1", "goal2", "target_achievement",
    "diet1", "diet2", "diet3", "diet4", "diet_comment",
    "exercise_prescription", "exercise_time", "exercise_frequency",
    "exercise_intensity", "daily_activity", "exercise_comment",
    "nonsmoker", "smoking_cessation", "other1", "other2",
    "ophthalmology", "dental", "cancer_screening",
]


def parse_date(s):
    y, m, d = map(int, s.split("/"))
    return date(y, m, d)


def pct(n, total):
    return f"{n:,} / {total:,} ({n / total * 100:.1f}%)"


def quartiles(values):
    q1, q2, q3 = statistics.quantiles(values, n=4)
    return f"中央値 {q2:g} (IQR {q1:g}–{q3:g}), 平均 {statistics.mean(values):.2f}"


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    path = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_CSV
    with open(path, encoding="cp932", newline="") as f:
        rows = list(csv.DictReader(f))

    by_patient = defaultdict(list)
    for r in rows:
        r["_date"] = parse_date(r["issue_date"])
        by_patient[r["patient_id"]].append(r)
    for plans in by_patient.values():
        plans.sort(key=lambda r: (r["_date"], int(r["id"])))

    total = len(rows)
    dates = [r["_date"] for r in rows]
    print("## 利用規模\n")
    print(f"- 計画書件数: {total:,}")
    print(f"- 期間: {min(dates)} 〜 {max(dates)}")
    print(f"- 患者数: {len(by_patient):,}")
    print(f"- 医師数: {len({r['doctor_id'] for r in rows})}")
    print(f"- 患者1人あたり件数: {quartiles([len(p) for p in by_patient.values()])}")
    print("- 主病名:")
    for name, n in Counter(r["main_diagnosis"] for r in rows).most_common():
        print(f"  - {name}: {pct(n, total)}")

    print("\n## 月別件数\n")
    for ym, n in sorted(Counter((d.year, d.month) for d in dates).items()):
        print(f"- {ym[0]}/{ym[1]:02d}: {n:,}")

    print("\n## 継続作成\n")
    continued = sum(1 for r in rows if int(r["creation_count"]) >= 2)
    print(f"- 作成回数2回以上: {pct(continued, total)}")
    with_prev = total - len(by_patient)
    print(f"- データ内に同一患者の前回計画がある件数: {pct(with_prev, total)}")

    pairs = [(p[i - 1], p[i]) for p in by_patient.values() for i in range(1, len(p))]
    changed_counts = []
    field_changes = Counter()
    intervals = []
    for prev, cur in pairs:
        changed = [f for f in CONTENT_FIELDS if prev[f] != cur[f]]
        changed_counts.append(len(changed))
        field_changes.update(changed)
        intervals.append((cur["_date"] - prev["_date"]).days)

    n_pairs = len(pairs)
    print(f"\n## 前回計画からの変更項目数（{len(CONTENT_FIELDS)}項目中, {n_pairs:,}組）\n")
    print(f"- 変更項目数: {quartiles(changed_counts)}")
    dist = Counter(changed_counts)
    print(f"- 変更なし: {pct(dist[0], n_pairs)}")
    print(f"- 2項目以下: {pct(sum(v for k, v in dist.items() if k <= 2), n_pairs)}")
    print(f"- 5項目以下: {pct(sum(v for k, v in dist.items() if k <= 5), n_pairs)}")
    print(f"- 前回からの間隔(日): {quartiles(intervals)}")
    print("- 項目別の変更率:")
    for field, n in field_changes.most_common():
        print(f"  - {field}: {pct(n, n_pairs)}")


if __name__ == "__main__":
    main()
