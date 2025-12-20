# File: app/creds.py

import os
from pathlib import Path
from typing import Optional, Tuple

def get_app_tokens(env_path: Optional[str] = None) -> Tuple[str, str]:
    """
    Load APP_TOKEN and APP_SECRET from environment or a .env file (one function only).

    - If python-dotenv is installed, prefer it (no overwrites).
    - Otherwise, minimally parse KEY=VALUE lines from `.env` and export into os.environ.
    - Validates both values are present and non-empty.
    - Returns (APP_TOKEN, APP_SECRET).

    Args:
        env_path: Optional path to .env. Defaults to './.env' if present.

    Raises:
        RuntimeError: if APP_TOKEN or APP_SECRET is missing/blank.
    """
    # Attempt python-dotenv if available; fallback to minimal parser.
    try:
        from dotenv import load_dotenv  # type: ignore
        if env_path:
            load_dotenv(env_path, override=False)
        else:
            default_env = Path.cwd() / ".env"
            if default_env.exists():
                load_dotenv(default_env, override=False)
            else:
                load_dotenv(override=False)
    except Exception:
        p = Path(env_path) if env_path else Path.cwd() / ".env"
        if p.exists():
            with p.open("r", encoding="utf-8") as f:
                for raw in f:
                    line = raw.strip()
                    if not line or line.startswith("#") or "=" not in line:
                        continue
                    key, val = line.split("=", 1)
                    key = key.strip()
                    val = val.strip().split(" #", 1)[0].strip()
                    if (val.startswith('"') and val.endswith('"')) or (val.startswith("'") and val.endswith("'")):
                        val = val[1:-1]
                    os.environ.setdefault(key, val)  # don't override real env

    token = os.getenv("APP_TOKEN", "").strip()
    secret = os.getenv("APP_SECRET", "").strip()
    if not token:
        raise RuntimeError("APP_TOKEN missing. Set it as an env var or in `.env`.")
    if not secret:
        raise RuntimeError("APP_SECRET missing. Set it as an env var or in `.env`.")
    return token, secret
