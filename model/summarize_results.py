"""Prints the 'Results at a glance' section of docs/methodology.md straight from outputs/results.json,
so the numbers in the document can never be out of date.   Run from the repo root:
    python model/summarize_results.py
Copy the output into section 7 of docs/methodology.md every time you regenerate results.json."""
import json

TIE_POINTS = 1.0     # overall scores closer than this are reported as tied

def main(path="outputs/results.json"):
    d = json.load(open(path))
    rots = sorted(d["rotations"], key=lambda r: r["rank"])
    names = {r["id"]: r["name"] for r in rots}
    short = lambda i: names[i].split(":")[0].split("(")[0].strip()

    print("## 7. Results at a glance (generated from outputs/results.json)\n")
    print("| Rotation | Water | Climate | Soil health | Overall | Under stress | Rank | Irrigation (mm per year) |")
    print("|---|---|---|---|---|---|---|---|")
    for r in rots:
        s = r["scores"]
        tied = [short(o["id"]) for o in rots if o["id"] != r["id"] and abs(o["scores"]["overall"] - s["overall"]) < TIE_POINTS]
        rank = f"{r['rank']} (tied with {', '.join(tied)})" if tied else str(r["rank"])
        print(f"| {short(r['id'])} | {s['water']} | {s['climate']} | {s['soil_health']} | {s['overall']} | "
              f"{r['stress_test']['overall_under_stress']} | {rank} | {r['details']['yearly_irrigation_mm']} |")

    u = d["uncertainty_ranges"]
    print(f"\n- Uncertainty cases (water need -10% to +10%, heat limits -2 to +2 C): {u['cases_total']} cases.")
    for rid, n in u["cases_ranked_first"].items():
        if n:
            print(f"  - {short(rid)} ranks first in {n} of {u['cases_total']} cases.")
    gaps = []
    for c in u["cases"]:
        o = sorted(c["overall"].values(), reverse=True)
        gaps.append(o[0] - o[1])
    print(f"- The leader's margin over second place is {min(gaps):.1f} to {max(gaps):.1f} points across the cases.")
    print("- Overall score ranges: " + "; ".join(f"{short(k)} {v['min']} to {v['max']}" for k, v in u["overall_range"].items()) + ".")
    print("- The order of the rotations " + ("changes" if u["ranking_ever_changes"] else "never changes")
          + " across the cases (this includes swaps between second and third place).")
    st = d["ranking_stability"]
    print("- Across farmer priorities, the share of settings in which each rotation ranks first: "
          + ", ".join(f"{short(k)} {round(100 * v)}%" for k, v in st.items()) + ".")
    for crit, f in d["flip_points"].items():
        label = crit.replace("_", " ")
        if f["top_never_changes"]:
            print(f"- {label.capitalize()}: the leader ({short(f['leader_at_0_pct'])}) never changes at any weight.")
        else:
            parts = [f"from {short(x['from'])} to {short(x['to'])} at about {x['at_weight_pct']}%" for x in f["flip_points"]]
            print(f"- {label.capitalize()} (default weight {f['current_weight_pct']}%): as its weight rises, the leader changes "
                  + "; ".join(parts) + ".")
    print(f"\nData status: {d['data_status']}  |  ET0 source: {d['et0_source']}")

if __name__ == "__main__":
    main()