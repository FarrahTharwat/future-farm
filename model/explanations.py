"""Turn score contributions into plain-language sentences (template based, no LLM)."""
LABELS = {"water": "water suitability", "climate": "climate suitability",
          "soil_health": "soil-health potential", "resilience": "climate resilience"}

def explain(name, sub_scores, contributions):
    top = max(contributions, key=contributions.get)
    low = min(sub_scores, key=sub_scores.get)
    return [
        f"{name}: its biggest contribution to the overall score comes from {LABELS.get(top, top)}.",
        f"Its weakest area is {LABELS.get(low, low)} (score {sub_scores[low]}), which needs local review.",
    ]
