import datetime
from typing import Any


class AppContext:
    def __init__(self):
        self.as_of: datetime.date = datetime.date.today()

    def __str__(self) -> str:
        def fmt(v: Any) -> str:
            if isinstance(
                v, (datetime.date, datetime.datetime)
            ):  # why: human-friendly ISO
                return v.isoformat()
            return repr(v)

        body = ", ".join(
            f"{k}={fmt(v)}" for k, v in sorted(self.__dict__.items())
        )
        return f"{self.__class__.__name__}({body})"

    __repr__ = __str__

    def update(self, **kwargs: Any) -> "AppContext":
        for k, v in kwargs.items():
            if hasattr(self, k):
                setattr(self, k, v)
        return self


app_context = AppContext()
