import shlex
import subprocess
import time

import schedule  # lightweight job scheduling, uses system local time


def midnight_scheduler(*commands: str, run_now: bool = False) -> None:
    """
    Run given shell commands sequentially at local midnight every day.

    Args:
        *commands: Full command-line strings (e.g., 'python -m ... --flag=value').
        run_now:   If True, run once immediately before scheduling.

    Notes:
        - Stops the daily sequence on the first non-zero exit code.
        - Uses system local time for midnight. Set your container/host TZ appropriately.
        - Blocks forever (until interrupted).
    """

    def run_all() -> None:
        # why: stop on first failure to avoid dependent job running on bad state
        for idx, cmd in enumerate(commands, 1):
            print(
                f"\n[{time.strftime('%Y-%m-%d %H:%M:%S')}] ({idx}/{len(commands)}) $ {cmd}",
                flush=True,
            )
            rc = subprocess.run(shlex.split(cmd), check=False).returncode
            if rc != 0:
                print(
                    f"Command failed with exit code {rc}; stopping today's run.",
                    flush=True,
                )
                return
        print("All commands finished successfully.", flush=True)

    if run_now:
        run_all()

    schedule.every().day.at("00:00").do(run_all)
    print("Midnight scheduler started. Next run at local 00:00.", flush=True)
    try:
        while True:
            schedule.run_pending()
            time.sleep(1)
    except KeyboardInterrupt:
        print("\nScheduler interrupted. Bye.", flush=True)


if __name__ == "__main__":
    midnight_scheduler(
        "python -m hbc.jobs.dispatch --job-name=job_fetch_nyc_open_data_311_service_requests --as-of=20091231 --incremental=True --log-level=INFO",
        "python -m hbc.jobs.dispatch --job-name=job_analyse_nyc_open_data_311_service_requests --as-of=20091231 --log-level=INFO --n-worst=10 --n-best=10 --n-days=10",
        run_now=False,
    )
