import datetime
from typing import Any, Union

from hbc import utils as ul


class AppContext:
    """Lightweight container for runtime directories and the logical as-of date."""

    def __init__(self) -> None:
        """Initialize default directories and set the logical date to today."""
        # why: keep storage private so validation runs via the property
        self._as_of: datetime.date = datetime.date.today()
        self.dir_base = ul.get_dir_base()
        self.dir_cache = ul.get_dir_cache()
        self.dir_analytics = ul.get_dir_analytics()
        self.dir_logging = ul.get_dir_logging()

    def __str__(self) -> str:
        """Pretty string representation for logging/debug output."""

        def fmt(v: Any) -> str:
            if isinstance(v, (datetime.date, datetime.datetime)):
                return v.isoformat()
            return repr(v)

        body = ",\n".join(
            f"{k}: {fmt(v)}"
            for k, v in sorted(self.__dict__.items())
            if not k.startswith("_")
        )
        return f"{self.__class__.__name__}\nas_of : {self.as_of}\n{body}"

    __repr__ = __str__

    @property
    def as_of(self) -> datetime.date:
        """Get the current logical business date."""
        return self._as_of

    @as_of.setter
    def as_of(
        self, value: Union[datetime.date, datetime.datetime, str]
    ) -> None:
        """
        Set the logical business date.
        Accepts `date`, `datetime` (converted to date), or ISO `YYYY-MM-DD` string.
        """
        if isinstance(value, datetime.datetime):
            self._as_of = value.date()
            return
        if isinstance(value, datetime.date):
            self._as_of = value
            return
        if isinstance(value, str):
            try:
                self._as_of = ul.str_as_date(value)
                return
            except ValueError as exc:
                raise ValueError(
                    f"Invalid ISO date string for as_of: {value!r}"
                ) from exc
        raise TypeError(
            "as_of must be a datetime.date, datetime.datetime, or ISO 'YYYY-MM-DD' string"
        )


app_context = AppContext()
