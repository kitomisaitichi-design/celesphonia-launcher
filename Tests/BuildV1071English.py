"""Carry the reviewed 1.07 display pack only after checking 1.071's full inventory."""
from pathlib import Path
import argparse
import json
import shutil
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))
import EnglishCatalog as catalog

parser = argparse.ArgumentParser()
parser.add_argument('--extracted', type=Path, required=True)
args = parser.parse_args()
game = args.extracted.resolve()
assert (game / 'data/System.json').is_file()
ident = 'three-in-one-3-3-5-v1-071'
prior = ROOT / 'English/three-in-one-3-3-5-v1-07'
recipe = next(row for row in catalog.load(ROOT / 'mod-recipes.json')['Recipes'] if row['Id'] == ident)
assert all(catalog.sha((game / name).read_bytes()) == digest for name, digest in recipe['Fingerprints'].items())
inventory = catalog.inventory(game)
assert not inventory['errors'], inventory['errors']
previous = catalog.load(prior / 'catalog.json')
new = {row['source'] for row in inventory['slots'].values() if catalog.CJK.search(row['source']) and row['source'] not in previous}
assert not new, 'Review added display strings before carrying the previous pack.'
target = ROOT / 'English' / ident
target.mkdir(parents=True, exist_ok=True)
shutil.copy2(prior / 'dictionary.json', target / 'dictionary.json')
catalog.dump(target / 'catalog.json', {key: {'source': key, **row} for key, row in previous.items()})
manifest = catalog.load(prior / 'manifest.json')
manifest.update(VersionId=ident, Name=recipe['Name'] + ' English text overlay', Fingerprints=recipe['Fingerprints'])
manifest['CatalogHash'] = catalog.sha((target / 'catalog.json').read_bytes())
catalog.dump(target / 'manifest.json', manifest)
print(json.dumps({'status': 'PASS', 'version': ident, 'newDisplayStrings': 0, 'translatedTexts': manifest['TranslatedTexts'], 'dictionaryReusedByteForByte': True}))
