import argparse
from typing import Any, Sequence

from hbc import app_context, utils as ul
from hbc.jobs.pipeline.job_nyc_open_data import job_poll_nyc_open_data_311
from hbc.jobs.registry import JOB_REGISTRY


def ns_to_dict(ns: argparse.Namespace) -> dict[str, Any]:
    def conv(v: Any) -> Any:
        if isinstance(v, argparse.Namespace):
            return {k: conv(getattr(v, k)) for k in vars(v)}
        if isinstance(v, list):
            return [conv(i) for i in v]
        return v

    return {k: conv(getattr(ns, k)) for k in vars(ns)}


def _infer_type(s: str) -> Any:
    ls = s.lower()
    if ls in {"true", "false"}:
        return ls == "true"
    try:
        if "." in s:
            return float(s)
        return int(s)
    except ValueError:
        return s


def _parse_extra_kwargs(rest: list[str]) -> dict[str, Any]:
    """
    Accepts: --key=value, --key value, and bare --flag (→ True).
    """
    out: dict[str, Any] = {}
    it = iter(rest)
    for token in it:
        if not token.startswith("--"):
            continue
        key = token[2:].strip()
        if not key:
            continue

        # --key=value form
        if "=" in key:
            k, v = key.split("=", 1)
            out[k.replace("-", "_")] = _infer_type(v)
            continue

        # Try space-separated value: --key value
        try:
            nxt = next(it)
        except StopIteration:
            out[key.replace("-", "_")] = True  # bare flag
            continue

        if nxt.startswith("--"):
            # The next token is another option → current is a bare flag
            out[key.replace("-", "_")] = True
            # push back: handle nxt in next loop iteration
            it = iter([nxt, *it])
        else:
            out[key.replace("-", "_")] = _infer_type(nxt)
    return out


def main(argv: Sequence[str] | None = None) -> None:
    parser = argparse.ArgumentParser("hbc.jobs")
    parser.add_argument(
        "--job-name", required=True, choices=sorted(JOB_REGISTRY.keys())
    )
    parser.add_argument("--as-of", type=str, help="YYYY-MM-DD (optional)")

    args, rest = parser.parse_known_args(argv)
    run_kwargs = _parse_extra_kwargs(list(rest))

    if args.as_of:
        app_context.update(as_of=ul.str_as_date(args.as_of))

    job_fn = JOB_REGISTRY[args.job_name]
    job_fn(**run_kwargs)


if __name__ == "__main__":
    main(["--as-of=20090105", "--job-name=job_poll_nyc_open_data_311"])
