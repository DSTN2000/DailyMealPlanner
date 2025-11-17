import pathlib
import pandas as pd
import sqlite3
import urllib.request
import zipfile
import os

# Check if the .tsv file is not present, download it first
script_dir = pathlib.Path(__file__).parent
tsv_file = script_dir / "opennutrition_foods.tsv"

if not tsv_file.exists():
    print("opennutrition_foods.tsv not found. Downloading...")

    # 1. Download the zip file
    zip_url = "https://downloads.opennutrition.app/opennutrition-dataset-2025.1.zip"
    zip_path = script_dir / "opennutrition-dataset-2025.1.zip"

    urllib.request.urlretrieve(zip_url, zip_path)
    print(f"Downloaded {zip_path}")

    # 2. Unzip
    with zipfile.ZipFile(zip_path, 'r') as zip_ref:
        zip_ref.extractall(script_dir)
    print("Extracted files")

    # 3. Only keep the "opennutrition_foods.tsv" file and move it to the same level as this file
    import shutil

    # Find the .tsv file in extracted contents
    for root, dirs, files in os.walk(script_dir):
        for file in files:
            if file == "opennutrition_foods.tsv":
                extracted_tsv = pathlib.Path(root) / file
                if extracted_tsv != tsv_file:
                    extracted_tsv.rename(tsv_file)
                    print(f"Moved {file} to {tsv_file}")
                break

    # Remove all download artifacts: zip file, extracted directories, and any other extracted files
    zip_path.unlink()
    print("Removed zip file")

    # Remove any extracted directories and files (except the .tsv file)
    for item in script_dir.iterdir():
        if item.name.startswith("LICENSE") or item.name.startswith("README"):
            item.unlink()

    print("Setup complete!")

df = pd.read_csv(tsv_file, sep='\t')
# DEBUG: save as .csv
# df.to_csv('opennutrition_foods.csv')

con = sqlite3.connect('opennutrition_foods.db')
df.to_sql('opennutrition_foods.db', con)