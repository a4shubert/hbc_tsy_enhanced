# setup.py
from pathlib import Path

from setuptools import find_packages, setup

ROOT = Path(__file__).parent

setup(
    name="hbc",  # <- what shows up in pip / PyPI (distribution name)
    version="0.1.1",
    description="HBC utilities and jobs",
    long_description=(ROOT / "README.md").read_text(encoding="utf-8") if (ROOT / "README.md").exists() else "",
    long_description_content_type="text/markdown",
    license="MIT",
    packages=find_packages(exclude=("notebooks", "tests", ".git", ".idea", "__MACOSX")),
    include_package_data=True,
    python_requires=">=3.9",
    install_requires=[
        "numpy",
        "pandas",
        "sqlalchemy",
        "openpyxl",
        "PyYAML",
        "schedule",
        "sodapy",
        "folium",
        "matplotlib",
        "ipython",
        "notebook",
        "nbclassic",
        "pytest",
    ],
    entry_points={
        "console_scripts": [
            "hbc-dispatch=hbc.jobs.dispatch:main",
        ],
    },
)
