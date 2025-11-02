import pandas as pd
import sqlite3

df = pd.read_csv("opennutrition_foods.tsv", sep='\t')
df.to_csv('opennutrition_foods.csv')

con = sqlite3.connect('opennutrition_foods.db')
df.to_sql('opennutrition_foods.db', con)