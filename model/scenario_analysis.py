"""Sensitivity: how stable is the ranking when the farmer's priorities change?"""
import itertools
from scoring import rank

def weight_sweep(rotations, criteria, step=10):
    """Try every weight combination on a grid; return how often each rotation wins."""
    wins = {rid: 0 for rid in rotations}
    n = 0
    for combo in itertools.product(range(0, 101, step), repeat=len(criteria)):
        if sum(combo) != 100:
            continue
        w = dict(zip(criteria, combo))
        wins[rank(rotations, w)[0][0]] += 1
        n += 1
    return {rid: round(c / n, 2) for rid, c in wins.items()}
