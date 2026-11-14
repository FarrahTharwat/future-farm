"""Weighted multi-criteria scoring (MCDA):  Score(R) = sum_i w_i * S_i(R)
Every sub-score is 0-100. Weights are the farmer's priorities and are normalised to sum to 1.
Because the model is a plain weighted sum, each criterion's contribution (w_i * S_i)
IS the explanation. No SHAP needed.
"""

def normalise_weights(weights):
    total = sum(weights.values())
    if total <= 0:
        raise ValueError("weights must sum to > 0")
    return {k: v / total for k, v in weights.items()}

def score_rotation(sub_scores, weights):
    w = normalise_weights(weights)
    contributions = {k: round(w[k] * sub_scores[k], 2) for k in w}
    return {"overall": round(sum(contributions.values()), 1), "contributions": contributions}

def rank(rotations, weights):
    """rotations: {rotation_id: {criterion: 0-100}}  ->  list of (id, overall) best first"""
    scored = {rid: score_rotation(s, weights)["overall"] for rid, s in rotations.items()}
    return sorted(scored.items(), key=lambda x: -x[1])
