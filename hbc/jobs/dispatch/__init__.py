import argparse
import logging
from pathlib import Path
from typing import Any, Sequence

from hbc import app_context, utils as ul
from hbc.jobs.registry import JOB_REGISTRY


def ns_to_dict(ns: argparse.Namespace) -> dict[str, Any]:
    """Recursively convert argparse.Namespace (with nested namespaces) to dict."""

    def conv(v: Any) -> Any:
        if isinstance(v, argparse.Namespace):
            return {k: conv(getattr(v, k)) for k in vars(v)}
        if isinstance(v, list):
            return [conv(i) for i in v]
        return v

    return {k: conv(getattr(ns, k)) for k in vars(ns)}


def _infer_type(s: str) -> Any:
    """Try to coerce CLI string token to bool/int/float; fallback to str."""

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
    """Entrypoint for dispatching a registered job with parsed CLI args."""
    parser = argparse.ArgumentParser("hbc.jobs")
    parser.add_argument(
        "--job-name", required=True, choices=sorted(JOB_REGISTRY.keys())
    )
    parser.add_argument("--as-of", type=str, help="YYYY-MM-DD (optional)")
    parser.add_argument("--log-level", type=str, help="log level")
    parser.add_argument("--dir-base", type=str, help="Base directory override")
    parser.add_argument(
        "--dir-cache", type=str, help="Cache directory override"
    )
    parser.add_argument(
        "--dir-analytics", type=str, help="Analytics directory override"
    )
    parser.add_argument(
        "--dir-logging", type=str, help="Logging directory override"
    )

    args, rest = parser.parse_known_args(argv)

    log_level = args.log_level if args.log_level else logging.INFO

    run_kwargs = _parse_extra_kwargs(list(rest))
    if args.as_of:
        app_context.as_of = args.as_of
    app_context.dir_base = (
        Path(args.dir_base) if args.dir_base else ul.get_dir_base()
    )
    # keep utility helpers in sync when base is overridden via CLI
    ul.set_dir_base(app_context.dir_base)
    app_context.dir_cache = (
        Path(args.dir_cache) if args.dir_cache else ul.get_dir_cache()
    )
    app_context.dir_analytics = (
        Path(args.dir_analytics)
        if args.dir_analytics
        else ul.get_dir_analytics()
    )
    app_context.dir_logging = (
        Path(args.dir_logging) if args.dir_logging else ul.get_dir_logging()
    )

    # we conf log for the job
    ul.conf_log(
        file_path=ul.path_to_str(
            ul.mk_dir(app_context.dir_logging / args.job_name)
            / f"{args.job_name}_{ul.get_id()}.txt"
        ),
        level=log_level,
        console=False,
        file=True,
        reset_handlers=True,
    ),

    # we execute job
    job_fn = JOB_REGISTRY[args.job_name]
    job_fn(**run_kwargs)


if __name__ == "__main__":
    main(
        [
            "--as-of=20090105",
            "--job-name=job_poll_nyc_open_data_311",
            "--incremental=False",
        ]
    )
