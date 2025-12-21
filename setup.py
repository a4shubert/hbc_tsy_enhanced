# setup.py
"""
hbc-tsy packaging (setuptools).

Install:
    pip install .

Editable install (dev):
    pip install -e ".[dev]"
"""
from pathlib import Path

from setuptools import find_packages, setup


BASE_DIR = Path(__file__).resolve().parent


def read_text(rel_path: str) -> str:
    path = BASE_DIR / rel_path
    return path.read_text(encoding="utf-8") if path.exists() else ""


setup(
    name="hbc-tsy",
    version="0.1.0",
    description="NYC 311 Service Requests data pipeline and utilities.",
    long_description=read_text("README.md"),
    long_description_content_type="text/markdown",
    license="MIT",
    python_requires=">=3.10",
    packages=find_packages(exclude=("notebooks", "tests", "*.tests", "*.tests.*")),
    include_package_data=True,
    package_data={
        "hbc": [
            "ltp/configs/*.yaml",
        ]
    },
    install_requires=[
        "numpy>=1.23",
        "pandas>=1.5",
        "openpyxl>=3.1",
        "PyYAML>=6.0",
        "schedule>=1.2",
        "sodapy>=2.2",
        "folium>=0.14",
        "matplotlib>=3.7",
        "ipython>=8.0",
    ],
    extras_require={
        "dev": [
            "pytest>=7.0",
            "ruff>=0.1.0",
            "black>=23.0",
            "mypy>=1.5",
        ]
    },
    entry_points={
        "console_scripts": [
            "hbc-dispatch=hbc.jobs.dispatch:main",
        ]
    },
    classifiers=[
        "Programming Language :: Python :: 3",
        "Programming Language :: Python :: 3 :: Only",
        "License :: OSI Approved :: MIT License",
        "Operating System :: OS Independent",
    ],
)
